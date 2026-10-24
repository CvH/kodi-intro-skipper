// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

using System.Threading.Channels;
using IntroSkipper.Server.Scanning;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IntroSkipper.Server.Analysis;

/// <summary>
/// Background worker processing media scan and analysis jobs sequentially.
/// </summary>
public sealed class AnalysisQueue(
    MediaScanner scanner,
    MediaFileIndex fileIndex,
    StandaloneAnalyzer analyzer,
    ScanProgressTracker progressTracker,
    ILogger<AnalysisQueue> logger) : BackgroundService
{
    private readonly Channel<ScanJob> _channel = Channel.CreateUnbounded<ScanJob>(new UnboundedChannelOptions
    {
        SingleReader = true,
        SingleWriter = false,
    });

    private readonly Lock _lock = new();
    private string _currentStatus = "Idle";
    private int _queuedCount;
    private bool _isScanning;
    private CancellationTokenSource? _activeJobCts;

    /// <summary>
    /// Gets the current status of the background analysis worker.
    /// </summary>
    public string Status
    {
        get
        {
            lock (_lock)
            {
                return _currentStatus;
            }
        }
        private set
        {
            lock (_lock)
            {
                _currentStatus = value;
            }
        }
    }

    /// <summary>
    /// Gets the count of jobs currently waiting in queue.
    /// </summary>
    public int QueuedCount
    {
        get
        {
            lock (_lock)
            {
                return _queuedCount;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether a scan is currently active.
    /// </summary>
    public bool IsScanning
    {
        get
        {
            lock (_lock)
            {
                return _isScanning;
            }
        }
    }

    /// <summary>
    /// Enqueues a folder for scanning and analysis.
    /// </summary>
    /// <param name="folderPath">Directory to scan.</param>
    /// <param name="exportEdl">Whether to generate EDL files.</param>
    public void Enqueue(string folderPath, bool exportEdl = false)
    {
        lock (_lock)
        {
            _queuedCount++;
        }

        _channel.Writer.TryWrite(new ScanJob(folderPath, exportEdl));
        logger.LogInformation("Enqueued scan for {Path}", folderPath);
    }

    /// <summary>
    /// Stops the currently running scan job immediately and clears all pending jobs in the queue.
    /// </summary>
    /// <returns>True if an active or queued scan was stopped, false if already idle.</returns>
    public bool StopScan()
    {
        lock (_lock)
        {
            if (!_isScanning && _queuedCount == 0)
            {
                return false;
            }

            logger.LogInformation(">>> Stopping active scan and draining pending queue...");

            try
            {
                _activeJobCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            while (_channel.Reader.TryRead(out _))
            {
            }

            _queuedCount = 0;
            _isScanning = false;
            _currentStatus = "Stopped";
            progressTracker.FinishScan("Stopped");
            return true;
        }
    }

    /// <inheritdoc/>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("IntroSkipper background analysis service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            ScanJob job;
            try
            {
                job = await _channel.Reader.ReadAsync(stoppingToken).ConfigureAwait(false);
                lock (_lock)
                {
                    _queuedCount = Math.Max(0, _queuedCount - 1);
                    _isScanning = true;
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }

            using var jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            lock (_lock)
            {
                _activeJobCts = jobCts;
            }

            try
            {
                Status = $"Scanning: {job.FolderPath}";
                progressTracker.StartScan(job.FolderPath);

                var episodes = await scanner.ScanDirectoryAsync(job.FolderPath, jobCts.Token).ConfigureAwait(false);

                foreach (var ep in episodes)
                {
                    fileIndex.Register(ep);
                }

                var seasons = episodes.GroupBy(e => (e.SeriesName, e.SeasonNumber)).ToList();
                logger.LogInformation("Found {EpCount} episodes across {SeasonCount} seasons in {Path}", episodes.Count, seasons.Count, job.FolderPath);

                var index = 1;
                foreach (var seasonGroup in seasons)
                {
                    jobCts.Token.ThrowIfCancellationRequested();
                    Status = $"Analyzing season {index}/{seasons.Count}: {seasonGroup.Key.SeriesName} S{seasonGroup.Key.SeasonNumber}";

                    await analyzer.AnalyzeSeasonAsync([.. seasonGroup], job.ExportEdl, jobCts.Token).ConfigureAwait(false);
                    index++;
                }

                progressTracker.FinishScan("Idle");
            }
            catch (OperationCanceledException) when (!stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation(">>> Scan for {Path} was stopped by user request", job.FolderPath);
                progressTracker.FinishScan("Stopped");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing scan job for {Path}", job.FolderPath);
                progressTracker.FinishScan("Error");
            }
            finally
            {
                lock (_lock)
                {
                    _activeJobCts = null;
                    _isScanning = false;
                    Status = "Idle";
                }
            }
        }

        logger.LogInformation("IntroSkipper background analysis service stopped");
    }

    private sealed record ScanJob(string FolderPath, bool ExportEdl);
}
