// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

using Microsoft.Extensions.Logging;

namespace IntroSkipper.Server.Analysis;

/// <summary>
/// Thread-safe tracker and logger for active media scanning and analysis operations.
/// </summary>
public sealed class ScanProgressTracker
{
    private const int MaxRecentFiles = 20;

    private readonly ILogger<ScanProgressTracker> _logger;
    private readonly Lock _lock = new();
    private readonly List<string> _recentlyScanned = [];

    private bool _isScanning;
    private string? _currentFolder;
    private string? _currentFile;
    private string? _currentSeries;
    private int? _currentSeason;
    private int? _currentEpisode;
    private string _currentStage = "Idle";
    private int _fileIndex;
    private int _totalFiles;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScanProgressTracker"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    public ScanProgressTracker(ILogger<ScanProgressTracker> logger)
    {
        _logger = logger;
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
    /// Marks the start of a scan job.
    /// </summary>
    /// <param name="folderPath">Root folder or share being scanned.</param>
    public void StartScan(string folderPath)
    {
        lock (_lock)
        {
            _isScanning = true;
            _currentFolder = folderPath;
            _currentFile = null;
            _currentSeries = null;
            _currentSeason = null;
            _currentEpisode = null;
            _currentStage = "Discovering files";
            _fileIndex = 0;
            _totalFiles = 0;
        }

        _logger.LogInformation(">>> Started scanning media folder/share: {Path}", folderPath);
    }

    /// <summary>
    /// Updates the currently scanned file and operational stage, logging the event to server logs.
    /// </summary>
    /// <param name="filePath">Full path to the video file currently being analyzed.</param>
    /// <param name="series">Series name.</param>
    /// <param name="season">Season number.</param>
    /// <param name="episode">Episode number.</param>
    /// <param name="stage">Current analysis stage description.</param>
    /// <param name="index">1-based file index within the current batch.</param>
    /// <param name="total">Total files in the current batch.</param>
    public void UpdateFile(
        string filePath,
        string? series,
        int? season,
        int? episode,
        string stage,
        int index,
        int total)
    {
        lock (_lock)
        {
            _currentFile = filePath;
            _currentSeries = series;
            _currentSeason = season;
            _currentEpisode = episode;
            _currentStage = stage;
            _fileIndex = index;
            _totalFiles = total;
        }

        var progressPrefix = total > 0 ? $"[{index}/{total}] " : string.Empty;
        var episodeTag = (season.HasValue && episode.HasValue) ? $" (S{season.Value:D2}E{episode.Value:D2})" : string.Empty;
        var showName = !string.IsNullOrWhiteSpace(series) ? $"{series}{episodeTag}: " : string.Empty;

        _logger.LogInformation(">>> Scanning file {Progress}{Show}{File} | Stage: {Stage}", progressPrefix, showName, filePath, stage);
    }

    /// <summary>
    /// Reports an active media analysis operation on a specific file.
    /// </summary>
    /// <param name="filePath">File currently being processed.</param>
    /// <param name="activity">Activity description.</param>
    public void ReportActivity(string filePath, string activity)
    {
        lock (_lock)
        {
            _currentFile = filePath;
            _currentStage = activity;
        }

        _logger.LogInformation(">>> [{Activity}] {File}", activity, filePath);
    }

    /// <summary>
    /// Updates the general operational stage name.
    /// </summary>
    /// <param name="stage">Stage description.</param>
    public void UpdateStage(string stage)
    {
        lock (_lock)
        {
            _currentStage = stage;
        }

        _logger.LogInformation(">>> Analysis stage updated: {Stage}", stage);
    }

    /// <summary>
    /// Marks a file as completed and records it in recent history.
    /// </summary>
    /// <param name="filePath">File path completed.</param>
    /// <param name="segmentCount">Number of detected segments for the file, or negative if not recorded.</param>
    public void CompleteFile(string filePath, int segmentCount = -1)
    {
        lock (_lock)
        {
            _recentlyScanned.Insert(0, filePath);
            if (_recentlyScanned.Count > MaxRecentFiles)
            {
                _recentlyScanned.RemoveAt(_recentlyScanned.Count - 1);
            }
        }

        if (segmentCount >= 0)
        {
            _logger.LogInformation(">>> Completed analysis for file: {File} (found {Count} segment(s))", filePath, segmentCount);
        }
        else
        {
            _logger.LogInformation(">>> Completed analysis for file: {File}", filePath);
        }
    }

    /// <summary>
    /// Marks the active scan as completed or stopped.
    /// </summary>
    /// <param name="finalStatus">Final status label ("Idle" or "Stopped").</param>
    public void FinishScan(string finalStatus = "Idle")
    {
        string? folder;
        lock (_lock)
        {
            folder = _currentFolder;
            _isScanning = false;
            _currentFolder = null;
            _currentFile = null;
            _currentSeries = null;
            _currentSeason = null;
            _currentEpisode = null;
            _currentStage = finalStatus;
            _fileIndex = 0;
            _totalFiles = 0;
        }

        _logger.LogInformation(">>> Finished scanning media folder/share: {Path} ({Status})", folder ?? "Unknown", finalStatus);
    }

    /// <summary>
    /// Captures a point-in-time snapshot of the current scan progress for API responses.
    /// </summary>
    /// <param name="queuedJobs">Count of jobs waiting in queue.</param>
    /// <returns>A progress snapshot.</returns>
    public ScanProgressSnapshot GetSnapshot(int queuedJobs)
    {
        lock (_lock)
        {
            var statusText = _isScanning
                ? (!string.IsNullOrWhiteSpace(_currentFile)
                    ? $"Scanning: {_currentStage} - {System.IO.Path.GetFileName(_currentFile)}"
                    : $"Scanning: {_currentStage}")
                : _currentStage;

            return new ScanProgressSnapshot(
                _isScanning,
                statusText,
                _currentFolder,
                _currentFile,
                _currentSeries,
                _currentSeason,
                _currentEpisode,
                _currentStage,
                _fileIndex,
                _totalFiles,
                queuedJobs,
                _recentlyScanned.ToList());
        }
    }
}

/// <summary>
/// Point-in-time snapshot of the media scanning worker progress.
/// </summary>
/// <param name="IsScanning">True if a scan is currently active.</param>
/// <param name="Status">Human-readable summary status text.</param>
/// <param name="CurrentFolder">Folder or share currently being processed.</param>
/// <param name="CurrentFile">File currently being probed or analyzed.</param>
/// <param name="CurrentSeries">Series name for the current file.</param>
/// <param name="CurrentSeason">Season number for the current file.</param>
/// <param name="CurrentEpisode">Episode number for the current file.</param>
/// <param name="CurrentStage">Detailed stage name.</param>
/// <param name="FileIndex">1-based file index within the active set.</param>
/// <param name="TotalFiles">Total files in the active set.</param>
/// <param name="QueuedJobs">Number of jobs waiting in the queue.</param>
/// <param name="RecentlyScannedFiles">List of recently completed file paths.</param>
public sealed record ScanProgressSnapshot(
    bool IsScanning,
    string Status,
    string? CurrentFolder,
    string? CurrentFile,
    string? CurrentSeries,
    int? CurrentSeason,
    int? CurrentEpisode,
    string CurrentStage,
    int FileIndex,
    int TotalFiles,
    int QueuedJobs,
    IReadOnlyList<string> RecentlyScannedFiles);
