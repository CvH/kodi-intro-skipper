// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

using System.Security.Cryptography;
using System.Text;

namespace IntroSkipper.Server.Scanning;

/// <summary>
/// Generates deterministic, stable GUIDs for files and episodes so identity is preserved across runs.
/// </summary>
public static class ItemIdGenerator
{
    /// <summary>
    /// Generates a deterministic GUID based on the canonical file path.
    /// </summary>
    /// <param name="filePath">Full path to the video file.</param>
    /// <returns>A deterministic GUID.</returns>
    public static Guid FromPath(string filePath)
    {
        var normalized = Path.GetFullPath(filePath).Trim().ToLowerInvariant().Replace('\\', '/');
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return new Guid(hash.AsSpan(0, 16));
    }

    /// <summary>
    /// Generates a deterministic GUID based on series name, season number, and episode number.
    /// </summary>
    /// <param name="series">Series name.</param>
    /// <param name="season">Season number.</param>
    /// <param name="episode">Episode number.</param>
    /// <returns>A deterministic GUID.</returns>
    public static Guid FromSeriesSeasonEpisode(string series, int season, int episode)
    {
        var normalized = $"{series.Trim().ToLowerInvariant()}|S{season:D2}E{episode:D2}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return new Guid(hash.AsSpan(0, 16));
    }

    /// <summary>
    /// Generates a deterministic GUID for a series by name.
    /// </summary>
    /// <param name="series">Series name.</param>
    /// <returns>A deterministic GUID.</returns>
    public static Guid FromSeries(string series)
    {
        var normalized = $"series|{series.Trim().ToLowerInvariant()}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return new Guid(hash.AsSpan(0, 16));
    }

    /// <summary>
    /// Generates a deterministic GUID for a season.
    /// </summary>
    /// <param name="series">Series name.</param>
    /// <param name="season">Season number.</param>
    /// <returns>A deterministic GUID.</returns>
    public static Guid FromSeason(string series, int season)
    {
        var normalized = $"season|{series.Trim().ToLowerInvariant()}|{season}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return new Guid(hash.AsSpan(0, 16));
    }
}
