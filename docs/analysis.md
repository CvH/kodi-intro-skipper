# Analysis

How library changes become analysis passes, how a pass decides what to analyze, and what it records. Credits detection is in `docs/credits.md`, and where results are stored is in `docs/segments.md`.

## The analysis queue

`AnalysisQueue` runs analysis passes on background worker tasks. Feeders enqueue scan requests, which analyze configured media sources and paths.

- **Manual and API scan.** A scan request runs over configured scan sources or specified season directories, analyzing the episodes against current configuration hashes.
- **Queue and Worker.** `AnalysisQueue` tracks running and queued passes, ensuring only one pass analyzes a given season at a time.

## Seasons

`MediaScanner` turns media directories and video files into seasons the analyzers work on, grouped by series and season number.

## A season's pass

`StandaloneAnalyzer` coordinates analyzer runs over each season:

1. **Verify.** `QueueVerifier` classifies episodes against stored analysis records and configuration hashes.
2. **Reset replaced files.** Episodes whose media file changed lose automatic segments and cache through `ResetItemsForReanalysisAsync`, then analyze like new ones.
3. **Read chapters.** Analyzers read chapters from embedded media chapters (`FFmpegOutputParser.ParseChapters` and `IFFmpegService.GetEmbeddedChaptersAsync`).
4. **Run each mode.**
   - Introduction and Recap run a first-wins chain: ChapterAnalyzer, then ChromaprintAnalyzer when ffmpeg is compatible.
   - Commercial uses chapters only (`ChapterAnalyzer`).
   - Credits runs `CreditsPass`, which combines candidates across analyzers (`docs/credits.md`).
   - Preview uses chapters, or derives previews from the credits end (`AnimePreviewDeriver`, `docs/credits.md`).
   - Subtitle matching is an opt-in fallback before default analyzers (`SubtitleAnalyzer`).

### Subtitle analysis

Text subtitle cues matching `SubtitleRecapPattern` or `SubtitlePreviewPattern` detect Recap and Preview. `IFFmpegService.ExtractSubtitleCuesAsync` extracts cues from embedded streams or sidecars (`SubtitleSidecarFiles`).

## Analysis records

`AnalyzedItems` holds one row per item and mode, `(ItemId, Type, ConfigHash, FileVersion)`, recording that the item was analyzed under a configuration hash, whether or not it found segments. `QueueVerifier` settles an item whose record matches the current hash (`EpisodeState.Analyzed` with segments, `EpisodeState.NoSegments` without).

- **Configuration hash.** `ConfigHasher.Analysis` hashes the settings that affect one mode's output.
- **File version.** Each record stores the write time of the media file, in ticks. Verification flags `QueuedEpisode.FileChanged` when a file changes on disk.

## Detection cache

`DetectionCacheService` stores raw ffmpeg output in its own database, `introskipper-cache.db`, behind `DetectionCacheDatabase`. Rows are keyed by item, entry type (`CacheEntryType`), mode and the time range they cover, carry a configuration hash from `ConfigHasher.DetectionCache`, and are Brotli-compressed.

## Upgrading from 10.11

`LegacyDatabaseImporter` turns old per-season episode lists into analysis records under the season's hash. `LegacyAnalysisCompatibility` recognizes frozen analysis hashes of releases 1.10.11.22, 1.10.11.23 and 1.10.11.24 under retained settings.
