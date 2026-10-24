// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace IntroSkipper.Server.Scanning;

/// <summary>
/// Discovers media files on the filesystem and parses them into series and season structures.
/// </summary>
public sealed partial class MediaScanner(ILogger<MediaScanner> logger)
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".mkv", ".mp4", ".avi", ".m4v", ".ts", ".mov", ".wmv", ".webm", ".flv", ".strm"
    };

    [GeneratedRegex(@"^(?<series>.*?)[.\s_-]+[sS](?<season>\d{1,4})[eE](?<episode>\d{1,4})(?:[.\s_-]+(?<title>.*))?$", RegexOptions.IgnoreCase)]
    private static partial Regex StandardEpisodePattern();

    [GeneratedRegex(@"^(?<series>.*?)[.\s_-]+(?<season>\d{1,4})x(?<episode>\d{1,4})(?:[.\s_-]+(?<title>.*))?$", RegexOptions.IgnoreCase)]
    private static partial Regex XEpisodePattern();

    [GeneratedRegex(@"[sS](?<season>\d{1,4})[eE](?<episode>\d{1,4})", RegexOptions.IgnoreCase)]
    private static partial Regex EmbeddedEpisodePattern();

    [GeneratedRegex(@"Season\s*(?<season>\d{1,4})", RegexOptions.IgnoreCase)]
    private static partial Regex SeasonFolderPattern();

    [GeneratedRegex(@"^(?:e|ep|episode)?\s*(?<episode>\d{1,4})(?:[.\s_-]+(?<title>.*))?$", RegexOptions.IgnoreCase)]
    private static partial Regex NumericEpisodePattern();

    /// <summary>
    /// Scans a directory recursively for video files and parses their episode information.
    /// </summary>
    /// <param name="directoryPath">Directory path to scan.</param>
    /// <returns>A list of parsed episode information records.</returns>
    public IReadOnlyList<ParsedEpisodeInfo> ScanDirectory(string directoryPath)
    {
        if (!Directory.Exists(directoryPath))
        {
            logger.LogWarning("Directory does not exist: {Path}", directoryPath);
            return [];
        }

        var results = new List<ParsedEpisodeInfo>();
        try
        {
            foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories))
            {
                var extension = Path.GetExtension(filePath);
                if (!SupportedExtensions.Contains(extension))
                {
                    continue;
                }

                if (TryParseEpisode(filePath, out var episodeInfo) && episodeInfo is not null)
                {
                    results.Add(episodeInfo);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to scan directory {Path}", directoryPath);
        }

        return results;
    }

    /// <summary>
    /// Tries to parse a single media file path into episode metadata.
    /// </summary>
    /// <param name="filePath">File path.</param>
    /// <param name="episode">Parsed episode info when successful.</param>
    /// <returns>True if the file was recognized as an episode; otherwise false.</returns>
    public bool TryParseEpisode(string filePath, out ParsedEpisodeInfo? episode)
    {
        episode = null;
        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;

        // Try standard Show - S01E02 - Title
        var match = StandardEpisodePattern().Match(fileNameWithoutExt);
        if (match.Success)
        {
            var series = CleanName(match.Groups["series"].Value);
            if (string.IsNullOrWhiteSpace(series))
            {
                series = DetectSeriesFromParent(directory);
            }

            var season = int.Parse(match.Groups["season"].ValueSpan, CultureInfo.InvariantCulture);
            var epNum = int.Parse(match.Groups["episode"].ValueSpan, CultureInfo.InvariantCulture);
            var title = CleanName(match.Groups["title"].Value);

            episode = new ParsedEpisodeInfo(filePath, series, season, epNum, string.IsNullOrWhiteSpace(title) ? fileNameWithoutExt : title);
            return true;
        }

        // Try 1x02 pattern
        match = XEpisodePattern().Match(fileNameWithoutExt);
        if (match.Success)
        {
            var series = CleanName(match.Groups["series"].Value);
            if (string.IsNullOrWhiteSpace(series))
            {
                series = DetectSeriesFromParent(directory);
            }

            var season = int.Parse(match.Groups["season"].ValueSpan, CultureInfo.InvariantCulture);
            var epNum = int.Parse(match.Groups["episode"].ValueSpan, CultureInfo.InvariantCulture);
            var title = CleanName(match.Groups["title"].Value);

            episode = new ParsedEpisodeInfo(filePath, series, season, epNum, string.IsNullOrWhiteSpace(title) ? fileNameWithoutExt : title);
            return true;
        }

        // Try embedded S01E02 anywhere in file name
        match = EmbeddedEpisodePattern().Match(fileNameWithoutExt);
        if (match.Success)
        {
            var series = DetectSeriesFromParent(directory);
            var season = int.Parse(match.Groups["season"].ValueSpan, CultureInfo.InvariantCulture);
            var epNum = int.Parse(match.Groups["episode"].ValueSpan, CultureInfo.InvariantCulture);

            episode = new ParsedEpisodeInfo(filePath, series, season, epNum, fileNameWithoutExt);
            return true;
        }

        // Try folder structure: Show/Season 1/02.mkv
        var dirSeasonMatch = SeasonFolderPattern().Match(directory);
        if (dirSeasonMatch.Success)
        {
            var season = int.Parse(dirSeasonMatch.Groups["season"].ValueSpan, CultureInfo.InvariantCulture);
            var numMatch = NumericEpisodePattern().Match(fileNameWithoutExt);
            if (numMatch.Success)
            {
                var epNum = int.Parse(numMatch.Groups["episode"].ValueSpan, CultureInfo.InvariantCulture);
                var series = DetectSeriesFromGrandparent(directory);
                episode = new ParsedEpisodeInfo(filePath, series, season, epNum, fileNameWithoutExt);
                return true;
            }
        }

        return false;
    }

    private static string DetectSeriesFromParent(string directory)
    {
        var parent = Path.GetFileName(directory);
        if (SeasonFolderPattern().IsMatch(parent))
        {
            return Path.GetFileName(Path.GetDirectoryName(directory) ?? string.Empty);
        }

        return parent;
    }

    private static string DetectSeriesFromGrandparent(string directory)
    {
        var parentDir = Path.GetDirectoryName(directory);
        return string.IsNullOrEmpty(parentDir) ? Path.GetFileName(directory) : Path.GetFileName(parentDir);
    }

    private static string CleanName(string name)
    {
        return name.Replace('.', ' ').Replace('_', ' ').Trim();
    }
}
