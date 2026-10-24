// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Server.Scanning;

/// <summary>
/// Parsed metadata for an episode video file.
/// </summary>
/// <param name="FilePath">Full path to the media file on disk.</param>
/// <param name="SeriesName">Name of the TV series.</param>
/// <param name="SeasonNumber">Season number.</param>
/// <param name="EpisodeNumber">Episode number.</param>
/// <param name="Title">Episode title or file name.</param>
public sealed record ParsedEpisodeInfo(
    string FilePath,
    string SeriesName,
    int SeasonNumber,
    int EpisodeNumber,
    string Title);
