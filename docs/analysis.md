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

- **Configuration hash.** `ConfigHasher.Analysis` hashes the settings that affect one mode's output under the season's action, so the expected hash depends on the action and the mode, not the episode. Chromaprint availability is part of the hash for Chromaprint-capable modes, so a season settled with no segments is analyzed again once Chromaprint becomes available. A mode's subtitle pattern joins its hash only while that mode's subtitle detection is active, so with subtitle detection off, the subtitle settings change neither hash, and editing a pattern whose detection is off reopens nothing. A blank pattern detects nothing and hashes as detection off. The selected subtitle languages join the same token as sorted 639-2/T codes (`SubtitleLanguageSelection.Normalized`), so writing `ger` for `deu` reopens nothing. The subtitle settings form a separate part of a compound hash, so `QueueVerifier` can tell a subtitle-only change from a change to the other settings (`ConfigHasher.IsSubtitleOnlyHashChange`). The Preview hash of a season that derives previews from the credits end also carries a token for the derivation rule (`docs/credits.md`). Every Recap hash carries a token for the Chromaprint recap end rule, so a change to that rule reruns Recap once in every season, from the cached fingerprints and black-frame scans. The Recap hash also carries the recap chapter pattern. An install that still holds the recap chapter default of any release through 12.0.6 gets the current default in memory at every startup (`PluginConfiguration.UpgradeLegacyDefaults`) and reruns Recap once in every season. The configuration file keeps the old string until the next dashboard save, and a pattern the user edited is kept. While a preferred audio language is set under `PreferAudioStreamWithMostChannels`, the Chromaprint-backed modes carry a token for the stream ranking, which puts the default-disposition stream first as ffmpeg does. Seasons fingerprinted under the older channels-only ranking rerun once, and without a preferred language the token is absent. While `UseEmbeddedChaptersForAnalysis` is on, every mode's hash carries a chapter source token under every action, and with the setting off the token is absent, so the default configuration keeps its hashes. The token names the source a record was computed from. A switch alone reruns nothing: `QueueVerifier` settles a record that matches the hash of the other source and flags it for the chapter source check of the pass, which reopens the mode only when the two sources hold different chapters. The token sits outside the subtitle part, so a switch is never a subtitle-only change. A downgrade to a release without the token reruns every record written while the setting was on.
- **File version.** Chromaprint compares episodes across a season, and library events cannot always say whether a media file was replaced. Each record therefore stores the file version, the write time held for the media file, in ticks. Verification compares it with every record the item has, whatever the mode, since a record of a disabled mode still proves the file changed. A mismatch flags `QueuedEpisode.FileChanged` and the pass resets the item, except on a Recap or Preview record (see Subtitle file version). The check runs on every pass, so a replacement is caught even when its event was missed. A record without a version is stamped with the media version seen, unless the file changed. An item without a media version cannot be compared and keeps every verdict, except on a Recap or Preview record that carries the sidecar version (see Subtitle file version).
- **Subtitle file version.** A mode with active subtitle detection records `QueuedEpisode.SubtitleFileVersion` instead: a hash of the media version and the names, lengths and write times of the text sidecars extraction reads (`SubtitleSidecarFiles.FileVersion`), or the media version when there are none. Verification lists each folder once per season pass (`SubtitleSidecarFiles.FolderListings`), keeps the files with a subtitle extension, and takes their lengths and write times from that listing, so a sidecar written after its folder was listed counts from the next pass. On NTFS a listing reports a hard-linked sidecar as last written through its folder, so one rewritten through another link counts once something opens it by the library path. VobSub sidecars and sidecars in a language the setting leaves out are never read and do not count. A rewritten sidecar changes what subtitle detection reads, not the media that the other modes and the fingerprints describe, and a record written before subtitle detection was turned on or off carries the other version. So a Recap or Preview record that does not carry the version its mode records now reopens only that mode, with no reset and no cache wipe, when another record of the item confirms the media version. The mode then reruns subtitle detection alone when the record's hash is current apart from the subtitle settings, and its full chain otherwise. The sidecar version cannot tell a sidecar edit from a replaced media file, so without such a record the episode is reset like any replaced file. In a library that scans only Recap and Preview, a sidecar edit can therefore cost a reset and a fingerprint-cache wipe. With the mode's subtitle detection off, a record whose hash still matches was written under today's settings, so a different version can only mean the media file was replaced, and the episode is reset. That holds for a mode the pass does not run too, whose hash is compared under the season's saved analyzer action. An item without a media version cannot show a replaced file, so a sidecar change there reopens the mode without a reset.
- **Exclusions.** A record survives the item being excluded, so an item excluded and then included again with a matching record stays settled. `ExcludedTimestamps/Clear` is the explicit reset.

## Detection cache

`DetectionCacheService` stores raw ffmpeg output in its own database, `introskipper-cache.db`, behind `DetectionCacheDatabase`. Rows are keyed by item, entry type (`CacheEntryType`), mode and the time range they cover, carry a configuration hash from `ConfigHasher.DetectionCache`, and are Brotli-compressed.

## Upgrading from 10.11

`LegacyDatabaseImporter` turns old per-season episode lists into analysis records under the season's hash. `LegacyAnalysisCompatibility` recognizes frozen analysis hashes of releases 1.10.11.22, 1.10.11.23 and 1.10.11.24 under retained settings.
