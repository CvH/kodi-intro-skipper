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
public sealed record ScanSource(string Name, string Path, bool Enabled, string ContentType = "tvshows");
