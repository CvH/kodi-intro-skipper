// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Data;

/// <summary>
/// Represents chapter metadata for a media file.
/// </summary>
public sealed class ChapterInfo
{
    /// <summary>
    /// Gets or sets chapter name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets chapter start position in ticks (100-nanosecond units).
    /// </summary>
    public long StartPositionTicks { get; set; }
}
