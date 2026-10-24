# Kodi Intro Skipper

<div align="center">
    <p>
        <img alt="Plugin Banner" src="https://raw.githubusercontent.com/intro-skipper/intro-skipper/12.0/images/logo.png" />
    </p>
    <p>
        Standalone intro and credit detection daemon and native Kodi add-on.
    </p>
</div>

> [!NOTE]
> ### Origin & Acknowledgements
> This project is a standalone fork of [intro-skipper/intro-skipper](https://github.com/intro-skipper/intro-skipper), originally created and maintained by the IntroSkipper team for Jellyfin.
>
> It refactors the multi-episode audio fingerprinting (Chromaprint), chapter analysis, black-frame detection, and keyframe credit analysis pipelines into a standalone, lightweight daemon (`IntroSkipper.Server`) and pairs it with a native Kodi service add-on (`service.introskipper`) for playback skipping without requiring Jellyfin.
>
> All original detection algorithms, database schemas, and analyzers remain licensed under the [GNU General Public License v3.0](LICENSE).

---

## Features

- **Native Kodi OSD Skip Button**: A sleek, non-blocking on-screen overlay displayed during intro and credits scenes.
- **Dynamic Circular Countdown**: Starts with a full circular progress ring and smoothly drains to an empty ring as the intro plays, alongside a remaining seconds countdown.
- **Remote & Mouse Friendly**: Press **Select / OK** on your remote or click to skip immediately; press **Back** to dismiss.
- **Configurable Actions**: Choose between **Prompt (Skip Button)**, **Automatically Skip**, **Play Next Episode** (credits), or **Disabled**.
- **Kodi `sources.xml` Integration**: Select and trigger scans directly from your configured Kodi video sources inside the add-on UI.
- **Standalone Daemon**: Fast REST API with a local SQLite database (`introskipper.db`), running independently as a service on your Kodi box, NAS, or home server.

---

## Kodi Add-on (`service.introskipper`)

The Kodi add-on runs as a background service during playback and provides a management interface from the Add-ons menu.

### Installation
Copy the `kodi-addon/service.introskipper` directory to your Kodi addons folder (e.g. `/storage/.kodi/addons/` on LibreELEC, or `~/.kodi/addons/` on generic Linux).

### Add-on Settings & Features

| Setting | Default | Description |
|---|---|---|
| **Server URL** | `http://127.0.0.1:8037` | URL of the running `IntroSkipper.Server` daemon |
| **Intro Action** | `Show Prompt` | Action on intro: `Prompt (Skip Button)`, `Automatically Skip`, or `Disabled` |
| **Credits Action** | `Show Prompt` | Action on credits: `Prompt`, `Automatically Skip`, `Play Next Episode`, or `Disabled` |
| **Credits Duration** | `8s` | Time in seconds to display the credits skip prompt |
| **Client / Server Path Prefix** | *empty* | Path translation if Kodi accesses media via network shares (e.g. `smb://NAS/tv/` → `/storage/tvshows/`) |

### Source Management & Scanning from Kodi
Open **Intro Skipper** from Kodi's Program / Video Add-ons menu:
1. **Configure Sources to Scan**: Lists all video paths found in Kodi's `sources.xml` and lets you enable/disable each path.
2. **Start Scan on Enabled Sources**: Enqueues analysis on the daemon for all enabled folders.
3. **Check Server & Scan Status**: Displays daemon health, FFmpeg/Chromaprint status, database location, and job queue.
4. **Live Scan Monitor**: Opens a real-time progress dialog tracking the current scan.

---

## Standalone Server Daemon (`IntroSkipper.Server`)

The standalone daemon handles all media analysis, audio fingerprinting, and segment storage.

### Running the Server
```bash
./IntroSkipper.Server --urls "http://0.0.0.0:8037"
```

### System Requirements
* **.NET 10 Runtime** (`dotnet-runtime-10.0` or self-contained publish)
* **FFmpeg with Chromaprint support**: `IntroSkipper.Server` automatically uses an `./ffmpeg` binary located in its own folder, or system `ffmpeg` / `jellyfin-ffmpeg`.

### REST API Endpoints

| Endpoint | Method | Description |
|---|---|---|
| `/api/status` | `GET` | Health check, queue count, database path, and FFmpeg / Chromaprint validity |
| `/api/segments` | `GET` | Returns detected segments for a file (`?path=/path/to/media.mkv` or `?series=Title&season=1&episode=2`) |
| `/api/scan` | `POST` | Enqueues a folder for scanning: `{"path": "/path/to/media", "exportEdl": false}` |
| `/api/queue` | `GET` | Lists pending analysis jobs and active season scanning state |

#### Example: Query Segments
```bash
curl "http://127.0.0.1:8037/api/segments?path=%2Fstorage%2Ftvshows%2FShow%2FS01E01.mkv"
```
```json
[
  {
    "id": "01a16d2c-3e4e-7cb1-bb80-ca0d99153e2c",
    "type": "Introduction",
    "start": 0.0,
    "end": 26.026,
    "source": "Chapter"
  },
  {
    "id": "01a16d2c-3ea2-71f1-a53c-0ac91d947a77",
    "type": "Credits",
    "start": 421.504,
    "end": 437.984,
    "source": "Chapter"
  }
]
```

---

## Disclaimer & License

This project is licensed under the [GNU General Public License v3.0](LICENSE).

It is provided as-is, without warranty of any kind, express or implied. Use it at your own risk. The authors accept no liability for data loss, missed detections, false positives, or any other damages arising from its use.

Audio fingerprints (Chromaprint), silence detection, and black frame detection data generated by this tool are derived entirely from your own local media files. No claim of ownership is made over any generated fingerprint or detection data by the authors. Any data you generate is your responsibility.

This project is not affiliated with, endorsed by, or officially connected to the Jellyfin project, Kodi foundation, or any media rights holder. "Kodi" and "Jellyfin" and any related trademarks belong to their respective owners.
