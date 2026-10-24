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

