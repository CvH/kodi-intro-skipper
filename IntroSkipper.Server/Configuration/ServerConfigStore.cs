// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using IntroSkipper.Configuration;
using IntroSkipper.Data;
using Microsoft.Extensions.Logging;

namespace IntroSkipper.Server.Configuration;

/// <summary>
/// Manages loading, saving, and persisting <see cref="PluginConfiguration"/> to config.json.
/// </summary>
public sealed class ServerConfigStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly string _configFilePath;
    private readonly ILogger<ServerConfigStore> _logger;
    private readonly object _syncLock = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerConfigStore"/> class.
    /// </summary>
    /// <param name="storage">Server storage provider containing data directory.</param>
    /// <param name="logger">Logger instance.</param>
    public ServerConfigStore(ServerStorage storage, ILogger<ServerConfigStore> logger)
    {
        _logger = logger;
        _configFilePath = Path.Combine(storage.DataDirectory, "config.json");
        Current = LoadOrCreateDefault();
    }

    /// <summary>
    /// Gets the active configuration instance.
    /// </summary>
    public PluginConfiguration Current { get; }

    /// <summary>
    /// Gets the full path to the configuration file.
    /// </summary>
    public string ConfigFilePath => _configFilePath;

    /// <summary>
    /// Saves the current configuration to disk.
    /// </summary>
    public void Save()
    {
        lock (_syncLock)
        {
            try
            {
                var json = JsonSerializer.Serialize(Current, JsonOptions);
                File.WriteAllText(_configFilePath, json);
                _logger.LogInformation("IntroSkipper configuration saved to {Path}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save configuration to {Path}", _configFilePath);
                throw;
            }
        }
    }

    /// <summary>
    /// Updates configuration properties from an updated instance and persists to disk.
    /// </summary>
    /// <param name="updated">Updated configuration source.</param>
    public void Update(PluginConfiguration updated)
    {
        ArgumentNullException.ThrowIfNull(updated);

        lock (_syncLock)
        {
            CopyProperties(updated, Current);
            Save();
        }
    }

    /// <summary>
    /// Updates configuration properties from a JSON element and persists to disk.
    /// Supports partial updates.
    /// </summary>
    /// <param name="root">Root JSON element containing properties to update.</param>
    public void UpdateFromJson(JsonElement root)
    {
        lock (_syncLock)
        {
            var properties = typeof(PluginConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                if (!prop.CanRead || !prop.CanWrite)
                {
                    continue;
                }

                var camelCaseName = JsonNamingPolicy.CamelCase.ConvertName(prop.Name);
                if (root.TryGetProperty(camelCaseName, out var element) || root.TryGetProperty(prop.Name, out element))
                {
                    if (prop.PropertyType == typeof(bool) && (element.ValueKind == JsonValueKind.True || element.ValueKind == JsonValueKind.False))
                    {
                        prop.SetValue(Current, element.GetBoolean());
                    }
                    else if (prop.PropertyType == typeof(int) && element.TryGetInt32(out var intVal))
                    {
                        prop.SetValue(Current, intVal);
                    }
                    else if (prop.PropertyType == typeof(double) && element.TryGetDouble(out var dblVal))
                    {
                        prop.SetValue(Current, dblVal);
                    }
                    else if (prop.PropertyType == typeof(string) && element.ValueKind == JsonValueKind.String)
                    {
                        prop.SetValue(Current, element.GetString() ?? string.Empty);
                    }
                }
            }

            if (TryGetArrayProperty(root, "seriesExclusions", out var seriesArr))
            {
                Current.SeriesExclusions.Clear();
                foreach (var item in seriesArr.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        Current.SeriesExclusions.Add(item.GetString());
                    }
                }
            }

            if (TryGetArrayProperty(root, "movieExclusions", out var movieArr))
            {
                Current.MovieExclusions.Clear();
                foreach (var item in movieArr.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        Current.MovieExclusions.Add(item.GetString());
                    }
                }
            }

            if (TryGetArrayProperty(root, "pathExclusions", out var pathArr))
            {
                Current.PathExclusions.Clear();
                foreach (var item in pathArr.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                    {
                        Current.PathExclusions.Add(item.GetString());
                    }
                }
            }

            if (TryGetArrayProperty(root, "scanSources", out var sourcesArr))
            {
                Current.ScanSources.Clear();
                foreach (var item in sourcesArr.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object)
                    {
                        var name = item.TryGetProperty("name", out var n) ? n.GetString() ?? string.Empty : string.Empty;
                        var path = item.TryGetProperty("path", out var p) ? p.GetString() ?? string.Empty : string.Empty;
                        var enabled = item.TryGetProperty("enabled", out var e) && e.GetBoolean();
                        if (!string.IsNullOrWhiteSpace(path))
                        {
                            Current.ScanSources.Add(new ScanSource(name, path, enabled));
                        }
                    }
                }
            }

            Save();
        }
    }

    /// <summary>
    /// Gets all configured media scan source paths.
    /// </summary>
    /// <returns>A read-only list of configured sources.</returns>
    public IReadOnlyList<ScanSource> GetScanSources()
    {
        lock (_syncLock)
        {
            return Current.ScanSources.ToList();
        }
    }

    /// <summary>
    /// Adds or updates a media scan source path.
    /// </summary>
    /// <param name="name">Source friendly name.</param>
    /// <param name="path">Source filesystem or share path.</param>
    /// <param name="enabled">Whether the source is enabled.</param>
    public void SetScanSource(string name, string path, bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        lock (_syncLock)
        {
            var existing = Current.ScanSources.FirstOrDefault(s => string.Equals(s.Path, path, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                Current.ScanSources.Remove(existing);
            }

            var friendlyName = string.IsNullOrWhiteSpace(name)
                ? (existing?.Name ?? Path.GetFileName(path.TrimEnd('/', '\\')))
                : name;

            Current.ScanSources.Add(new ScanSource(friendlyName, path, enabled));
            Save();
        }
    }

    /// <summary>
    /// Removes a media scan source path by its path string.
    /// </summary>
    /// <param name="path">Path to remove.</param>
    /// <returns>True if found and removed; otherwise false.</returns>
    public bool RemoveScanSource(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        lock (_syncLock)
        {
            var existing = Current.ScanSources.FirstOrDefault(s => string.Equals(s.Path, path, StringComparison.OrdinalIgnoreCase));
            if (existing is not null)
            {
                Current.ScanSources.Remove(existing);
                Save();
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Synchronizes a list of sources (e.g. from Kodi sources.xml) with the server's sources.
    /// Any incoming path not present on server is added. Existing paths retain their server-configured enabled state.
    /// </summary>
    /// <param name="incoming">Incoming source paths from Kodi.</param>
    /// <returns>The merged and synchronized list of scan sources.</returns>
    public IReadOnlyList<ScanSource> SyncScanSources(IEnumerable<ScanSource> incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);

        lock (_syncLock)
        {
            var changed = false;
            foreach (var item in incoming)
            {
                if (string.IsNullOrWhiteSpace(item.Path))
                {
                    continue;
                }

                var existing = Current.ScanSources.FirstOrDefault(s => string.Equals(s.Path, item.Path, StringComparison.OrdinalIgnoreCase));
                if (existing is null)
                {
                    Current.ScanSources.Add(item);
                    changed = true;
                }
            }

            if (changed)
            {
                Save();
            }

            return Current.ScanSources.ToList();
        }
    }

    private static bool TryGetArrayProperty(JsonElement root, string propertyName, out JsonElement arrayElement)
    {
        var camel = JsonNamingPolicy.CamelCase.ConvertName(propertyName);
        if ((root.TryGetProperty(camel, out arrayElement) || root.TryGetProperty(propertyName, out arrayElement))
            && arrayElement.ValueKind == JsonValueKind.Array)
        {
            return true;
        }

        arrayElement = default;
        return false;
    }

    /// <summary>
    /// Resets configuration to default values and persists to disk.
    /// </summary>
    /// <returns>The newly reset configuration.</returns>
    public PluginConfiguration Reset()
    {
        lock (_syncLock)
        {
            var defaults = new PluginConfiguration();
            CopyProperties(defaults, Current);
            Save();
            _logger.LogInformation("IntroSkipper configuration reset to defaults");
            return Current;
        }
    }

    private static void CopyProperties(PluginConfiguration source, PluginConfiguration target)
    {
        var properties = typeof(PluginConfiguration).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in properties)
        {
            if (prop.CanRead && prop.CanWrite)
            {
                var value = prop.GetValue(source);
                prop.SetValue(target, value);
            }
        }

        target.SeriesExclusions.Clear();
        foreach (var item in source.SeriesExclusions)
        {
            target.SeriesExclusions.Add(item);
        }

        target.MovieExclusions.Clear();
        foreach (var item in source.MovieExclusions)
        {
            target.MovieExclusions.Add(item);
        }

        target.PathExclusions.Clear();
        foreach (var item in source.PathExclusions)
        {
            target.PathExclusions.Add(item);
        }

        target.ScanSources.Clear();
        foreach (var item in source.ScanSources)
        {
            target.ScanSources.Add(new ScanSource(item.Name, item.Path, item.Enabled));
        }
    }

    private PluginConfiguration LoadOrCreateDefault()
    {
        lock (_syncLock)
        {
            if (File.Exists(_configFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_configFilePath);
                    var loaded = JsonSerializer.Deserialize<PluginConfiguration>(json, JsonOptions);
                    if (loaded is not null)
                    {
                        _logger.LogInformation("Loaded configuration from {Path}", _configFilePath);
                        return loaded;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse configuration from {Path}. Using default values.", _configFilePath);
                }
            }

            var defaultConfig = new PluginConfiguration();
            try
            {
                var json = JsonSerializer.Serialize(defaultConfig, JsonOptions);
                File.WriteAllText(_configFilePath, json);
                _logger.LogInformation("Initialized default configuration at {Path}", _configFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write initial default configuration to {Path}", _configFilePath);
            }

            return defaultConfig;
        }
    }
}
