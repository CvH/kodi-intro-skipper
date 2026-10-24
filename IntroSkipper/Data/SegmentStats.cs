// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Data;

/// <summary>
/// Aggregated statistics of stored segments.
/// </summary>
/// <param name="TotalItems">Number of unique media items with at least one segment.</param>
/// <param name="TotalSegments">Total active segments count.</param>
/// <param name="IntroCount">Total Introduction segments count.</param>
/// <param name="CreditsCount">Total Credits segments count.</param>
/// <param name="RecapCount">Total Recap segments count.</param>
/// <param name="PreviewCount">Total Preview segments count.</param>
/// <param name="TotalTicks">Total duration of skipped content in ticks.</param>
public sealed record SegmentStats(
    int TotalItems,
    int TotalSegments,
    int IntroCount,
    int CreditsCount,
    int RecapCount,
    int PreviewCount,
    long TotalTicks);
