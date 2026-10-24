// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

using IntroSkipper.Configuration;
using IntroSkipper.Data;
using IntroSkipper.FFmpeg;
using IntroSkipper.Server;
using IntroSkipper.Server.Analysis;
using IntroSkipper.Server.Configuration;
using IntroSkipper.Server.Kodi;
using IntroSkipper.Server.Logging;
using IntroSkipper.Server.Models;
using IntroSkipper.Server.Scanning;
using IntroSkipper.Server.Web;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add Windows Service and Linux systemd daemon support
builder.Services.AddSystemd();
builder.Services.AddWindowsService();

// Parse CLI flags and environment variables
var dataDir = Environment.GetEnvironmentVariable("INTROSKIPPER_DATA_DIR");
var ffmpegPath = Environment.GetEnvironmentVariable("INTROSKIPPER_FFMPEG_PATH");
var port = 8037;

for (var i = 0; i < args.Length; i++)
{
    if (args[i] == "--data-dir" && i + 1 < args.Length)
    {
        dataDir = args[++i];
    }
    else if (args[i] == "--ffmpeg-path" && i + 1 < args.Length)
    {
        ffmpegPath = args[++i];
    }
    else if (args[i] == "--port" && i + 1 < args.Length && int.TryParse(args[++i], out var p))
    {
        port = p;
    }
}

if (!string.IsNullOrWhiteSpace(ffmpegPath))
{
    FFmpegService.CustomFFmpegPath = ffmpegPath;
}

// Ensure server.log is written to data directory or current working directory
var logDir = dataDir ?? Directory.GetCurrentDirectory();
var logPath = Path.Combine(logDir, "server.log");
var fileLogger = new ServerFileLoggerProvider(logPath);
builder.Logging.AddProvider(fileLogger);
builder.Services.AddSingleton(fileLogger);

// Ensure Kestrel listens on configured port if no urls specified
if (builder.Configuration["urls"] is null && Environment.GetEnvironmentVariable("ASPNETCORE_URLS") is null)
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Register Services
builder.Services.AddSingleton(sp => new ServerStorage(dataDir, sp.GetRequiredService<ILoggerFactory>()));
builder.Services.AddSingleton(sp => sp.GetRequiredService<ServerStorage>().Database);
builder.Services.AddSingleton(sp => sp.GetRequiredService<ServerStorage>().CacheDatabase);
builder.Services.AddSingleton(sp => new DetectionCacheService(
    sp.GetRequiredService<ILogger<DetectionCacheService>>(),
    sp.GetRequiredService<ServerStorage>().CacheDatabase));

builder.Services.AddSingleton<KodiVfsBridge>();
builder.Services.AddSingleton<IFFmpegService, FFmpegService>();
builder.Services.AddSingleton<MediaScanner>();
builder.Services.AddSingleton<MediaFileIndex>();
builder.Services.AddSingleton<ServerConfigStore>();
builder.Services.AddSingleton(sp => sp.GetRequiredService<ServerConfigStore>().Current);
builder.Services.AddSingleton<ScanProgressTracker>();
builder.Services.AddSingleton<StandaloneAnalyzer>();
builder.Services.AddSingleton<AnalysisQueue>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AnalysisQueue>());

var app = builder.Build();

// Wire Kodi VFS path resolution and activity tracking into FFmpegService
var kodiBridge = app.Services.GetRequiredService<KodiVfsBridge>();
FFmpegService.PathResolver = kodiBridge.BuildVfsUrl;

var progressTracker = app.Services.GetRequiredService<ScanProgressTracker>();
FFmpegService.ActivityReporter = (path, activity) =>
{
    progressTracker.ReportActivity(path, activity);
};

// Initialize SQLite databases on startup
using (var scope = app.Services.CreateScope())
{
    var storage = scope.ServiceProvider.GetRequiredService<ServerStorage>();
    await storage.InitializeAsync().ConfigureAwait(false);
}

// Health check
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

// Server status
app.MapGet("/api/status", async (
    ServerStorage storage,
    IFFmpegService ffmpeg,
    AnalysisQueue queue,
    ScanProgressTracker tracker,
    KodiVfsBridge kodi,
    CancellationToken ct) =>
{
    var valid = await ffmpeg.CheckFFmpegVersionAsync(ct).ConfigureAwait(false);
    var version = ffmpeg.GetCheckResult().Outputs.FirstOrDefault(o => o.Name == "version")?.Output.Split('\n')[0].Trim() ?? "unknown";
    var kodiConnected = await kodi.PingAsync(ct).ConfigureAwait(false);
    var snapshot = tracker.GetSnapshot(queue.QueuedCount);

    return Results.Ok(new ServerStatusResponse(
        queue.Status,
        queue.QueuedCount,
        storage.DataDirectory,
        storage.SegmentDatabasePath,
        version,
        valid,
        kodi.BaseUrl,
        kodiConnected,
        snapshot.IsScanning,
        snapshot.CurrentFile,
        snapshot.CurrentStage,
        snapshot.FileIndex,
        snapshot.TotalFiles));
});

// Currently scanned files & progress status endpoints
app.MapGet("/api/scan/current", (ScanProgressTracker tracker, AnalysisQueue queue) =>
    Results.Ok(tracker.GetSnapshot(queue.QueuedCount)));

app.MapGet("/api/scan/status", (ScanProgressTracker tracker, AnalysisQueue queue) =>
    Results.Ok(tracker.GetSnapshot(queue.QueuedCount)));

// Segment query endpoint (used by Kodi addon)
app.MapGet("/api/segments", async (
    [FromQuery] string? path,
    [FromQuery] string? series,
    [FromQuery] int? season,
    [FromQuery] int? episode,
    MediaFileIndex index,
    ServerStorage storage,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(path) && string.IsNullOrWhiteSpace(series))
    {
        return Results.BadRequest(new { error = "Either 'path' or 'series' must be provided." });
    }

    if (!index.TryResolve(path, series, season, episode, out var itemId))
    {
        return Results.NotFound(new { error = "Media item not found in index." });
    }

    var segments = await storage.Database.GetSegmentsAsync(itemId, cancellationToken: ct).ConfigureAwait(false);
    var response = segments.Select(s => new SegmentResponse(
        s.Id,
        s.Type.ToString(),
        TickConversions.ToSeconds(s.StartTicks),
        TickConversions.ToSeconds(s.EndTicks),
        s.Source.ToString())).ToList();

    return Results.Ok(response);
});

// Start scan endpoint
var handleStartScan = (
    [FromBody] ScanRequest request,
    AnalysisQueue queue) =>
{
    if (string.IsNullOrWhiteSpace(request?.Path) || (!Directory.Exists(request.Path) && !request.Path.Contains("://")))
    {
        return Results.BadRequest(new { error = "Valid directory or network share path is required." });
    }

    queue.Enqueue(request.Path, request.ExportEdl);
    return Results.Accepted("/api/scan/status", new { message = "Scan enqueued", path = request.Path });
};

app.MapPost("/api/scan", handleStartScan);
app.MapPost("/api/scan/start", handleStartScan);

// Single item / episode scan endpoint
app.MapPost("/api/scan/item", async (
    [FromBody] ScanItemRequest request,
    MediaScanner scanner,
    MediaFileIndex fileIndex,
    StandaloneAnalyzer analyzer,
    AnalysisQueue queue,
    ServerStorage storage,
    CancellationToken ct) =>
{
    if (string.IsNullOrWhiteSpace(request?.Path))
    {
        return Results.BadRequest(new { error = "Path is required." });
    }

    var series = request.Series;
    var season = request.Season ?? 1;
    var episode = request.Episode ?? 1;
    var title = request.Title ?? string.Empty;

    if (string.IsNullOrWhiteSpace(series))
    {
        if (scanner.TryParseEpisode(request.Path, out var parsed) && parsed is not null)
        {
            series = parsed.SeriesName;
            season = parsed.SeasonNumber;
            episode = parsed.EpisodeNumber;
            title = parsed.Title;
        }
        else
        {
            series = Path.GetFileNameWithoutExtension(request.Path);
        }
    }

    var episodeInfo = new ParsedEpisodeInfo(request.Path, series, season, episode, title);
    fileIndex.Register(episodeInfo);

    if (request.Wait)
    {
        // Try to include sibling episodes from the same folder so Chromaprint cross-episode fingerprinting works
        var dir = Path.GetDirectoryName(request.Path);
        if (string.IsNullOrEmpty(dir) && request.Path.Contains('/'))
        {
            dir = request.Path[..request.Path.LastIndexOf('/')];
        }

        IReadOnlyList<ParsedEpisodeInfo> batch = [episodeInfo];
        if (!string.IsNullOrEmpty(dir))
        {
            try
            {
                var folderEpisodes = await scanner.ScanDirectoryAsync(dir, ct).ConfigureAwait(false);
                var sameSeason = folderEpisodes.Where(e => e.SeriesName.Equals(series, StringComparison.OrdinalIgnoreCase) && e.SeasonNumber == season).ToList();
                if (sameSeason.Count > 1)
                {
                    batch = sameSeason;
                    foreach (var ep in sameSeason)
                    {
                        fileIndex.Register(ep);
                    }
                }
            }
            catch
            {
                // Fall back to single episode
            }
        }

        await analyzer.AnalyzeSeasonAsync(batch, request.ExportEdl, ct).ConfigureAwait(false);

        var episodeId = ItemIdGenerator.FromPath(request.Path);
        var stored = await storage.Database.GetSegmentsAsync(episodeId, cancellationToken: ct).ConfigureAwait(false);
        var segResponses = stored.Select(s => new SegmentResponse(
            s.Id,
            s.Type.ToString(),
            TickConversions.ToSeconds(s.StartTicks),
            TickConversions.ToSeconds(s.EndTicks),
            s.Source.ToString())).ToList();

        return Results.Ok(new ScanItemResponse(
            request.Path,
            series,
            season,
            episode,
            segResponses,
            $"Analysis complete. Found {segResponses.Count} segment(s)."));
    }
    else
    {
        queue.Enqueue(request.Path, request.ExportEdl);
        return Results.Accepted("/api/scan/status", new ScanItemResponse(
            request.Path,
            series,
            season,
            episode,
            [],
            "Analysis enqueued."));
    }
});

// Stop scan endpoint
app.MapPost("/api/scan/stop", (AnalysisQueue queue) =>
{
    var stopped = queue.StopScan();
    return Results.Ok(new StopScanResponse(
        stopped ? "Scan stopped and queue cleared." : "No active scan was running.",
        stopped));
});

// Server log inspection endpoint
app.MapGet("/api/logs", (
    [FromQuery] int? lines,
    [FromQuery] bool? raw,
    ServerFileLoggerProvider loggerProvider) =>
{
    var count = Math.Clamp(lines ?? 100, 1, 1000);
    var recent = loggerProvider.ReadRecentLines(count);

    if (raw == true)
    {
        return Results.Text(string.Join(Environment.NewLine, recent), "text/plain; charset=utf-8");
    }

    return Results.Ok(new ServerLogsResponse(
        recent,
        recent.Count,
        loggerProvider.LogPath));
});

// Library / segment statistics endpoint
app.MapGet("/api/stats", async (
    ServerStorage storage,
    MediaFileIndex index,
    CancellationToken ct) =>
{
    var stats = await storage.Database.GetStatsAsync(ct).ConfigureAwait(false);
    var seconds = TickConversions.ToSeconds(stats.TotalTicks);

    var time = TimeSpan.FromSeconds(seconds);
    var formattedTime = time.TotalHours >= 1
        ? $"{(int)time.TotalHours}h {time.Minutes:D2}m {time.Seconds:D2}s"
        : $"{time.Minutes}m {time.Seconds:D2}s";

    var seriesCount = index.GetAllBySeason().Select(g => g.Key.Series).Distinct(StringComparer.OrdinalIgnoreCase).Count();
    if (seriesCount == 0 && stats.TotalItems > 0)
    {
        seriesCount = 1;
    }

    return Results.Ok(new ServerStatsResponse(
        seriesCount,
        stats.TotalItems,
        stats.TotalSegments,
        stats.IntroCount,
        stats.CreditsCount,
        stats.RecapCount,
        stats.PreviewCount,
        seconds,
        formattedTime));
});

// Configuration management endpoints
app.MapGet("/api/config", (ServerConfigStore configStore) => Results.Ok(configStore.Current));

app.MapPost("/api/config", (
    [FromBody] System.Text.Json.JsonElement payload,
    ServerConfigStore configStore) =>
{
    configStore.UpdateFromJson(payload);
    return Results.Ok(configStore.Current);
});

app.MapPost("/api/config/reset", (ServerConfigStore configStore) => Results.Ok(configStore.Reset()));

// Media scan source management endpoints
app.MapGet("/api/sources", (ServerConfigStore configStore) =>
    Results.Ok(configStore.GetScanSources()));

app.MapPost("/api/sources", (
    [FromBody] ScanSourceRequest request,
    ServerConfigStore configStore) =>
{
    if (string.IsNullOrWhiteSpace(request?.Path))
    {
        return Results.BadRequest(new { error = "Path is required." });
    }

    configStore.SetScanSource(request.Name ?? string.Empty, request.Path, request.Enabled ?? true, request.ContentType);
    return Results.Ok(configStore.GetScanSources());
});

app.MapPost("/api/sources/sync", (
    [FromBody] SyncSourcesRequest request,
    ServerConfigStore configStore) =>
{
    var incoming = (request?.Sources ?? [])
        .Where(s => !string.IsNullOrWhiteSpace(s.Path))
        .Select(s => new ScanSource(s.Name ?? string.Empty, s.Path, s.Enabled ?? true, s.ContentType ?? "tvshows"));

    var synced = configStore.SyncScanSources(incoming);
    return Results.Ok(synced);
});

app.MapDelete("/api/sources", (
    [FromQuery] string? path,
    ServerConfigStore configStore) =>
{
    if (string.IsNullOrWhiteSpace(path))
    {
        return Results.BadRequest(new { error = "Path query parameter is required." });
    }

    var removed = configStore.RemoveScanSource(path);
    return Results.Ok(new { path, removed });
});

// Kodi playback & client settings endpoints
app.MapGet("/api/kodi/settings", (ServerConfigStore configStore) =>
{
    var cfg = configStore.Current;
    return Results.Ok(new KodiSettingsDto(
        cfg.KodiIntroAction,
        cfg.KodiCreditsAction,
        cfg.KodiCreditsPromptDuration,
        cfg.KodiPathPrefixClient,
        cfg.KodiPathPrefixServer));
});

app.MapPost("/api/kodi/settings", (
    [FromBody] KodiSettingsDto settings,
    ServerConfigStore configStore) =>
{
    if (settings is null)
    {
        return Results.BadRequest(new { error = "Settings payload is required." });
    }

    var cfg = configStore.Current;
    if (!string.IsNullOrWhiteSpace(settings.IntroAction))
    {
        cfg.KodiIntroAction = settings.IntroAction;
    }

    if (!string.IsNullOrWhiteSpace(settings.CreditsAction))
    {
        cfg.KodiCreditsAction = settings.CreditsAction;
    }

    if (settings.CreditsPromptDuration > 0)
    {
        cfg.KodiCreditsPromptDuration = settings.CreditsPromptDuration;
    }

    cfg.KodiPathPrefixClient = settings.PathPrefixClient ?? string.Empty;
    cfg.KodiPathPrefixServer = settings.PathPrefixServer ?? string.Empty;

    configStore.Save();

    return Results.Ok(new KodiSettingsDto(
        cfg.KodiIntroAction,
        cfg.KodiCreditsAction,
        cfg.KodiCreditsPromptDuration,
        cfg.KodiPathPrefixClient,
        cfg.KodiPathPrefixServer));
});

// Interactive web dashboard & settings
app.MapGet("/", () => Results.Content(DashboardPage.GetHtml(), "text/html"));
app.MapGet("/settings", () => Results.Content(DashboardPage.GetHtml(), "text/html"));
app.MapGet("/fonts/inter.woff2", () => Results.Bytes(DashboardPage.GetFontBytes(), "font/woff2"));

await app.RunAsync().ConfigureAwait(false);
