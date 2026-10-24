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

To create a self-contained, single-file binary for deployment:

```bash
# Linux x64
dotnet publish IntroSkipper.Server/IntroSkipper.Server.csproj -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish/linux-x64

# Windows x64
dotnet publish IntroSkipper.Server/IntroSkipper.Server.csproj -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -o publish/win-x64
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
| `--data-dir <path>` | `INTROSKIPPER_DATA_DIR` | Binary folder (`AppContext.BaseDirectory`) | Directory where SQLite databases are saved |
| `--ffmpeg-path <path>` | `INTROSKIPPER_FFMPEG_PATH` | `ffmpeg` on `PATH` | Path to `ffmpeg` binary with chromaprint support |
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

## API Endpoints

### 1. Server Status
`GET /api/status`

Returns current background queue status, database paths, and FFmpeg capability:
```json
{
  "status": "Idle",
  "queuedJobs": 0,
  "dataDirectory": "/opt/introskipper",
  "segmentDatabasePath": "/opt/introskipper/introskipper.db",
  "ffmpegVersion": "ffmpeg version 8.1.3-Jellyfin",
  "ffmpegValid": true
}
```

### 2. Enqueue Media Scan
`POST /api/scan`

Scans a folder, groups episodes into seasons, and runs analysis in the background:
```json
{
  "path": "/mnt/storage/tv/Breaking Bad",
  "exportEdl": false
}
```
*Response: `202 Accepted`*

### 3. Query Segments (Used by Kodi Addon)
`GET /api/segments?path={filePath}&series={series}&season={season}&episode={episode}`

Example:
```
GET /api/segments?series=Breaking+Bad&season=1&episode=1
```
Response:
```json
[
  {
    "id": "019485b0-312a-7182-9654-1b32d3f24021",
    "type": "Introduction",
    "start": 124.5,
    "end": 185.0,
    "source": "Fingerprint"
  },
  {
    "id": "019485b0-312a-7182-9654-1b32d3f24022",
    "type": "Credits",
    "start": 2840.0,
    "end": 2915.0,
    "source": "Keyframe"
  }
]
```

---

## Native Kodi EDL (Edit Decision List) Support

If you set `"exportEdl": true` when scanning, IntroSkipper will write `.edl` files alongside each video (e.g., `Episode.edl` next to `Episode.mkv`).

Kodi natively detects `.edl` files in the same folder as the video and automatically cuts/skips marked sections during playback, requiring no addons installed on Kodi.

---

## Kodi Addon Setup

See [kodi-addon/README.md](file:///mnt/d/GIT/intro-skipper/kodi-addon/README.md) for installation and configuration of the `service.introskipper` Kodi Python addon.
