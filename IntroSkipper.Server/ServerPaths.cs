// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Server;

/// <summary>
/// Resolves and manages application and userdata directories and subfolders.
/// </summary>
public sealed class ServerPaths
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ServerPaths"/> class.
    /// </summary>
    /// <param name="cliDataDir">Optional data directory specified via CLI argument.</param>
    public ServerPaths(string? cliDataDir = null)
    {
        BaseDataDir = ResolveBaseDataDir(cliDataDir);

        DbDirectory = Path.Combine(BaseDataDir, "db");
        SegmentDbPath = Path.Combine(DbDirectory, "introskipper.db");
        CacheDbPath = Path.Combine(DbDirectory, "introskipper-cache.db");

        LogsDirectory = Path.Combine(BaseDataDir, "logs");
        LogFilePath = Path.Combine(LogsDirectory, "server.log");

        ConfigDirectory = Path.Combine(BaseDataDir, "config");
        ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");

        AddonDirectory = PathDefaults.DefaultAddonDir;
        BinDirectory = Path.Combine(AddonDirectory, "bin");
    }

    /// <summary>
    /// Gets the base userdata directory.
    /// </summary>
    public string BaseDataDir { get; }

    /// <summary>
    /// Gets the database directory for SQLite database files.
    /// </summary>
    public string DbDirectory { get; }

    /// <summary>
    /// Gets the full path to the segment database.
    /// </summary>
    public string SegmentDbPath { get; }

    /// <summary>
    /// Gets the full path to the detection cache database.
    /// </summary>
    public string CacheDbPath { get; }

    /// <summary>
    /// Gets the directory for server log files.
    /// </summary>
    public string LogsDirectory { get; }

    /// <summary>
    /// Gets the full path to the server log file.
    /// </summary>
    public string LogFilePath { get; }

    /// <summary>
    /// Gets the configuration directory.
    /// </summary>
    public string ConfigDirectory { get; }

    /// <summary>
    /// Gets the full path to the configuration file.
    /// </summary>
    public string ConfigFilePath { get; }

    /// <summary>
    /// Gets the Kodi add-on installation directory.
    /// </summary>
    public string AddonDirectory { get; }

    /// <summary>
    /// Gets the add-on binary directory where executables reside.
    /// </summary>
    public string BinDirectory { get; }

    /// <summary>
    /// Ensures that the data directory and all subfolders (db, logs, config) exist.
    /// </summary>
    public void EnsureDirectoriesCreated()
    {
        Directory.CreateDirectory(BaseDataDir);
        Directory.CreateDirectory(DbDirectory);
        Directory.CreateDirectory(LogsDirectory);
        Directory.CreateDirectory(ConfigDirectory);
    }

    private static string ResolveBaseDataDir(string? cliDataDir)
    {
        if (!string.IsNullOrWhiteSpace(cliDataDir))
        {
            return Path.GetFullPath(cliDataDir);
        }

        var envDataDir = Environment.GetEnvironmentVariable("INTROSKIPPER_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(envDataDir))
        {
            return Path.GetFullPath(envDataDir);
        }

        var buildDefault = PathDefaults.DefaultDataDir;
        if (!string.IsNullOrWhiteSpace(buildDefault))
        {
            try
            {
                // Check if directory exists or if its parent exists/is creatable
                var full = Path.GetFullPath(buildDefault);
                var parent = Path.GetDirectoryName(full);
                if (Directory.Exists(full) || (!string.IsNullOrEmpty(parent) && Directory.Exists(parent)))
                {
                    return full;
                }
            }
            catch
            {
                // Fall back to base directory on invalid path format
            }
        }

        return AppContext.BaseDirectory;
    }
}
