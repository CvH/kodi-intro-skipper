// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

using System.Globalization;
using IntroSkipper.Analyzers;
using IntroSkipper.Analyzers.Credits;
using IntroSkipper.Configuration;
using IntroSkipper.Data;
using IntroSkipper.Db;
using IntroSkipper.FFmpeg;
using IntroSkipper.Helper;
using IntroSkipper.Server.Scanning;
using Microsoft.Extensions.Logging;

namespace IntroSkipper.Server.Analysis;

/// <summary>
/// Orchestrates the detection pipeline (Chapters, Chromaprint, CreditsPass) over video files outside Jellyfin.
/// </summary>
public sealed class StandaloneAnalyzer(
    ILoggerFactory loggerFactory,
    IFFmpegService ffmpegService,
    DetectionCacheService cacheService,
    IntroSkipperDatabase database,
    PluginConfiguration config,
    ScanProgressTracker progressTracker)
{
    private readonly ILogger<StandaloneAnalyzer> _logger = loggerFactory.CreateLogger<StandaloneAnalyzer>();
    private readonly ILoggerFactory _loggerFactory = loggerFactory;
    private readonly IFFmpegService _ffmpegService = ffmpegService;
    private readonly DetectionCacheService _cacheService = cacheService;
    private readonly IntroSkipperDatabase _database = database;
    private readonly PluginConfiguration _config = config;
    private readonly ScanProgressTracker _progressTracker = progressTracker;

    /// <summary>
    /// Analyzes a season of episodes, detecting introduction and credit sequences.
    /// </summary>
    /// <param name="episodes">Parsed episodes belonging to the season.</param>
    /// <param name="exportEdl">Whether to generate Kodi EDL files alongside the media.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the analysis pass.</returns>
    public async Task AnalyzeSeasonAsync(
        IReadOnlyList<ParsedEpisodeInfo> episodes,
        bool exportEdl = false,
        CancellationToken cancellationToken = default)
    {
        if (episodes.Count == 0)
        {
            return;
        }

        var seriesName = episodes[0].SeriesName;
        var seasonNum = episodes[0].SeasonNumber;
        _logger.LogInformation("Starting analysis for {Series} Season {Season} ({Count} episodes)", seriesName, seasonNum, episodes.Count);

        var ffmpegValid = await _ffmpegService.CheckFFmpegVersionAsync(cancellationToken).ConfigureAwait(false);
        var introHash = ConfigHasher.Analysis(_config, AnalysisMode.Introduction, AnalyzerAction.Default, ffmpegValid, chapterSource: ChapterSource.Embedded);
        var creditsHash = ConfigHasher.Analysis(_config, AnalysisMode.Credits, AnalyzerAction.Default, ffmpegValid, chapterSource: ChapterSource.Embedded);

        var queuedEpisodes = new List<QueuedEpisode>();
        var seriesId = ItemIdGenerator.FromSeries(seriesName);
        var seasonId = ItemIdGenerator.FromSeason(seriesName, seasonNum);

        var orderedEpisodes = episodes.OrderBy(e => e.EpisodeNumber).ToList();
        for (var i = 0; i < orderedEpisodes.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = orderedEpisodes[i];
            _progressTracker.UpdateFile(file.FilePath, seriesName, seasonNum, file.EpisodeNumber, "Probing media & chapters", i + 1, orderedEpisodes.Count);

            var episodeId = ItemIdGenerator.FromPath(file.FilePath);
            var (duration, chapters) = await ProbeMediaAsync(file.FilePath, episodeId, cancellationToken).ConfigureAwait(false);

            if (duration <= 0)
            {
                _logger.LogWarning("Skipping {File}: duration probe failed", file.FilePath);
                continue;
            }

            var queued = new QueuedEpisode
            {
                EpisodeId = episodeId,
                SeasonId = seasonId,
                SeriesId = seriesId,
                Name = file.Title,
                SeriesName = seriesName,
                SeasonNumber = seasonNum,
                EpisodeNumber = file.EpisodeNumber,
                Path = file.FilePath,
                Duration = duration,
                Chapters = chapters,
                ChaptersSource = ChapterSource.Embedded,
                Category = QueuedMediaCategory.Episode,
                IntroFingerprintEnd = Math.Min(duration >= 5 * 60 ? duration * _config.AnalysisPercent / 100.0 : duration, 60 * _config.AnalysisLengthLimit),
                CreditsFingerprintStart = Math.Max(0, duration - _config.MaximumCreditsDuration),
                CreditsFingerprintEnd = duration,
                AnalysisConfigHash = introHash,
            };

            queuedEpisodes.Add(queued);
        }

        if (queuedEpisodes.Count == 0)
        {
            _logger.LogWarning("No readable episodes in season {Series} S{Season}", seriesName, seasonNum);
            return;
        }

        try
        {
            // 1. Chapter Analyzer for Intro
            _progressTracker.UpdateStage("Chapter analysis");
            var chapterAnalyzer = new ChapterAnalyzer(
                _loggerFactory.CreateLogger<ChapterAnalyzer>(),
                _ffmpegService,
                _database,
                _config,
                enableChapterDetection: true,
                enableRecapBlackFrameFallback: false);

            var remainingIntro = await chapterAnalyzer.AnalyzeMediaFiles(queuedEpisodes, AnalysisMode.Introduction, cancellationToken).ConfigureAwait(false);

            // 2. Chromaprint Audio Fingerprint Analyzer for Intro (across the season)
            if (ffmpegValid && remainingIntro.Count > 0)
            {
                _progressTracker.UpdateStage("Chromaprint audio fingerprinting");
                var chromaprintAnalyzer = new ChromaprintAnalyzer(
                    _loggerFactory.CreateLogger<ChromaprintAnalyzer>(),
                    _ffmpegService,
                    _cacheService,
                    _database,
                    _config);

                await chromaprintAnalyzer.AnalyzeMediaFiles(remainingIntro, AnalysisMode.Introduction, cancellationToken).ConfigureAwait(false);
            }

            // 3. Credits Pass (Keyframe Analyzer + Chapter combination)
            _progressTracker.UpdateStage("Credits analysis (keyframe & black frame scan)");
            var creditsPass = new CreditsPass(_loggerFactory, _ffmpegService, _cacheService, _database, _config);
            await creditsPass.RunAsync(queuedEpisodes, AnalyzerAction.Default, ffmpegValid, cancellationToken).ConfigureAwait(false);

            // Mark analyzed items and optionally export EDL
            _progressTracker.UpdateStage("Saving detected segments");
            await _database.MarkItemsAnalyzedAsync(
                AnalysisMode.Introduction,
                queuedEpisodes.Select(e => (e.EpisodeId, (long?)File.GetLastWriteTimeUtc(e.Path).Ticks)),
                introHash,
                cancellationToken).ConfigureAwait(false);

            await _database.MarkItemsAnalyzedAsync(
                AnalysisMode.Credits,
                queuedEpisodes.Select(e => (e.EpisodeId, (long?)File.GetLastWriteTimeUtc(e.Path).Ticks)),
                creditsHash,
                cancellationToken).ConfigureAwait(false);

            if (exportEdl)
            {
                _progressTracker.UpdateStage("Exporting EDL files");
                foreach (var episode in queuedEpisodes)
                {
                    await ExportEdlAsync(episode.EpisodeId, episode.Path, cancellationToken).ConfigureAwait(false);
                }
            }

            for (var i = 0; i < queuedEpisodes.Count; i++)
            {
                var ep = queuedEpisodes[i];
                var segments = await _database.GetSegmentsAsync(ep.EpisodeId, cancellationToken: cancellationToken).ConfigureAwait(false);
                _progressTracker.CompleteFile(ep.Path, segments.Count);
            }

            _logger.LogInformation("Successfully analyzed {Series} Season {Season}", seriesName, seasonNum);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Analysis failed for {Series} Season {Season}", seriesName, seasonNum);
        }
    }

    /// <summary>
    /// Analyzes a single episode on demand and returns its detected segment entities.
    /// </summary>
    /// <param name="episodeInfo">Parsed episode info.</param>
    /// <param name="exportEdl">Whether to generate Kodi EDL files alongside the media.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A list of detected segment entities for the episode.</returns>
    public async Task<IReadOnlyList<DbSegment>> AnalyzeItemAsync(
        ParsedEpisodeInfo episodeInfo,
        bool exportEdl = false,
        CancellationToken cancellationToken = default)
    {
        await AnalyzeSeasonAsync([episodeInfo], exportEdl, cancellationToken).ConfigureAwait(false);

        var episodeId = ItemIdGenerator.FromPath(episodeInfo.FilePath);
        return await _database.GetSegmentsAsync(episodeId, cancellationToken: cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Exports detected segments into a Kodi-compatible .edl file next to the video.
    /// </summary>
    /// <param name="episodeId">Episode Item ID.</param>
    /// <param name="filePath">Full path to the video file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the export operation.</returns>
    public async Task ExportEdlAsync(Guid episodeId, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            var segments = await _database.GetSegmentsAsync(episodeId, cancellationToken: cancellationToken).ConfigureAwait(false);
            if (segments.Count == 0)
            {
                return;
            }

            var edlPath = Path.ChangeExtension(filePath, ".edl");
            using var writer = new StreamWriter(edlPath, append: false);
            foreach (var segment in segments.OrderBy(s => s.StartTicks))
            {
                var startSec = TickConversions.ToSeconds(segment.StartTicks);
                var endSec = TickConversions.ToSeconds(segment.EndTicks);
                // Action 0 = Cut / Automatic skip in Kodi
                await writer.WriteLineAsync(string.Create(CultureInfo.InvariantCulture, $"{startSec:F2} {endSec:F2} 0")).ConfigureAwait(false);
            }

            _logger.LogDebug("Exported EDL file: {Path}", edlPath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write EDL file for {Path}", filePath);
        }
    }

    private async Task<(double Duration, IReadOnlyList<MediaBrowser.Model.Entities.ChapterInfo> Chapters)> ProbeMediaAsync(
        string filePath,
        Guid episodeId,
        CancellationToken cancellationToken)
    {
        var duration = 0.0;
        try
        {
            var audioDuration = await _ffmpegService.ProbeAudioDurationAsync(filePath, cancellationToken).ConfigureAwait(false);
            if (audioDuration.HasValue && audioDuration.Value > 0)
            {
                duration = audioDuration.Value;
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to probe duration for {File}", filePath);
        }

        var tempEpisode = new QueuedEpisode
        {
            EpisodeId = episodeId,
            Path = filePath,
            Duration = duration,
        };

        var chapters = await _ffmpegService.GetEmbeddedChaptersAsync(tempEpisode, cancellationToken).ConfigureAwait(false) ?? [];

        // Fallback only if duration probe completely failed but chapters are present
        if (duration <= 0 && chapters.Length > 0)
        {
            duration = TimeSpan.FromTicks(chapters[^1].StartPositionTicks).TotalSeconds;
        }

        return (duration, chapters);
    }
}
