# AGENTS.md

intro-skipper: a standalone media intro and credit scene detector server and engine (C#, `net10.0`), serving Kodi and external clients via REST API and a built-in web dashboard.

## Project map

- `IntroSkipper/` core detection engine
  - `Analyzers/` ChromaprintAnalyzer (cross-episode audio fingerprints), ChapterAnalyzer, BlackFrameAnalyzer (legacy, behind `UseLegacyBlackFrameAnalyzer`), Credits/KeyframeAnalyzer (black roll and card credits from one keyframe scan), AnimePreviewDeriver, SubtitleAnalyzer (recaps and previews from text subtitle cues)
  - `Manager/` QueueVerifier (analysis state per episode)
  - `FFmpeg/` FFmpegService (composes FFmpegProcessRunner, FFmpegVersionGate and EpisodeProbeMemo), the only media probing path
  - `Db/` database facades, entities, admission policy, legacy importer
  - `Migrations/` EF migrations for `introskipper-v2.db`
  - `SegmentChanges/` intent models
  - `Data/` TickConversions, DTOs, ScanSource
  - `Configuration/`, `Helper/`
- `IntroSkipper.Server/` standalone server and web dashboard
  - `IntroSkipper.Server/Analysis/` StandaloneAnalyzer, MediaScanner, AnalysisQueue
  - `IntroSkipper.Server/Web/` embedded web dashboard
  - `IntroSkipper.Server/Program.cs` REST endpoints (`/api/segments`, `/api/config`, `/api/scan`)
- `IntroSkipper.Tests/` xUnit tests
- `tools/CreditsRunner/` runs the keyframe credits analyzer, or one season at a time the Introduction chain (intro mode) or the whole credits pass (credits-pass mode), over real files and scores it against labels
- `CONTEXT.md` domain glossary. Use its terms in code, tests and prose.
- `docs/` `segments.md` (storage, writes), `analysis.md` (queue, seasons, analysis records, detection cache), `credits.md` (credits pass, keyframe analyzer)

<important if="you need to build, run tests, or work on the codebase">

Run .NET commands from the repo root.

| Command | What it does |
|---|---|
| `dotnet build IntroSkipper.sln` | library, server, tests and `tools/CreditsRunner` |
| `dotnet test` | all tests (xUnit) |
| `dotnet test --filter "FullyQualifiedName~TestSegmentTombstones"` | one class; append `.MethodName` for one test |

- `TreatWarningsAsErrors` with StyleCop and all analyzers enabled (`AnalysisMode=AllEnabledByDefault`). Any new warning fails the build.
- Tests need jellyfin-ffmpeg with chromaprint. On Windows, `WindowsFfmpegTestBootstrap` downloads a portable build each time the test host starts and extracts `ffmpeg.exe` and `ffprobe.exe` to `IntroSkipper.Tests/bin/Debug/net10.0/_ffmpeg/extract/`; use those for experiments.
- Tests support jellyfin-ffmpeg 8.1.3 or 7.1.3 or newer.
</important>

<important if="you are writing or changing tests">
Tautological tests considered harmful.

Reuse the shared seams in `IntroSkipper.Tests/` instead of building new ones:
- `TempSegmentDb`, `TempCacheDb` (temp-file segment and detection-cache databases, deleted on dispose), `DatabaseTestHelpers` (facades over temp-file SQLite)
- `SegmentSeeding` (seeds user state through `ApplyChangeAsync`)
- `StubFFmpegService` (counting `IFFmpegService` with per-member hooks)
- `FfmpegTestHelpers` (real ffmpeg over fixture media), `LegacySchemaFixtures` (pre-v2 rows for importer tests)

Test parallelization is disabled assembly-wide.
</important>

<important if="you are creating a branch, picking a PR base, or rebasing">
Active branch is `kodi` tracking origin's `kodi`.
</important>

<important if="you are working on analyzers or the detection pipeline">
Analyzers are orchestrated per mode: a first-wins chain for every mode except credits, which `CreditsPass` (in `Analyzers/Credits/`) analyzes by combining every analyzer's candidate per episode. The pipeline is in `docs/analysis.md` and the credits rules in `docs/credits.md`. All media probing goes through `FFmpegService`. Analyzers read chapters only from `QueuedEpisode.Chapters`, which the season pass fills, and never fetch chapters themselves. Raw detection output is cached in the detection-cache DB keyed by config hash. A setting that changes detection output must be added to `Helper/ConfigHasher`, or cached results are reused under the old settings. A test fails until every new setting is hashed or listed as not affecting detection (`TestConfigHasher`).

Judge every keyframe credits change with `tools/CreditsRunner` against a baseline run, and every Chapter, Chromaprint or time adjustment change to Introduction with the runner's intro mode. A Chromaprint change also reaches credits, which credits-pass mode measures. Its README has the commands.
</important>

<important if="you are touching databases, EF Core, migrations, or anything in Db/">
Two SQLite databases sit behind singleton facades (`IntroSkipperDatabase`, `DetectionCacheDatabase`) that own initialization and migrations via internal retryable gates. Consumers go through a facade, never a DbContext directly. `introskipper-v2.db` is the source of truth for segments. The legacy `introskipper.db` is imported exactly once, read-only, by `LegacyDatabaseImporter`. Design rationale (plural segments, tombstones, provenance, shared Guid v7 ids): `docs/segments.md`. Analysis records and the detection cache: `docs/analysis.md`.
</important>

<important if="you are adding or changing any write to segment data">
Mutations and user overrides commit through `IntroSkipperDatabase.ApplyChangeAsync`: mutation, analysis bookkeeping, and tombstones in one transaction.
</important>

<important if="you are handling segment times, tombstones, automatic segment writes, or disabled items">
- Ticks internally, seconds only at the analyzer and HTTP edges (`Data/TickConversions`).
- Deleting an automatic segment leaves a tombstone that blocks automatic re-insertion. `AutoSegmentAdmissionPolicy` gates automatic writes so automation never contradicts recorded human intent.
- Per-item disable (`DisabledItems`) filters only the servable read. Analysis and storage are unaffected.
</important>

<important if="you are adding error handling, argument guards, or try/catch">
Internal code trusts its callers. Validation happens at the edges: controllers, configuration, and ffmpeg output. Catch blocks belong to the layers that own recovery (the ffmpeg process runner, the database gates, the analysis pass). A new one anywhere else needs a reason in the diff.
</important>

<important if="you are changing behavior that `docs/segments.md`, `docs/analysis.md` or `docs/credits.md` describes">
Update the doc in the same diff. These docs describe current behavior, so a sentence the diff makes false misleads every later reader.
</important>

<important if="you are writing XML docs or code comments">
Document purpose, invariants, errors, side effects, async or blocking behavior, and examples for non-obvious APIs. Skip comments that only restate names or obvious control flow.
</important>
