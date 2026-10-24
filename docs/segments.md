# Segments

The server database, `introskipper-v2.db`, is the source of truth for segments. This page covers how segments are stored, written, and served. Analysis records and the detection cache are in `docs/analysis.md`.

## Storage

- **One table for every mode.** `Segments` rows are `(Id, ItemId, Type, StartTicks, EndTicks, Source, State, ConfigHash, CreatedAt, UpdatedAt)` under one unique index on `(ItemId, Type, StartTicks, EndTicks)`. An item can hold any number of segments per type, and no mode is special-cased. `CK_Segments_Range` (`EndTicks > StartTicks AND StartTicks >= 0`) makes a valid range a database constraint.
- **Ticks.** Times are `long` ticks, and equality is exact. Seconds exist only at the analyzer and HTTP edges (`TickConversions`).
- **Shared ids.** `Id` is a Guid v7 generated when created. An automatic row whose boundaries survive re-analysis keeps its id.
- **Provenance.** `SegmentSource` records which analyzer, or the user, produced a row. `Unknown` comes only from legacy imports, and it counts as automatic. No erase deletes a user row, because analysis did not create it.
- **Tombstones.** Deleting an automatic segment sets its state to `SegmentState.Suppressed`. The row stays, hidden from normal reads, and refuses automatic re-detections of the deleted range (see Automatic writes). User segments are deleted outright. Tombstones survive config-hash cleanup and season reanalysis. Restore reactivates one.
- **Migrations.** Schema changes are plain EF migrations on `introskipper-v2.db`, starting from the `InitialCreate` baseline.

## Legacy import

`LegacyDatabaseImporter` reads the old `introskipper.db` once, over a read-only connection. It detects the old schema's shape through `pragma_table_info` and never modifies the file. The import commits atomically with a `DbImportRecord` marker, and the marker, not the file, answers whether the import ran.

## Writes

### Automatic writes

`ReplaceAutoSegmentsAsync` is the only analysis write path. `AutoSegmentAdmissionPolicy` gates what automation may write, so automation never contradicts recorded human intent:

- An automatic segment is dropped when it re-detects a tombstone of the same item and mode: it shares at least one tick with the tombstone and reaches no more than `AutoSegmentAdmissionPolicy.TombstoneToleranceTicks` (3 s) past either of its ends.
- An automatic segment is dropped when it strictly overlaps an active user row of the same item and mode. Touching boundaries do not block; one shared tick does.
- Automatic credits are dropped when they overlap an active introduction.

These rules hold at admission only. User writes are admitted unconditionally, and restore does not re-validate.

A mode serves one automatic result. For Recap and Preview, rows rank by source: Chapter, Subtitle, other automatic sources (Chromaprint, black frame, keyframe visuals, combined credits, legacy imports), then credits-derived previews. Credits-derived rows exist only for Preview. In the other modes every source ranks the same. A row ranks by its source only while its hash is empty or the writer's: a row an earlier configuration produced is stale and ranks below every writer.

- While an active row of a higher rank stands, a lower-ranked write changes nothing and returns 0.
- An admitted write replaces rows of its own rank and retires lower-ranked and stale rows. A row whose range the write repeats keeps its id and takes the writer's source and hash.
- A write whose candidates were all rejected leaves every standing row alone.

`ClearSubtitleSegmentsAsync` deletes a mode's subtitle rows after a complete subtitle scan finds no match.

### Interactive and Intent writes

Mutations commit through `IntroSkipperDatabase.ApplyChangeAsync`. It takes typed intents:
- `AddUserSegmentIntent`: adds a user segment and promotes an exact-range occupant in place.
- `UpdateSegmentIntent`: updates boundaries and promotes the row to user provenance.
- `DeleteSegmentIntent`: deletes a user row or tombstones an automatic row (`SegmentState.Suppressed`).
- `RestoreSegmentIntent`: clears a tombstone and restores an automatic row.
- `ReplaceUserSegmentsForModeIntent`: replaces all active segments of a mode with user ranges.
- `SegmentVisibilityChangeIntent`: toggles per-item disable in `DisabledItems`.

All changes commit atomically with analysis-record bookkeeping.

### Maintenance writes

Analyzer and maintenance code calls the facade directly:
- `ReplaceAutoSegmentsAsync`
- `CleanStaleAutomaticSegmentsAsync`
- `EraseItemsAsync`
- `ForgetItemsAsync`
- `DeleteSegmentsByModeAsync`
- `ClearCreditsDerivedPreviewsAsync`
- `ClearSubtitleSegmentsAsync`
- `ResetItemsForReanalysisAsync`

Pure bookkeeping, such as analysis records, season state and analyzer actions, modifies metadata alone.

## Per-item disable

An episode can be disabled individually via `SegmentVisibilityChangeIntent`.

- **Output only.** Analysis runs and rows are stored regardless of the flag. The filter lives only in the facade's servable read (`GetServableSegmentsAsync`). Re-enabling serves the untouched rows without re-analysis.
- **Storage.** `DisabledItems` holds one row per `ItemId`, and the row's presence is the flag.
