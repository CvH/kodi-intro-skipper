// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

using IntroSkipper.Db;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace IntroSkipper.Server;

/// <summary>
/// Configures and manages SQLite database storage located at the binary folder by default.
/// </summary>
public sealed class ServerStorage
{
    private readonly SegmentContextFactory _segmentContextFactory;
    private readonly CacheContextFactory _cacheContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerStorage"/> class.
    /// </summary>
    /// <param name="dataDirectory">Directory to save the SQLite database in. Defaults to the binary directory.</param>
    /// <param name="loggerFactory">Logger factory.</param>
    public ServerStorage(string? dataDirectory, ILoggerFactory loggerFactory)
    {
        DataDirectory = string.IsNullOrWhiteSpace(dataDirectory)
            ? AppContext.BaseDirectory
            : Path.GetFullPath(dataDirectory);

        Directory.CreateDirectory(DataDirectory);

        SegmentDatabasePath = Path.Combine(DataDirectory, "introskipper.db");
        CacheDatabasePath = Path.Combine(DataDirectory, "introskipper-cache.db");

        _segmentContextFactory = new SegmentContextFactory(SegmentDatabasePath);
        _cacheContextFactory = new CacheContextFactory(CacheDatabasePath);

        Database = new IntroSkipperDatabase(_segmentContextFactory, loggerFactory.CreateLogger<IntroSkipperDatabase>());
        CacheDatabase = new DetectionCacheDatabase(_cacheContextFactory, loggerFactory.CreateLogger<DetectionCacheDatabase>());
    }

    /// <summary>
    /// Gets the root data directory where databases and caches reside.
    /// </summary>
    public string DataDirectory { get; }

    /// <summary>
    /// Gets the full path to the segment database file.
    /// </summary>
    public string SegmentDatabasePath { get; }

    /// <summary>
    /// Gets the full path to the detection cache database file.
    /// </summary>
    public string CacheDatabasePath { get; }

    /// <summary>
    /// Gets the segment database facade.
    /// </summary>
    public IntroSkipperDatabase Database { get; }

    /// <summary>
    /// Gets the detection cache database facade.
    /// </summary>
    public DetectionCacheDatabase CacheDatabase { get; }

    /// <summary>
    /// Ensures both SQLite databases are created and migrated to the latest schema.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when initialization is done.</returns>
    public async Task InitializeAsync()
    {
        await Database.InitializeAsync().ConfigureAwait(false);
        _ = CacheDatabase.TryInitialize();
    }

    private sealed class SegmentContextFactory(string dbPath) : IDbContextFactory<IntroSkipperDbContext>
    {
        public IntroSkipperDbContext CreateDbContext()
        {
            var builder = new DbContextOptionsBuilder<IntroSkipperDbContext>();
            SqlitePragmas.Configure(builder, dbPath);
            return new IntroSkipperDbContext(builder.Options);
        }
    }

    private sealed class CacheContextFactory(string dbPath) : IDbContextFactory<DetectionCacheDbContext>
    {
        public DetectionCacheDbContext CreateDbContext()
        {
            var builder = new DbContextOptionsBuilder<DetectionCacheDbContext>();
            SqlitePragmas.Configure(builder, dbPath);
            return new DetectionCacheDbContext(builder.Options);
        }
    }
}
