// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

using System.Collections.Concurrent;

namespace IntroSkipper.Server.Scanning;

/// <summary>
/// Fast in-memory and persistent index matching media files and TV show metadata to stable Item GUIDs.
/// </summary>
public sealed class MediaFileIndex
{
    private readonly ConcurrentDictionary<Guid, ParsedEpisodeInfo> _byId = [];
    private readonly ConcurrentDictionary<string, Guid> _byPath = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Guid> _byMetadataKey = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Guid> _byFileName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Registers or updates a parsed episode in the index.
    /// </summary>
    /// <param name="episode">Episode information.</param>
    /// <returns>The stable ItemId GUID.</returns>
    public Guid Register(ParsedEpisodeInfo episode)
    {
        var id = ItemIdGenerator.FromPath(episode.FilePath);
        _byId[id] = episode;

        var normalizedPath = NormalizePath(episode.FilePath);
        _byPath[normalizedPath] = id;

        var metadataKey = BuildMetadataKey(episode.SeriesName, episode.SeasonNumber, episode.EpisodeNumber);
        _byMetadataKey[metadataKey] = id;

        var fileName = Path.GetFileName(episode.FilePath);
        _byFileName[fileName] = id;

        return id;
    }

    /// <summary>
    /// Attempts to find the ItemId GUID matching the given path, series/season/episode metadata, or file name.
    /// </summary>
    /// <param name="path">Optional file path.</param>
    /// <param name="series">Optional series title.</param>
    /// <param name="season">Optional season number.</param>
    /// <param name="episode">Optional episode number.</param>
    /// <param name="itemId">Resolved ItemId when found.</param>
    /// <returns>True if resolved; otherwise false.</returns>
    public bool TryResolve(string? path, string? series, int? season, int? episode, out Guid itemId)
    {
        // 1. Try exact metadata match (series, season, episode)
        if (!string.IsNullOrWhiteSpace(series) && season.HasValue && episode.HasValue)
        {
            var key = BuildMetadataKey(series, season.Value, episode.Value);
            if (_byMetadataKey.TryGetValue(key, out itemId))
            {
                return true;
            }

            // Fallback: If not indexed, generate deterministic GUID from metadata
            itemId = ItemIdGenerator.FromSeriesSeasonEpisode(series, season.Value, episode.Value);
            return true;
        }

        // 2. Try by file path
        if (!string.IsNullOrWhiteSpace(path))
        {
            var normalized = NormalizePath(path);
            if (_byPath.TryGetValue(normalized, out itemId))
            {
                return true;
            }

            // 3. Try matching by filename (e.g. if path was smb://... and server has /mnt/...)
            var fileName = Path.GetFileName(path.TrimEnd('/', '\\'));
            if (_byFileName.TryGetValue(fileName, out itemId))
            {
                return true;
            }

            // Fallback: generate deterministic GUID directly from path
            itemId = ItemIdGenerator.FromPath(path);
            return true;
        }

        itemId = Guid.Empty;
        return false;
    }

    /// <summary>
    /// Gets all indexed episodes grouped by (Series, Season).
    /// </summary>
    /// <returns>Lookup grouped by season.</returns>
    public ILookup<(string Series, int Season), ParsedEpisodeInfo> GetAllBySeason()
    {
        return _byId.Values.ToLookup(ep => (ep.SeriesName, ep.SeasonNumber));
    }

    private static string NormalizePath(string path)
    {
        return path.Trim().Replace('\\', '/').ToLowerInvariant();
    }

    private static string BuildMetadataKey(string series, int season, int episode)
    {
        return $"{series.Trim().ToLowerInvariant()}|S{season:D2}E{episode:D2}";
    }
}
