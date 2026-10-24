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
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }

            try
            {
                Status = $"Scanning: {job.FolderPath}";
                var episodes = await scanner.ScanDirectoryAsync(job.FolderPath, stoppingToken).ConfigureAwait(false);

                foreach (var ep in episodes)
                {
                    fileIndex.Register(ep);
                }

                var seasons = episodes.GroupBy(e => (e.SeriesName, e.SeasonNumber)).ToList();
                logger.LogInformation("Found {EpCount} episodes across {SeasonCount} seasons in {Path}", episodes.Count, seasons.Count, job.FolderPath);

                var index = 1;
                foreach (var seasonGroup in seasons)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    Status = $"Analyzing season {index}/{seasons.Count}: {seasonGroup.Key.SeriesName} S{seasonGroup.Key.SeasonNumber}";

                    await analyzer.AnalyzeSeasonAsync([.. seasonGroup], job.ExportEdl, stoppingToken).ConfigureAwait(false);
                    index++;
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing scan job for {Path}", job.FolderPath);
            }
            finally
            {
                Status = "Idle";
            }
        }

        logger.LogInformation("IntroSkipper background analysis service stopped");
    }

    private sealed record ScanJob(string FolderPath, bool ExportEdl);
}
