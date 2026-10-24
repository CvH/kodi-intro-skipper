// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Server.Models;

/// <summary>
/// A segment interval returned to clients like Kodi.
/// </summary>
/// <param name="Id">Segment unique identifier.</param>
/// <param name="Type">Segment type ("Introduction", "Credits", "Recap", "Preview").</param>
/// <param name="Start">Start timestamp in seconds.</param>
/// <param name="End">End timestamp in seconds.</param>
/// <param name="Source">Source that detected the segment ("Chapter", "Fingerprint", "Keyframe", "User").</param>
public sealed record SegmentResponse(
    Guid Id,
    string Type,
    double Start,
    double End,
    string Source);

/// <summary>
/// Server status response.
/// </summary>
public sealed record ServerStatusResponse(
    string Status,
    int QueuedJobs,
    string DataDirectory,
    string SegmentDatabasePath,
    string FFmpegVersion,
    bool FFmpegValid,
    string? KodiVfsUrl = null,
    bool KodiVfsConnected = false,
    bool IsScanning = false,
    string? CurrentFile = null,
    string? CurrentStage = null,
    int? FileIndex = null,
    int? TotalFiles = null);

/// <summary>
/// Request to scan and analyze a directory.
/// </summary>
/// <param name="Path">Directory or network share path to scan.</param>
/// <param name="ExportEdl">Whether to generate EDL files alongside media.</param>
public sealed record ScanRequest(
    string? Path = null,
    bool ExportEdl = false);

/// <summary>
/// Response returned from stopping an active scan.
/// </summary>
/// <param name="Message">Human-readable message.</param>
/// <param name="Stopped">True if an active scan was stopped, false if already idle.</param>
public sealed record StopScanResponse(
    string Message,
    bool Stopped);

/// <summary>
/// Response returned for server log inspection.
/// </summary>
/// <param name="Lines">Recent log lines.</param>
/// <param name="TotalLines">Total count of returned lines.</param>
/// <param name="LogPath">Path to server.log file.</param>
public sealed record ServerLogsResponse(
    IReadOnlyList<string> Lines,
    int TotalLines,
    string LogPath);

/// <summary>
/// Request to scan and analyze a single item / episode on demand.
/// </summary>
/// <param name="Path">Full file path or network URI of the video.</param>
/// <param name="Series">Series title (optional).</param>
/// <param name="Season">Season number (optional).</param>
/// <param name="Episode">Episode number (optional).</param>
/// <param name="Title">Episode title (optional).</param>
/// <param name="ExportEdl">Whether to generate EDL file.</param>
/// <param name="Wait">Whether to wait for analysis completion and return detected segments.</param>
public sealed record ScanItemRequest(
    string Path,
    string? Series = null,
    int? Season = null,
    int? Episode = null,
    string? Title = null,
    bool ExportEdl = false,
    bool Wait = true);

/// <summary>
/// Response returned from scanning a single item on demand.
/// </summary>
/// <param name="Path">Target file path.</param>
/// <param name="Series">Series name.</param>
/// <param name="Season">Season number.</param>
/// <param name="Episode">Episode number.</param>
/// <param name="Segments">Detected segments for this item.</param>
/// <param name="Message">Summary message.</param>
public sealed record ScanItemResponse(
    string Path,
    string Series,
    int Season,
    int Episode,
    IReadOnlyList<SegmentResponse> Segments,
    string Message);

/// <summary>
/// Aggregated statistics response.
/// </summary>
/// <param name="TotalSeries">Total unique series indexed.</param>
/// <param name="TotalEpisodes">Total unique episodes with detected segments.</param>
/// <param name="TotalSegments">Total active detected segments count.</param>
/// <param name="IntroCount">Total Introduction segments count.</param>
/// <param name="CreditsCount">Total Credits segments count.</param>
/// <param name="RecapCount">Total Recap segments count.</param>
/// <param name="PreviewCount">Total Preview segments count.</param>
/// <param name="TotalSkippedSeconds">Total skipped duration in seconds.</param>
/// <param name="FormattedSkippedTime">Formatted skipped time (e.g. "1h 23m 45s").</param>
public sealed record ServerStatsResponse(
    int TotalSeries,
    int TotalEpisodes,
    int TotalSegments,
    int IntroCount,
    int CreditsCount,
    int RecapCount,
    int PreviewCount,
    double TotalSkippedSeconds,
    string FormattedSkippedTime);

/// <summary>
/// Request to add, update, or remove a scan source.
/// </summary>
/// <param name="Name">Friendly name of the source.</param>
/// <param name="Path">Filesystem or network share path.</param>
/// <param name="Enabled">Whether the source is enabled.</param>
/// <param name="ContentType">Kodi content type (e.g. "tvshows", "movies", "unset").</param>
public sealed record ScanSourceRequest(
    string? Name = null,
    string Path = "",
    bool? Enabled = null,
    string? ContentType = null);

/// <summary>
/// Request to synchronize multiple scan sources.
/// </summary>
/// <param name="Sources">Collection of scan sources to synchronize.</param>
public sealed record SyncSourcesRequest(
    IReadOnlyList<ScanSourceRequest> Sources);

/// <summary>
/// Kodi playback and client integration settings.
/// </summary>
/// <param name="IntroAction">Action when intro is encountered (prompt, autoskip, disabled).</param>
/// <param name="CreditsAction">Action when credits are encountered (prompt, autoskip, nextEpisode, disabled).</param>
/// <param name="CreditsPromptDuration">Duration in seconds to display credits prompt.</param>
/// <param name="PathPrefixClient">Client-side path prefix mapping.</param>
/// <param name="PathPrefixServer">Server-side path prefix mapping.</param>
public sealed record KodiSettingsDto(
    string IntroAction,
    string CreditsAction,
    int CreditsPromptDuration,
    string PathPrefixClient,
    string PathPrefixServer);
