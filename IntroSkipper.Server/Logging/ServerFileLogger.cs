// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

using Microsoft.Extensions.Logging;

namespace IntroSkipper.Server.Logging;

/// <summary>
/// Simple thread-safe file logging provider that appends logs to a local file (e.g. server.log).
/// </summary>
public sealed class ServerFileLoggerProvider : ILoggerProvider
{
    private static readonly Lock WriteLock = new();
    private readonly string _filePath;
    private const long MaxFileSizeBytes = 10L * 1024 * 1024; // 10 MB

    /// <summary>
    /// Initializes a new instance of the <see cref="ServerFileLoggerProvider"/> class.
    /// </summary>
    /// <param name="filePath">Target log file path.</param>
    public ServerFileLoggerProvider(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }

    /// <summary>
    /// Gets the file path to the log file.
    /// </summary>
    public string LogPath => _filePath;

    /// <inheritdoc/>
    public ILogger CreateLogger(string categoryName)
    {
        return new FileLogger(categoryName, _filePath);
    }

    /// <summary>
    /// Reads the most recent lines from the server log file in a thread-safe manner.
    /// </summary>
    /// <param name="maxLines">Maximum lines to retrieve.</param>
    /// <returns>A list of recent log lines.</returns>
    public IReadOnlyList<string> ReadRecentLines(int maxLines = 100)
    {
        if (!File.Exists(_filePath))
        {
            return [];
        }

        try
        {
            lock (WriteLock)
            {
                using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var reader = new StreamReader(fs);
                var allLines = new List<string>();
                string? line;
                while ((line = reader.ReadLine()) is not null)
                {
                    allLines.Add(line);
                }

                if (allLines.Count <= maxLines)
                {
                    return allLines;
                }

                return allLines.Skip(allLines.Count - maxLines).ToList();
            }
        }
        catch
        {
            return [];
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
    }

    private sealed class FileLogger(string categoryName, string filePath) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => logLevel != LogLevel.None && logLevel >= LogLevel.Information;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message) && exception is null)
            {
                return;
            }

            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff", System.Globalization.CultureInfo.InvariantCulture);
            var levelStr = logLevel.ToString().ToLowerInvariant();
            var line = $"{timestamp} [{levelStr}] [{categoryName}] {message}";

            if (exception is not null)
            {
                line += Environment.NewLine + exception;
            }

            line += Environment.NewLine;

            try
            {
                lock (WriteLock)
                {
                    // Check file size rotation
                    if (File.Exists(filePath))
                    {
                        var info = new FileInfo(filePath);
                        if (info.Length > MaxFileSizeBytes)
                        {
                            var backup = filePath + ".1";
                            File.Move(filePath, backup, overwrite: true);
                        }
                    }

                    File.AppendAllText(filePath, line);
                }
            }
            catch
            {
                // Never allow logging failures to crash the application
            }
        }
    }
}
