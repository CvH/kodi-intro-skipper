// SPDX-FileCopyrightText: 2026 rlauuzo
// SPDX-FileCopyrightText: 2026 AbandonedCart
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Tests;

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using IntroSkipper.Configuration;
using IntroSkipper.Data;
using IntroSkipper.FFmpeg;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// The chapter source: ffprobe's chapter listing, and the season pass that reads each
/// episode's chapters once, from Jellyfin's chapter table or, with
/// <see cref="PluginConfiguration.UseEmbeddedChaptersForAnalysis"/>, from the media file.
/// </summary>
public class TestEmbeddedChapters
{
    private const string ChapterNameFormat = "Chapter {0}";
    private static readonly long Runtime = DatabaseTestHelpers.Ticks(120);

    /// <summary>
    /// The read yields what Jellyfin's probe stores in its chapter table: starts rounded to the
    /// millisecond, an untitled chapter named by its number in ffprobe's order, sorted by start.
    /// </summary>
    [Fact]
    public void ParseChapters_ShapesChaptersAsJellyfinStoresThem()
    {
        var chapters = FFmpegOutputParser.ParseChapters(
            """
            {
              "chapters": [
                { "start_time": "98.250000", "tags": { "title": "Main" } },
                { "start_time": "90.048625", "tags": { "title": "Opening" } },
                { "start_time": "0.000000" }
              ]
            }
            """,
            Runtime,
            ChapterNameFormat);

        Assert.Equal(
            [(0L, "Chapter 3"), (900_490_000L, "Opening"), (982_500_000L, "Main")],
            chapters.Select(chapter => (chapter.StartPositionTicks, chapter.Name)));
    }

    /// <summary>
    /// A half millisecond rounds to even as Jellyfin rounds it, and a title that is a timestamp,
    /// a bare number or blank is replaced like a missing one.
    /// </summary>
    [Theory]
    [InlineData("1.000500", "Opening", 10_000_000L, "Opening")]
    [InlineData("1.001500", "Opening", 10_020_000L, "Opening")]
    [InlineData("5.000000", "1:23", 50_000_000L, "Chapter 1")]
    [InlineData("5.000000", "12", 50_000_000L, "Chapter 1")]
    [InlineData("5.000000", "  ", 50_000_000L, "Chapter 1")]
    public void ParseChapters_RoundsAndNamesAsJellyfinDoes(string start, string title, long ticks, string name)
    {
        var chapter = Assert.Single(FFmpegOutputParser.ParseChapters(
            $$"""{ "chapters": [ { "start_time": "{{start}}", "tags": { "title": "{{title}}" } } ] }""",
            Runtime,
            ChapterNameFormat));

        Assert.Equal((ticks, name), (chapter.StartPositionTicks, chapter.Name));
    }

    /// <summary>
    /// Jellyfin saves no chapter at or past the item's runtime, an end marker included, and the
    /// rounded start is what it compares.
    /// </summary>
    [Fact]
    public void ParseChapters_DropsChaptersAtOrPastTheRuntime()
    {
        var chapters = FFmpegOutputParser.ParseChapters(
            """
            {
              "chapters": [
                { "start_time": "0.000000", "tags": { "title": "Episode" } },
                { "start_time": "119.999700", "tags": { "title": "End" } },
                { "start_time": "120.000000", "tags": { "title": "Past" } }
              ]
            }
            """,
            Runtime,
            ChapterNameFormat);

        Assert.Equal(["Episode"], chapters.Select(chapter => chapter.Name));
    }

    [Fact]
    public void ParseChapters_DropsStartsThatAreNotTickCounts()
    {
        var chapters = FFmpegOutputParser.ParseChapters(
            """
            {
              "chapters": [
                { "start_time": "NaN", "tags": { "title": "NaN" } },
                { "start_time": "1e100", "tags": { "title": "Too late" } },
                { "start_time": "-1.000000", "tags": { "title": "Before start" } },
                { "start_time": "15.000000", "tags": { "title": "Opening" } }
              ]
            }
            """,
            Runtime,
            ChapterNameFormat);

        var chapter = Assert.Single(chapters);
        Assert.Equal(150_000_000, chapter.StartPositionTicks);
        Assert.Equal("Opening", chapter.Name);
    }

    /// <summary>
    /// Output without a chapters array is a failed read, which the probe retries, not a file
    /// without chapters, which would settle the episode.
    /// </summary>
    /// <param name="raw">ffprobe stdout.</param>
    [Theory]
    [InlineData("{}")]
    [InlineData("[]")]
    [InlineData("""{ "chapters": {} }""")]
    public void ParseChapters_RejectsOutputWithoutAChaptersArray(string raw)
    {
        Assert.Throws<System.Text.Json.JsonException>(() => FFmpegOutputParser.ParseChapters(raw, Runtime, ChapterNameFormat));
    }

    [Fact]
    public void ParseChapters_ReadsAnEmptyChaptersArrayAsNoChapters()
    {
        Assert.Empty(FFmpegOutputParser.ParseChapters("""{ "chapters": [] }""", Runtime, ChapterNameFormat));
    }

    /// <summary>
    /// Pins the JSON shape the installed ffprobe prints for a Matroska file's chapters.
    /// </summary>
    [FactSkipFFmpegTests]
    public async Task GetEmbeddedChaptersAsync_ReadsTheContainerChapters()
    {
        using var directory = new TempDirectory();
        var metadata = directory.Join("chapters.txt");
        var path = directory.Join("chapters.mkv");
        await File.WriteAllTextAsync(
            metadata,
            """
            ;FFMETADATA1
            [CHAPTER]
            TIMEBASE=1/1000000
            START=0
            END=1048625
            title=Opening
            [CHAPTER]
            TIMEBASE=1/1000000
            START=1048625
            END=1500000
            title=Main
            [CHAPTER]
            TIMEBASE=1/1000000
            START=1500000
            END=2000000
            """.ReplaceLineEndings("\n"));
        await new FFmpegProcessRunner(NullLogger.Instance).RunAsync(
            "ffmpeg",
            ["-y", "-v", "error", "-f", "lavfi", "-i", "testsrc2=s=32x32:r=25:d=2", "-f", "ffmetadata", "-i", metadata, "-map", "0", "-map_chapters", "1", "-c:v", "ffv1", path]);

        var chapters = await FfmpegTestHelpers.CreateFFmpegService().GetEmbeddedChaptersAsync(
            new QueuedEpisode { EpisodeId = Guid.NewGuid(), Name = "chapters", Path = path, Duration = 2 });

        Assert.NotNull(chapters);
        Assert.Equal(
            [(0L, "Opening"), (10_490_000L, "Main"), (15_000_000L, "Chapter 3")],
            chapters.Select(chapter => (chapter.StartPositionTicks, chapter.Name)));
    }

    /// <summary>
    /// A file without chapters reads as an empty list, and one ffprobe cannot open reads as
    /// null, so the pass never takes an unreadable file for one without chapters.
    /// </summary>
    [FactSkipFFmpegTests]
    public async Task GetEmbeddedChaptersAsync_TellsAFileWithoutChaptersFromAnUnreadableOne()
    {
        using var directory = new TempDirectory();
        var ffmpeg = FfmpegTestHelpers.CreateFFmpegService();

        var none = await ffmpeg.GetEmbeddedChaptersAsync(FfmpegTestHelpers.QueueFile("video/rainbow.mp4"));
        var unreadable = await ffmpeg.GetEmbeddedChaptersAsync(
            new QueuedEpisode { EpisodeId = Guid.NewGuid(), Name = "missing", Path = directory.Join("missing.mkv") });

        Assert.NotNull(none);
        Assert.Empty(none);
        Assert.Null(unreadable);
    }
}
