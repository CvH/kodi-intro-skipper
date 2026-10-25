// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

using System.IO;
using System.Linq;
using IntroSkipper.Data;
using IntroSkipper.Server;
using IntroSkipper.Server.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace IntroSkipper.Tests;

/// <summary>
/// Unit tests for <see cref="ServerConfigStore"/> scan source synchronization,
/// default disabled behavior, and Kodi source protection.
/// </summary>
public sealed class TestScanSources
{
    [Fact]
    public void SyncScanSources_NewSources_DefaultDisabledAndMarkedAsKodi()
    {
        using var temp = new TempDirectory();
        var paths = new ServerPaths(temp.Info.FullName);
        var store = new ServerConfigStore(paths, NullLogger<ServerConfigStore>.Instance);

        var incoming = new[]
        {
            new ScanSource("TV Shows", "/storage/tvshows/", Enabled: true, "tvshows", IsKodiSource: true),
            new ScanSource("Movies", "smb://NAS/Movies/", Enabled: true, "movies", IsKodiSource: true),
        };

        var synced = store.SyncScanSources(incoming);

        Assert.Equal(2, synced.Count);
        Assert.All(synced, s => Assert.False(s.Enabled));
        Assert.All(synced, s => Assert.True(s.IsKodiSource));
    }

    [Fact]
    public void SyncScanSources_ExistingSources_PreserveUserEnabledState()
    {
        using var temp = new TempDirectory();
        var paths = new ServerPaths(temp.Info.FullName);
        var store = new ServerConfigStore(paths, NullLogger<ServerConfigStore>.Instance);

        var incomingInitial = new[]
        {
            new ScanSource("TV Shows", "/storage/tvshows/", Enabled: false, "tvshows", IsKodiSource: true),
        };
        store.SyncScanSources(incomingInitial);

        // User manually enables the source
        store.SetScanSource("TV Shows", "/storage/tvshows/", enabled: true, "tvshows");
        Assert.True(store.GetScanSources().Single(s => s.Path == "/storage/tvshows/").Enabled);

        // Subsequent sync occurs from Kodi
        var incomingNext = new[]
        {
            new ScanSource("TV Shows", "/storage/tvshows/", Enabled: false, "tvshows", IsKodiSource: true),
        };
        var resynced = store.SyncScanSources(incomingNext);

        Assert.True(resynced.Single(s => s.Path == "/storage/tvshows/").Enabled);
    }

    [Fact]
    public void RemoveScanSource_KodiSourcesCannotBeRemoved_CustomSourcesCanBeRemoved()
    {
        using var temp = new TempDirectory();
        var paths = new ServerPaths(temp.Info.FullName);
        var store = new ServerConfigStore(paths, NullLogger<ServerConfigStore>.Instance);

        // Synced from Kodi
        store.SyncScanSources([new ScanSource("TV Shows", "/storage/tvshows/", Enabled: false, "tvshows", IsKodiSource: true)]);

        // Custom manually added path
        store.SetScanSource("Custom Share", "/mnt/media/custom", enabled: true, "tvshows");

        // Attempting to remove Kodi source returns false and does not remove it
        var kodiRemoved = store.RemoveScanSource("/storage/tvshows/");
        Assert.False(kodiRemoved);
        Assert.Contains(store.GetScanSources(), s => s.Path == "/storage/tvshows/");

        // Removing custom source succeeds
        var customRemoved = store.RemoveScanSource("/mnt/media/custom");
        Assert.True(customRemoved);
        Assert.DoesNotContain(store.GetScanSources(), s => s.Path == "/mnt/media/custom");
    }

    [Fact]
    public void GetScanSources_AlwaysSortedByNameRegardlessOfEnabledState()
    {
        using var temp = new TempDirectory();
        var paths = new ServerPaths(temp.Info.FullName);
        var store = new ServerConfigStore(paths, NullLogger<ServerConfigStore>.Instance);

        // Add shares in mixed order and varying enabled/disabled states
        store.SetScanSource("Zebra Series", "/media/zebra", enabled: true);
        store.SetScanSource("Alpha Shows", "/media/alpha", enabled: false);
        store.SetScanSource("Middle Content", "/media/middle", enabled: true);
        store.SetScanSource("Beta Anime", "/media/beta", enabled: false);

        var sources = store.GetScanSources();

        Assert.Equal(4, sources.Count);
        Assert.Equal("Alpha Shows", sources[0].Name);
        Assert.Equal("Beta Anime", sources[1].Name);
        Assert.Equal("Middle Content", sources[2].Name);
        Assert.Equal("Zebra Series", sources[3].Name);

        // Toggle enabled state of items and verify order remains alphabetical by name
        store.SetScanSource("Alpha Shows", "/media/alpha", enabled: true);
        store.SetScanSource("Zebra Series", "/media/zebra", enabled: false);

        var resynced = store.GetScanSources();
        Assert.Equal("Alpha Shows", resynced[0].Name);
        Assert.Equal("Beta Anime", resynced[1].Name);
        Assert.Equal("Middle Content", resynced[2].Name);
        Assert.Equal("Zebra Series", resynced[3].Name);
    }
}
