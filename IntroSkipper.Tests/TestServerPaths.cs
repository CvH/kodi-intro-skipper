// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

using System;
using System.IO;
using IntroSkipper.Server;
using Xunit;

namespace IntroSkipper.Tests;

/// <summary>
/// Unit tests for <see cref="ServerPaths"/> and build-time path configuration.
/// </summary>
public sealed class TestServerPaths
{
    [Fact]
    public void Constructor_ExplicitDataDir_SetsSubfoldersCorrectly()
    {
        using var temp = new TempDirectory();
        var paths = new ServerPaths(temp.Info.FullName);

        Assert.Equal(Path.GetFullPath(temp.Info.FullName), paths.BaseDataDir);
        Assert.Equal(Path.Combine(paths.BaseDataDir, "db"), paths.DbDirectory);
        Assert.Equal(Path.Combine(paths.DbDirectory, "introskipper.db"), paths.SegmentDbPath);
        Assert.Equal(Path.Combine(paths.DbDirectory, "introskipper-cache.db"), paths.CacheDbPath);
        Assert.Equal(Path.Combine(paths.BaseDataDir, "logs"), paths.LogsDirectory);
        Assert.Equal(Path.Combine(paths.LogsDirectory, "server.log"), paths.LogFilePath);
        Assert.Equal(Path.Combine(paths.BaseDataDir, "config"), paths.ConfigDirectory);
        Assert.Equal(Path.Combine(paths.ConfigDirectory, "config.json"), paths.ConfigFilePath);
    }

    [Fact]
    public void EnsureDirectoriesCreated_CreatesDbLogsAndConfigFolders()
    {
        using var temp = new TempDirectory();
        var targetBase = Path.Combine(temp.Info.FullName, "userdata", "service.introskipper");
        var paths = new ServerPaths(targetBase);

        Assert.False(Directory.Exists(paths.DbDirectory));
        Assert.False(Directory.Exists(paths.LogsDirectory));
        Assert.False(Directory.Exists(paths.ConfigDirectory));

        paths.EnsureDirectoriesCreated();

        Assert.True(Directory.Exists(paths.BaseDataDir));
        Assert.True(Directory.Exists(paths.DbDirectory));
        Assert.True(Directory.Exists(paths.LogsDirectory));
        Assert.True(Directory.Exists(paths.ConfigDirectory));
    }

    [Fact]
    public void EnvironmentVariable_OverridesDefault()
    {
        using var temp = new TempDirectory();
        var envVar = "INTROSKIPPER_DATA_DIR";
        var original = Environment.GetEnvironmentVariable(envVar);
        try
        {
            Environment.SetEnvironmentVariable(envVar, temp.Info.FullName);
            var paths = new ServerPaths();

            Assert.Equal(Path.GetFullPath(temp.Info.FullName), paths.BaseDataDir);
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVar, original);
        }
    }

    [Fact]
    public void BuildDefaults_ExposeExpectedAddonIdAndDirs()
    {
        Assert.False(string.IsNullOrWhiteSpace(PathDefaults.DefaultAddonId));
        Assert.False(string.IsNullOrWhiteSpace(PathDefaults.DefaultAddonDir));
        Assert.False(string.IsNullOrWhiteSpace(PathDefaults.DefaultDataDir));

        Assert.Contains(PathDefaults.DefaultAddonId, PathDefaults.DefaultAddonDir, StringComparison.Ordinal);
        Assert.Contains(PathDefaults.DefaultAddonId, PathDefaults.DefaultDataDir, StringComparison.Ordinal);
    }
}
