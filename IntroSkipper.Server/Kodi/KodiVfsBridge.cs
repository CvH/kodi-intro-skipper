// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-License-Identifier: GPL-3.0-only

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using IntroSkipper.Server.Scanning;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace IntroSkipper.Server.Kodi;

/// <summary>
/// Bridges remote Kodi network shares (smb://, nfs://, etc.) via Kodi's VFS HTTP streaming gateway
/// and Kodi's JSON-RPC library introspection.
/// </summary>
public sealed class KodiVfsBridge : IDisposable
{
    private readonly ILogger<KodiVfsBridge> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="KodiVfsBridge"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="configuration">Application configuration.</param>
    public KodiVfsBridge(ILogger<KodiVfsBridge> logger, IConfiguration configuration)
    {
        _logger = logger;

        // 1. Try reading settings from guisettings.xml if running on same host (e.g. LibreELEC)
        var (detectedPort, detectedUser, detectedPass) = TryDetectKodiGuiSettings();

        // 2. Allow CLI flags / environment variables to override
        var configUrl = configuration["kodi-url"] ?? Environment.GetEnvironmentVariable("INTROSKIPPER_KODI_URL");
        var configUser = configuration["kodi-user"] ?? Environment.GetEnvironmentVariable("INTROSKIPPER_KODI_USER");
        var configPass = configuration["kodi-pass"] ?? Environment.GetEnvironmentVariable("INTROSKIPPER_KODI_PASS");

        var effectivePort = detectedPort > 0 ? detectedPort : 8080;
        BaseUrl = !string.IsNullOrWhiteSpace(configUrl) ? configUrl.TrimEnd('/') : $"http://127.0.0.1:{effectivePort}";
        Username = !string.IsNullOrWhiteSpace(configUser) ? configUser : (detectedUser ?? "kodi");
        Password = !string.IsNullOrWhiteSpace(configPass) ? configPass : (detectedPass ?? "kodi");

        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

        if (!string.IsNullOrEmpty(Username))
        {
            var authBytes = Encoding.UTF8.GetBytes($"{Username}:{Password}");
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));
        }

        _logger.LogInformation("Kodi VFS Bridge initialized. BaseUrl: {Url}, Username: {User}", BaseUrl, Username);
    }

    /// <summary>
    /// Gets the base URL to Kodi web server.
    /// </summary>
    public string BaseUrl { get; }

    /// <summary>
    /// Gets the HTTP basic authentication username.
    /// </summary>
    public string Username { get; }

    /// <summary>
    /// Gets the HTTP basic authentication password.
    /// </summary>
    public string Password { get; }

    /// <summary>
    /// Translates a media file path (such as smb:// or nfs://) into a Kodi VFS HTTP streaming URL.
    /// Local filesystem paths and existing HTTP(S) URLs are returned unchanged.
    /// </summary>
    /// <param name="mediaPath">Original media path or URI.</param>
    /// <returns>HTTP streaming URL or local path.</returns>
    public string BuildVfsUrl(string mediaPath)
    {
        if (string.IsNullOrWhiteSpace(mediaPath))
        {
            return mediaPath;
        }

        // If local filesystem path or already HTTP/HTTPS, return as is
        if (!mediaPath.Contains("://") ||
            mediaPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            mediaPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return mediaPath;
        }

        try
        {
            var encoded = Uri.EscapeDataString(mediaPath);
            var baseUri = new Uri(BaseUrl);
            var auth = !string.IsNullOrEmpty(Username)
                ? $"{Uri.EscapeDataString(Username)}:{Uri.EscapeDataString(Password)}@"
                : string.Empty;

            return $"{baseUri.Scheme}://{auth}{baseUri.Authority}/vfs/{encoded}";
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to build Kodi VFS URL for {Path}", mediaPath);
            return mediaPath;
        }
    }

    /// <summary>
    /// Discovers all video episodes for a given network folder by querying Kodi's JSON-RPC API.
    /// First attempts VideoLibrary.GetEpisodes with path filtering; if empty, crawls directories via Files.GetDirectory.
    /// </summary>
    /// <param name="folderPath">The network share folder path (e.g. smb://NAS/Serien/).</param>
    /// <param name="scanner">Scanner to parse filenames if library metadata is not populated.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of parsed episode metadata records.</returns>
    public async Task<IReadOnlyList<ParsedEpisodeInfo>> DiscoverEpisodesAsync(
        string folderPath,
        MediaScanner scanner,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Discovering episodes from Kodi for network folder: {Path}", folderPath);

        // 1. Query Kodi VideoLibrary.GetEpisodes with startswith filter
        var libraryEpisodes = await QueryLibraryEpisodesAsync(folderPath, cancellationToken).ConfigureAwait(false);
        if (libraryEpisodes.Count > 0)
        {
            _logger.LogInformation("Found {Count} episodes in Kodi VideoLibrary matching {Path}", libraryEpisodes.Count, folderPath);
            return libraryEpisodes;
        }

        // 2. Fallback: Crawl directory structure using Kodi Files.GetDirectory
        _logger.LogInformation("No indexed episodes in Kodi VideoLibrary for {Path}. Falling back to Kodi Files.GetDirectory crawl...", folderPath);
        var crawledEpisodes = new List<ParsedEpisodeInfo>();
        await CrawlDirectoryAsync(folderPath, scanner, crawledEpisodes, depth: 0, maxDepth: 10, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Crawled {Count} video files in {Path} via Kodi VFS", crawledEpisodes.Count, folderPath);
        return crawledEpisodes;
    }

    /// <summary>
    /// Checks if Kodi JSON-RPC is reachable.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if Kodi responded with 'pong'.</returns>
    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { jsonrpc = "2.0", method = "JSONRPC.Ping", id = 1 };
            using var response = await SendJsonRpcAsync(payload, cancellationToken).ConfigureAwait(false);
            if (response is not null && response.RootElement.TryGetProperty("result", out var res))
            {
                return res.GetString() == "pong";
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Kodi JSON-RPC ping failed");
        }

        return false;
    }

    private async Task<IReadOnlyList<ParsedEpisodeInfo>> QueryLibraryEpisodesAsync(
        string folderPath,
        CancellationToken cancellationToken)
    {
        var cleanPath = folderPath.TrimEnd('/');
        var payload = new
        {
            jsonrpc = "2.0",
            method = "VideoLibrary.GetEpisodes",
            params_ = new
            {
                properties = new[] { "file", "showtitle", "season", "episode", "title" },
                filter = new
                {
                    field = "path",
                    operator_ = "startswith",
                    value = cleanPath
                }
            },
            id = 1
        };

        // Note: property names with underscore are remapped to match JSON-RPC specs
        var jsonText = JsonSerializer.Serialize(payload).Replace("\"params_\":", "\"params\":").Replace("\"operator_\":", "\"operator\":");

        try
        {
            using var response = await SendJsonRpcStringAsync(jsonText, cancellationToken).ConfigureAwait(false);
            if (response is not null &&
                response.RootElement.TryGetProperty("result", out var result) &&
                result.TryGetProperty("episodes", out var episodes))
            {
                var list = new List<ParsedEpisodeInfo>();
                foreach (var ep in episodes.EnumerateArray())
                {
                    var file = ep.TryGetProperty("file", out var f) ? f.GetString() : null;
                    if (string.IsNullOrWhiteSpace(file))
                    {
                        continue;
                    }

                    var show = ep.TryGetProperty("showtitle", out var s) ? s.GetString() ?? string.Empty : string.Empty;
                    var season = ep.TryGetProperty("season", out var sn) ? sn.GetInt32() : 1;
                    var episodeNum = ep.TryGetProperty("episode", out var en) ? en.GetInt32() : 1;
                    var title = ep.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;

                    list.Add(new ParsedEpisodeInfo(file, show, season, episodeNum, title));
                }

                return list;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to query Kodi VideoLibrary for {Path}", folderPath);
        }

        return [];
    }

    private async Task CrawlDirectoryAsync(
        string directoryPath,
        MediaScanner scanner,
        List<ParsedEpisodeInfo> accumulator,
        int depth,
        int maxDepth,
        CancellationToken cancellationToken)
    {
        if (depth > maxDepth || cancellationToken.IsCancellationRequested)
        {
            return;
        }

        var payload = new
        {
            jsonrpc = "2.0",
            method = "Files.GetDirectory",
            params_ = new
            {
                directory = directoryPath,
                media = "video",
                properties = new[] { "title", "season", "episode", "showtitle" }
            },
            id = 1
        };

        var jsonText = JsonSerializer.Serialize(payload).Replace("\"params_\":", "\"params\":");
        try
        {
            using var response = await SendJsonRpcStringAsync(jsonText, cancellationToken).ConfigureAwait(false);
            if (response is null ||
                !response.RootElement.TryGetProperty("result", out var result) ||
                !result.TryGetProperty("files", out var files))
            {
                return;
            }

            foreach (var item in files.EnumerateArray())
            {
                cancellationToken.ThrowIfCancellationRequested();

                var fileType = item.TryGetProperty("filetype", out var ft) ? ft.GetString() : "file";
                var file = item.TryGetProperty("file", out var f) ? f.GetString() : null;
                if (string.IsNullOrWhiteSpace(file))
                {
                    continue;
                }

                if (fileType == "directory")
                {
                    await CrawlDirectoryAsync(file, scanner, accumulator, depth + 1, maxDepth, cancellationToken).ConfigureAwait(false);
                }
                else if (fileType == "file")
                {
                    // Check if Kodi provided metadata
                    var show = item.TryGetProperty("showtitle", out var st) ? st.GetString() : null;
                    var season = item.TryGetProperty("season", out var sn) ? sn.GetInt32() : -1;
                    var episodeNum = item.TryGetProperty("episode", out var en) ? en.GetInt32() : -1;
                    var title = item.TryGetProperty("title", out var t) ? t.GetString() ?? string.Empty : string.Empty;

                    if (!string.IsNullOrWhiteSpace(show) && season >= 0 && episodeNum >= 0)
                    {
                        accumulator.Add(new ParsedEpisodeInfo(file, show, season, episodeNum, title));
                    }
                    else if (scanner.TryParseEpisode(file, out var parsed) && parsed is not null)
                    {
                        accumulator.Add(parsed);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to list files in Kodi directory: {Dir}", directoryPath);
        }
    }

    private async Task<JsonDocument?> SendJsonRpcAsync(object payload, CancellationToken cancellationToken)
    {
        var jsonText = JsonSerializer.Serialize(payload);
        return await SendJsonRpcStringAsync(jsonText, cancellationToken).ConfigureAwait(false);
    }

    private async Task<JsonDocument?> SendJsonRpcStringAsync(string jsonText, CancellationToken cancellationToken)
    {
        var url = $"{BaseUrl.TrimEnd('/')}/jsonrpc";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonText, Encoding.UTF8, "application/json")
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Kodi JSON-RPC HTTP error {Code} from {Url}", response.StatusCode, url);
            return null;
        }

        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        return await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static (int Port, string? User, string? Pass) TryDetectKodiGuiSettings()
    {
        var candidates = new[]
        {
            "/storage/.kodi/userdata/guisettings.xml",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".kodi", "userdata", "guisettings.xml"),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Kodi", "userdata", "guisettings.xml")
        };

        foreach (var path in candidates)
        {
            if (!File.Exists(path))
            {
                continue;
            }

            try
            {
                var doc = XDocument.Load(path);
                var settings = doc.Descendants("setting");

                int port = 8080;
                string? user = null;
                string? pass = null;

                foreach (var s in settings)
                {
                    var id = (string?)s.Attribute("id");
                    if (id == "services.webserverport" && int.TryParse(s.Value, out var p))
                    {
                        port = p;
                    }
                    else if (id == "services.webserverusername")
                    {
                        user = s.Value;
                    }
                    else if (id == "services.webserverpassword")
                    {
                        pass = s.Value;
                    }
                }

                return (port, user, pass);
            }
            catch
            {
                // Ignore parse errors on candidate files
            }
        }

        return (8080, null, null);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _httpClient.Dispose();
    }
}
