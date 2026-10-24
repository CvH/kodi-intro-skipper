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
    bool KodiVfsConnected = false);

/// <summary>
/// Request to scan and analyze a directory.
/// </summary>
public sealed record ScanRequest(
    string Path,
    bool ExportEdl = false);
