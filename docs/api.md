<!-- SPDX-FileCopyrightText: 2026 CvH -->
<!-- SPDX-License-Identifier: GPL-3.0-only -->

# IntroSkipper Server REST API Reference

The `IntroSkipper.Server` standalone daemon provides a lightweight, high-performance REST API for checking server health, controlling background and on-demand scans, retrieving detected intro/credit segments, inspecting server logs, and monitoring library statistics.

---

## Overview & Conventions

- **Default Base URL:** `http://127.0.0.1:8037`
- **Content Type:** `application/json` (unless requesting raw text)
- **Time Units:** Segment boundary times returned by `/api/segments` and `/api/scan/item` are in floating-point seconds.
- **Network Paths:** Accepts local filesystem paths as well as network share URIs (`smb://`, `nfs://`). Kodi network URIs are routed through the built-in Kodi VFS bridge.

> [!TIP]
> Pipe curl output to `jq` for syntax-highlighted, formatted JSON responses:
> ```bash
> curl -s http://127.0.0.1:8037/api/status | jq .
> ```

---

## Endpoint Summary

| Endpoint | Method | Description |
|---|---|---|
| [`/api/status`](#1-server-status) | `GET` | Server health, queue length, FFmpeg capability, and Kodi VFS state |
| [`/api/scan/status`](#2-scan-progress--activity) | `GET` | Real-time progress, active file, pipeline stage, and rolling log |
| [`/api/scan`](#3-enqueue-library-scan) | `POST` | Enqueue a folder or network share into background analysis queue |
| [`/api/scan/item`](#4-on-demand-episode-scan) | `POST` | Scan a single episode immediately (synchronous or background) |
| [`/api/scan/stop`](#5-stop-active-scan) | `POST` | Cancel running scan, terminate FFmpeg processes, and clear queue |
| [`/api/segments`](#6-query-segments) | `GET` | Query detected intro and credit boundaries for an episode |
| [`/api/stats`](#7-library-statistics) | `GET` | Summary statistics of indexed shows, episodes, segments, and time saved |
| [`/api/logs`](#8-server-logs) | `GET` | Retrieve recent server log lines (JSON or raw plain text) |
| [`/api/config`](#9-configuration-management) | `GET` / `POST` | Retrieve or update server detection configuration and exclusions |
| [`/api/config/reset`](#9-configuration-management) | `POST` | Reset all configuration properties back to default values |
| [`/` or `/settings`](#10-web-dashboard--settings-gui) | `GET` | Standalone interactive web dashboard & settings user interface |

---

## Detailed Endpoint Reference

### 1. Server Status

Returns high-level server health, active queue size, database paths, FFmpeg detection capability, Kodi VFS connection status, and current scanning state.

- **Method:** `GET`
- **Path:** `/api/status`

#### Curl Example
```bash
curl -s http://127.0.0.1:8037/api/status
```

#### Example Response (`200 OK`)
```json
{
  "status": "Idle",
  "queuedJobs": 0,
  "dataDirectory": "/storage/downloads/intro-skipper/",
  "segmentDatabasePath": "/storage/downloads/intro-skipper/introskipper.db",
  "fFmpegVersion": "ffmpeg version 8.1.3-Jellyfin Copyright (c) 2000-2026 the FFmpeg developers",
  "fFmpegValid": true,
  "kodiVfsUrl": "http://127.0.0.1:8080",
  "kodiVfsConnected": true,
  "isScanning": false,
  "currentFile": null,
  "currentStage": "Idle",
  "fileIndex": 0,
  "totalFiles": 0
}
```

---

### 2. Scan Progress & Activity

Monitors real-time progress of library scans. Reports the current stage of the detection pipeline, active file, file index / total count, and recent activity messages.

- **Method:** `GET`
- **Path:** `/api/scan/status`

#### Pipeline Stages
- `Idle`: No active scan.
- `Scanning directories`: Crawling folders and indexing media files.
- `Probing media & chapters`: Reading audio duration, video streams, and chapter cues.
- `Chapter analysis`: Detecting intro/credits markers from embedded chapter names.
- `Chromaprint audio fingerprinting`: Cross-episode audio fingerprint correlation.
- `Credits analysis (keyframe & black frame scan)`: Keyframe credits and black roll detection.
- `Saving detected segments`: Persisting detected boundaries to SQLite database.

#### Curl Example
```bash
curl -s http://127.0.0.1:8037/api/scan/status
```

#### Example Response (`200 OK`)
```json
{
  "isScanning": true,
  "currentFile": "smb://192.168.0.99/Anime/Dan Da Dan (2024)/Staffel 01/DAN DA DAN - S01E01.mkv",
  "currentStage": "Chromaprint audio fingerprinting",
  "fileIndex": 1,
  "totalFiles": 12,
  "recentActivity": [
    "Scanning file [1/12] DAN DA DAN (S01E01)",
    "Probing audio duration and embedded chapters",
    "Running Chromaprint cross-episode fingerprinting"
  ]
}
```

---

### 3. Enqueue Library Scan

Enqueues an entire directory or network share for background scanning. The server crawls the path, parses series/season/episode metadata, groups episodes into season batches, and runs the multi-stage detection pipeline in the background.

- **Method:** `POST`
- **Path:** `/api/scan`
- **Headers:** `Content-Type: application/json`

#### Request Parameters
| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `path` | `string` | **Yes** | — | Filesystem path or network URI (`smb://...`, `nfs://...`). |
| `exportEdl` | `boolean` | No | `false` | If `true`, writes `.edl` cut files alongside media for native Kodi skipping without add-ons. |

#### Curl Example
```bash
curl -s -X POST -H "Content-Type: application/json" \
  -d '{"path": "smb://192.168.0.99/Anime/Dan Da Dan (2024)", "exportEdl": false}' \
  http://127.0.0.1:8037/api/scan
```

#### Example Response (`202 Accepted`)
```json
{
  "message": "Scan enqueued for path: smb://192.168.0.99/Anime/Dan Da Dan (2024)",
  "path": "smb://192.168.0.99/Anime/Dan Da Dan (2024)"
}
```

---

### 4. On-Demand Episode Scan

Scans a single episode on demand. Ideal for context-menu invocation from Kodi, automated download webhooks (e.g., Sonarr), or immediate file analysis.

When sibling episodes in the same season folder are available, the server automatically includes them in the season batch to allow Chromaprint cross-episode audio fingerprinting.

- **Method:** `POST`
- **Path:** `/api/scan/item`
- **Headers:** `Content-Type: application/json`

#### Request Parameters
| Field | Type | Required | Default | Description |
|---|---|---|---|---|
| `path` | `string` | **Yes** | — | Media file path or network URI. |
| `series` | `string` | No | `null` | Show title (auto-detected from path if omitted). |
| `season` | `integer` | No | `null` | Season number (auto-detected if omitted). |
| `episode` | `integer` | No | `null` | Episode number (auto-detected if omitted). |
| `wait` | `boolean` | No | `false` | If `true`, request blocks until analysis finishes and returns detected segments immediately. If `false`, returns `202 Accepted` and processes in background. |
| `exportEdl` | `boolean` | No | `false` | If `true`, exports `.edl` file alongside video upon completion. |

#### Curl Example: Synchronous Analysis (`wait: true`)
```bash
curl -s -X POST -H "Content-Type: application/json" \
  -d '{"path": "smb://192.168.0.99/Anime/Dan Da Dan (2024)/Staffel 01/DAN DA DAN - S01E01 - Wenn das mal nicht der Anfang einer Liebe ist.mkv", "wait": true}' \
  http://127.0.0.1:8037/api/scan/item
```

#### Example Response (`200 OK`)
```json
{
  "path": "smb://192.168.0.99/Anime/Dan Da Dan (2024)/Staffel 01/DAN DA DAN - S01E01 - Wenn das mal nicht der Anfang einer Liebe ist.mkv",
  "series": "DAN DA DAN",
  "season": 1,
  "episode": 1,
  "segments": [
    {
      "id": "01a16d94-dfb3-791a-9cfe-a15e9a7f5c09",
      "type": "Introduction",
      "start": 0.0,
      "end": 89.845,
      "source": "Chromaprint"
    },
    {
      "id": "01a16dd7-1952-7a37-a494-e31312838ab6",
      "type": "Credits",
      "start": 1406.201,
      "end": 1445.241,
      "source": "BlackFrame"
    }
  ],
  "message": "Analysis complete. Found 2 segment(s)."
}
```

#### Curl Example: Asynchronous Background (`wait: false`)
```bash
curl -s -X POST -H "Content-Type: application/json" \
  -d '{"path": "smb://192.168.0.99/Anime/Dan Da Dan (2024)/Staffel 01/DAN DA DAN - S01E02.mkv", "wait": false}' \
  http://127.0.0.1:8037/api/scan/item
```

#### Example Response (`202 Accepted`)
```json
{
  "path": "smb://192.168.0.99/Anime/Dan Da Dan (2024)/Staffel 01/DAN DA DAN - S01E02.mkv",
  "series": "DAN DA DAN",
  "season": 1,
  "episode": 2,
  "segments": [],
  "message": "Analysis enqueued."
}
```

---

### 5. Stop Active Scan

Immediately halts the running scan process, terminates active FFmpeg processes, and clears all pending jobs from the analysis queue.

- **Method:** `POST`
- **Path:** `/api/scan/stop`

#### Curl Example
```bash
curl -s -X POST http://127.0.0.1:8037/api/scan/stop
```

#### Example Response (`200 OK`)
```json
{
  "message": "Scan stopped and queue cleared.",
  "stopped": true
}
```

---

### 6. Query Segments

Retrieves detected segment boundaries for playback skipping. Used by the Kodi add-on when playback starts to register skip intervals.

Supports querying either by exact media path or by series name and season/episode index.

- **Method:** `GET`
- **Path:** `/api/segments`

#### Query Parameters
| Parameter | Type | Required | Description |
|---|---|---|---|
| `path` | `string` | Optional* | Full media file path or URI (URL-encoded). |
| `series` | `string` | Optional* | Show title. |
| `season` | `integer` | Optional | Season number (1-based). |
| `episode` | `integer` | Optional | Episode number (1-based). |

*\*At least one of `path` or `series` must be supplied.*

#### Curl Example: Query by File Path
```bash
curl -s "http://127.0.0.1:8037/api/segments?path=smb%3A%2F%2F192.168.0.99%2FAnime%2FDan%20Da%20Dan%20(2024)%2FStaffel%2001%2FDAN%20DA%20DAN%20-%20S01E01%20-%20Wenn%20das%20mal%20nicht%20der%20Anfang%20einer%20Liebe%20ist.mkv"
```

#### Curl Example: Query by Show / Season / Episode
```bash
curl -s "http://127.0.0.1:8037/api/segments?series=DAN+DA+DAN&season=1&episode=1"
```

#### Example Response (`200 OK`)
```json
[
  {
    "id": "01a16d94-dfb3-791a-9cfe-a15e9a7f5c09",
    "type": "Introduction",
    "start": 0.0,
    "end": 89.845,
    "source": "Chromaprint"
  },
  {
    "id": "01a16dd7-1952-7a37-a494-e31312838ab6",
    "type": "Credits",
    "start": 1406.201,
    "end": 1445.241,
    "source": "BlackFrame"
  }
]
```

*If no segments are detected or item has not been scanned, returns empty array `[]`.*

---

### 7. Library Statistics

Provides an aggregated statistical overview of the entire indexed media library, total detected segments by category, and total accumulated time saved from skipped intro and credit sequences.

- **Method:** `GET`
- **Path:** `/api/stats`

#### Curl Example
```bash
curl -s http://127.0.0.1:8037/api/stats
```

#### Example Response (`200 OK`)
```json
{
  "totalSeries": 1,
  "totalEpisodes": 78,
  "totalSegments": 120,
  "introCount": 65,
  "creditsCount": 55,
  "recapCount": 0,
  "previewCount": 0,
  "totalSkippedSeconds": 9153.94,
  "formattedSkippedTime": "2h 32m 33s"
}
```

---

### 8. Server Logs

Retrieves recent server log entries directly from the persistent rolling log file (`server.log`).

- **Method:** `GET`
- **Path:** `/api/logs`

#### Query Parameters
| Parameter | Type | Required | Default | Description |
|---|---|---|---|---|
| `lines` | `integer` | No | `100` | Number of recent log lines to fetch (max: `1000`). |
| `raw` | `boolean` | No | `false` | If `true`, returns plain text log directly. If `false`, returns structured JSON. |

#### Curl Example: Structured JSON
```bash
curl -s "http://127.0.0.1:8037/api/logs?lines=20"
```

#### Example Response (`200 OK`)
```json
{
  "lines": [
    "2026-10-24 14:15:50.411 [information] Application started.",
    "2026-10-24 14:16:15.197 [information] >>> Scanning file [1/1] DAN DA DAN (S01E01)",
    "2026-10-24 14:16:15.892 [information] >>> Completed analysis (found 2 segment(s))"
  ],
  "totalLines": 3,
  "logPath": "/storage/downloads/intro-skipper/server.log"
}
```

#### Curl Example: Raw Text Stream
```bash
curl -s "http://127.0.0.1:8037/api/logs?lines=20&raw=true"
```

#### Example Response (`200 OK`, `text/plain`)
```text
2026-10-24 14:15:50.411 [information] Application started.
2026-10-24 14:16:15.197 [information] >>> Scanning file [1/1] DAN DA DAN (S01E01)
2026-10-24 14:16:15.892 [information] >>> Completed analysis (found 2 segment(s))
```

---

### 9. Configuration Management

Read, update, or reset server configuration. Changes are persisted automatically to `config.json` in the server data directory and take effect immediately across all background analysis queues.

#### 9.1 Get Current Configuration
- **Method:** `GET`
- **Path:** `/api/config`

##### Curl Example
```bash
curl -s http://127.0.0.1:8037/api/config
```

#### 9.2 Update Configuration
- **Method:** `POST`
- **Path:** `/api/config`
- **Headers:** `Content-Type: application/json`

Supports both full configuration objects and partial updates (only specified fields are modified).

##### Curl Example: Update Analysis Percentage & Add Excluded Series
```bash
curl -s -X POST -H "Content-Type: application/json" \
  -d '{"analysisPercent": 30, "seriesExclusions": ["Sample Show"]}' \
  http://127.0.0.1:8037/api/config
```

#### 9.3 Reset Configuration to Defaults
- **Method:** `POST`
- **Path:** `/api/config/reset`

##### Curl Example
```bash
curl -s -X POST http://127.0.0.1:8037/api/config/reset
```

---

### 10. Web Dashboard & Settings GUI

Serves a full, responsive standalone Single Page Application with interactive controls for all detection rules, live KPI statistics, active scan progress monitoring, quick scan triggering, and exclusion management.

- **Method:** `GET`
- **Path:** `/` or `/settings`

#### Curl Example
```bash
curl -s http://127.0.0.1:8037/
```

Open `http://127.0.0.1:8037/` in any browser to access the graphical settings dashboard.

---

## HTTP Status Codes

| Status Code | Meaning | Common Scenario |
|---|---|---|
| `200 OK` | Request succeeded | Segment lookup, status query, stats, logs, or synchronous scan result |
| `202 Accepted` | Background job accepted | Scan request enqueued or asynchronous item scan queued |
| `400 Bad Request` | Missing or invalid parameter | Malformed JSON body, missing scan path, or missing query identifiers |
| `404 Not Found` | Resource not found | Episode not indexed in media database |
| `500 Internal Error`| Unhandled server exception | Critical database error or internal processing failure |
