// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Data;

/// <summary>
/// Represents a configured media scan source path.
/// </summary>
/// <param name="Name">Friendly name of the media source.</param>
/// <param name="Path">Filesystem or network share path.</param>
/// <param name="Enabled">Whether this source is currently enabled for automatic scanning.</param>
/// <param name="ContentType">Kodi or library content type (e.g. "tvshows", "movies", "unset").</param>
/// <param name="IsKodiSource">Whether this source was discovered and synced from Kodi.</param>
public sealed record ScanSource(string Name, string Path, bool Enabled = false, string ContentType = "tvshows", bool IsKodiSource = false)
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ScanSource"/> class.
    /// </summary>
    public ScanSource()
        : this(string.Empty, string.Empty, false, "tvshows", false)
    {
    }
}
