// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Data;

/// <summary>
/// Data transfer object for a media segment.
/// </summary>
/// <param name="Id">Segment unique identifier.</param>
/// <param name="ItemId">Item (episode/movie) unique identifier.</param>
/// <param name="Type">Segment classification type.</param>
/// <param name="StartTicks">Start position in ticks.</param>
/// <param name="EndTicks">End position in ticks.</param>
public sealed record MediaSegmentDto(
    Guid Id,
    Guid ItemId,
    MediaSegmentType Type,
    long StartTicks,
    long EndTicks);
