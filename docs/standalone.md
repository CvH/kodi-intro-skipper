# IntroSkipper Standalone Server & Kodi Integration

IntroSkipper can run as an independent, standalone background service without requiring Jellyfin. In this mode, it manages its own local SQLite database, scans directories, detects introduction and ending credit sequences using Chromaprint, Chapter, and Keyframe visual analysis, and serves segment boundaries to Kodi via a REST API or native EDL files.

---

## Architecture Overview

- **No Jellyfin Dependency:** Operates directly on video files and directories.
- **Database at Binary Folder:** The SQLite database (`introskipper.db` and `introskipper-cache.db`) is stored directly in the binary's directory by default (`AppContext.BaseDirectory`), or in a configurable data directory.
- **Service Integration:** Supports native Linux `systemd` daemon integration (`AddSystemd`) and Windows Service (`AddWindowsService`).
- **REST API:** Exposes lightweight endpoints for status, scanning, and segment queries.
- **Kodi Companion Addon:** A Python service addon (`kodi-addon/service.introskipper`) monitors playback and triggers on-screen "Skip Intro" / "Skip Credits" prompts or auto-skipping.

---

## Building the Standalone Server

Build the executable from the repository root:

```bash
dotnet build IntroSkipper.Server/IntroSkipper.Server.csproj -c Release
```

### Build-Time Configurable Paths

IntroSkipper defaults to a structured Kodi service add-on layout, but paths can be customized at build/publish time via MSBuild properties:

| MSBuild Property | Default Value | Description |
|---|---|---|
| `DefaultAddonId` | `service.introskipper` | Service add-on identifier |
| `DefaultAddonDir` | `/storage/.kodi/addons/$(DefaultAddonId)` | Addon installation folder (where `bin/` resides) |
| `DefaultDataDir` | `/storage/.kodi/userdata/addon_data/$(DefaultAddonId)` | Base userdata folder (where `db/`, `logs/`, `config/` reside) |

#### Publishing Self-Contained Single-File Binaries

```bash
# Linux ARM64 (e.g. LibreELEC on Raspberry Pi / Odroid) with default paths
dotnet publish IntroSkipper.Server/IntroSkipper.Server.csproj -c Release -r linux-arm64 \
  --self-contained -p:PublishSingleFile=true -o publish/linux-arm64

# Linux x64 with custom userdata path
dotnet publish IntroSkipper.Server/IntroSkipper.Server.csproj -c Release -r linux-x64 \
  --self-contained -p:PublishSingleFile=true \
  -p:DefaultAddonId=service.introskipper \
  -p:DefaultDataDir=/var/lib/introskipper \
  -o publish/linux-x64

# Windows x64
dotnet publish IntroSkipper.Server/IntroSkipper.Server.csproj -c Release -r win-x64 \
  --self-contained -p:PublishSingleFile=true -o publish/win-x64
```

---

## Directory & Subfolder Layout

When running with default paths on Kodi / LibreELEC, files are organized into dedicated subfolders:

```
/storage/.kodi/
├── addons/service.introskipper/
│   ├── addon.xml
│   ├── service.py, main.py, sources.py, context.py
│   ├── resources/
│   └── bin/
│       ├── IntroSkipper.Server    <-- Standalone server executable
│       ├── ffmpeg                 <-- FFmpeg binary with Chromaprint
│       └── ffprobe                <-- FFprobe binary
│
└── userdata/addon_data/service.introskipper/
    ├── db/
    │   ├── introskipper.db        <-- SQLite segment boundaries & metadata
    │   └── introskipper-cache.db  <-- Raw fingerprint & detection cache
    ├── logs/
    │   └── server.log             <-- Application server log
    └── config/
        ├── config.json            <-- Detection rules & server settings
        └── scan_sources.json      <-- Configured scan sources
```

---

## Running the Server

### 1. Interactive CLI
```bash
./IntroSkipper.Server --urls "http://0.0.0.0:8037"
```

Available command-line flags and environment variables:

| Parameter | Environment Variable | Default | Description |
|---|---|---|---|
| `--data-dir <path>` | `INTROSKIPPER_DATA_DIR` | `/storage/.kodi/userdata/addon_data/service.introskipper/` | Base directory for `db/`, `logs/`, and `config/` |
| `--ffmpeg-path <path>` | `INTROSKIPPER_FFMPEG_PATH` | `bin/ffmpeg` or `PATH` | Path to `ffmpeg` binary with chromaprint support |
| `--port <port>` | `PORT` | `8037` | HTTP port to listen on |
| `--urls <urls>` | `ASPNETCORE_URLS` | `http://0.0.0.0:8037` | Server URL bindings |

### 2. Linux `systemd` Service

1. Copy the published binary to `/opt/introskipper`:
   ```bash
   sudo mkdir -p /opt/introskipper
   sudo cp -r publish/linux-x64/* /opt/introskipper/
   sudo chown -R media:media /opt/introskipper
   ```

2. Install the systemd unit file from [`install/introskipper.service`](file:///mnt/d/GIT/intro-skipper/install/introskipper.service):
   ```bash
   sudo cp install/introskipper.service /etc/systemd/system/
   sudo systemctl daemon-reload
   sudo systemctl enable --now introskipper
   ```

3. Check service status:
   ```bash
   sudo systemctl status introskipper
   ```

### 3. Windows Service

Register as a Windows Service using `sc.exe`:

```cmd
sc.exe create IntroSkipper binPath= "C:\Program Files\IntroSkipper\IntroSkipper.Server.exe"
sc.exe start IntroSkipper
```

---

## REST API Endpoints

The server exposes a complete REST API for status monitoring, scanning, querying segments, inspecting logs, and viewing statistics.

For the complete API documentation with copy-paste `curl` examples and JSON response schemas, see the **[REST API Reference](api.md)**.

Quick summary of endpoints:
- `GET /api/status`: Server health, queue length, FFmpeg capability, and Kodi VFS state.
- `GET /api/scan/status`: Real-time scan progress, pipeline stage, active file, and activity log.
- `POST /api/scan`: Enqueue a directory or network share (`smb://`, `nfs://`) for background analysis.
- `POST /api/scan/item`: On-demand scan for a single episode (synchronous or background).
- `POST /api/scan/stop`: Cancel active scan and clear queue.
- `GET /api/segments`: Query detected intro/credit segments for an episode.
- `GET /api/stats`: Aggregated media library statistics and total time saved.
- `GET /api/logs`: View recent server logs in JSON or raw plain text.

---

## Native Kodi EDL (Edit Decision List) Support

If you set `"exportEdl": true` when scanning, IntroSkipper will write `.edl` files alongside each video (e.g., `Episode.edl` next to `Episode.mkv`).

Kodi natively detects `.edl` files in the same folder as the video and automatically cuts/skips marked sections during playback, requiring no addons installed on Kodi.

---

## Kodi Addon Setup

See [kodi-addon/README.md](file:///mnt/d/GIT/intro-skipper/kodi-addon/README.md) for installation and configuration of the `service.introskipper` Kodi Python addon.
