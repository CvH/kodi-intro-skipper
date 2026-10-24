// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

using IntroSkipper.Configuration;
using IntroSkipper.Data;
using IntroSkipper.FFmpeg;
using IntroSkipper.Server;
using IntroSkipper.Server.Analysis;
using IntroSkipper.Server.Models;
using IntroSkipper.Server.Scanning;
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

builder.Services.AddSingleton<IFFmpegService, FFmpegService>();
builder.Services.AddSingleton<MediaScanner>();
builder.Services.AddSingleton<MediaFileIndex>();
builder.Services.AddSingleton(new PluginConfiguration());
builder.Services.AddSingleton<StandaloneAnalyzer>();
builder.Services.AddSingleton<AnalysisQueue>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<AnalysisQueue>());

var app = builder.Build();

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
    CancellationToken ct) =>
{
    var valid = await ffmpeg.CheckFFmpegVersionAsync(ct).ConfigureAwait(false);
    var version = ffmpeg.GetCheckResult().Outputs.FirstOrDefault(o => o.Name == "version")?.Output.Split('\n')[0].Trim() ?? "unknown";

    return Results.Ok(new ServerStatusResponse(
        queue.Status,
        queue.QueuedCount,
        storage.DataDirectory,
        storage.SegmentDatabasePath,
        version,
        valid));
});

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

// Enqueue directory scan endpoint
app.MapPost("/api/scan", (
    [FromBody] ScanRequest request,
    AnalysisQueue queue) =>
{
    if (string.IsNullOrWhiteSpace(request.Path) || !Directory.Exists(request.Path))
    {
        return Results.BadRequest(new { error = "Valid directory path is required." });
    }

    queue.Enqueue(request.Path, request.ExportEdl);
    return Results.Accepted("/api/status", new { message = "Scan enqueued", path = request.Path });
});

// Minimal landing page
app.MapGet("/", (ServerStorage storage) => Results.Content(
    $"""
    <!DOCTYPE html>
    <html>
    <head><title>IntroSkipper Standalone Server</title></head>
    <body style="font-family: sans-serif; max-width: 800px; margin: 40px auto; line-height: 1.6;">
        <h1>IntroSkipper Standalone Server</h1>
        <p>Service is active and listening for Kodi addon requests.</p>
        <ul>
            <li><strong>Database:</strong> {storage.SegmentDatabasePath}</li>
            <li><strong>Status:</strong> <a href="/api/status">/api/status</a></li>
            <li><strong>Segments API:</strong> <code>/api/segments?path=...&series=...&season=1&episode=1</code></li>
        </ul>
    </body>
    </html>
    """, "text/html"));

await app.RunAsync().ConfigureAwait(false);
