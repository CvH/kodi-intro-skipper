// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-FileCopyrightText: 2026 AbandonedCart
// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Db;

/// <summary>
/// Single source of truth for the database file locations.
/// </summary>
internal static class IntroSkipperDatabasePaths
{
    /// <summary>
    /// Name of the data directory below base path.
    /// </summary>
    internal const string PluginDirectoryName = "introskipper";

    /// <summary>
    /// File name of the segment database.
    /// </summary>
    internal const string SegmentDatabaseFileName = "introskipper-v2.db";

    /// <summary>
    /// File name of the legacy segment database. Read once (read-only) by
    /// <see cref="LegacyDatabaseImporter"/> and never modified.
    /// </summary>
    internal const string LegacySegmentDatabaseFileName = "introskipper.db";

    /// <summary>
    /// File name of the detection cache database.
    /// </summary>
    internal const string DetectionCacheDatabaseFileName = "introskipper-cache.db";

    /// <summary>
    /// Returns the data directory, creating it when missing.
    /// </summary>
    /// <param name="baseDataPath">Base data path.</param>
    /// <returns>The data directory.</returns>
    internal static string GetPluginDirectory(string baseDataPath)
    {
        var directory = Path.Join(baseDataPath, PluginDirectoryName);
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>
    /// Returns the full path of the segment database, creating the containing directory when missing.
    /// </summary>
    /// <param name="baseDataPath">Base data path.</param>
    /// <returns>The segment database path.</returns>
    internal static string GetSegmentDatabasePath(string baseDataPath)
        => Path.Join(GetPluginDirectory(baseDataPath), SegmentDatabaseFileName);

    /// <summary>
    /// Returns the legacy segment database path that sits next to the given current
    /// segment database path.
    /// </summary>
    /// <param name="segmentDatabasePath">Path of the current segment database.</param>
    /// <returns>The legacy segment database path.</returns>
    internal static string GetLegacySegmentDatabasePath(string segmentDatabasePath)
        => Path.Join(Path.GetDirectoryName(segmentDatabasePath), LegacySegmentDatabaseFileName);

    /// <summary>
    /// Returns the full path of the detection cache database, creating the containing directory when missing.
    /// </summary>
    /// <param name="baseDataPath">Base data path.</param>
    /// <returns>The detection cache database path.</returns>
    internal static string GetDetectionCacheDatabasePath(string baseDataPath)
        => Path.Join(GetPluginDirectory(baseDataPath), DetectionCacheDatabaseFileName);
}
