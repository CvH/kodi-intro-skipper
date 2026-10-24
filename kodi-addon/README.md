# IntroSkipper Kodi Addon (`service.introskipper`)

A Python 3 service addon for Kodi (Matrix, Nexus, Omega, Piers) that automatically detects and skips TV show introduction sequences and ending credits during playback by connecting to the standalone **IntroSkipper Server**.

---

## Features

- **Prompt or Auto-Skip:**
  - **Prompt Mode:** Displays an on-screen dialog with countdown to skip the intro or credits by pressing Enter/OK/Select on your remote or keyboard.
  - **Auto-Skip Mode:** Seamlessly jumps over the intro or credits without user interaction.
  - **Next Episode Mode:** Automatically triggers next episode playback when credits start.
- **Remote & Local Playback:** Works whether Kodi is running on the same machine as the server, or on a remote device (Android TV, Fire TV, Nvidia Shield, LibreELEC/CoreELEC).
- **Network Path Mapping:** Configurable client-to-server path prefix mapping (e.g. `smb://nas/tv/` to `/mnt/storage/tv/`).
- **Metadata Fallback:** Matches episodes by series title, season, and episode number even if paths differ.

---

## Installation

### Option 1: Install from Zip file in Kodi
1. Zip the `service.introskipper` directory:
   ```bash
   cd kodi-addon
   zip -r service.introskipper-1.0.0.zip service.introskipper/
   ```
2. In Kodi, navigate to **Settings** → **Add-ons** → **Install from zip file**.
3. Select the `service.introskipper-1.0.0.zip` file.

### Option 2: Direct Directory Copy / Symlink
Copy or symlink `service.introskipper` directly into your Kodi addons folder:
- **Linux:** `~/.kodi/addons/service.introskipper`
- **Windows:** `%APPDATA%\Kodi\addons\service.introskipper`
- **Android:** `/Android/data/org.xbmc.kodi/files/.kodi/addons/service.introskipper`
- **LibreELEC / CoreELEC:** `/storage/.kodi/addons/service.introskipper`

Restart Kodi or enable the add-on under **My add-ons** → **Services** → **Intro Skipper**.

---

## Configuration

In Kodi, go to **Settings** → **Add-ons** → **My add-ons** → **Services** → **Intro Skipper** → **Settings**:

1. **Connection:**
   - **Server URL:** The HTTP address of your IntroSkipper standalone server (e.g., `http://192.168.1.100:8037`).
2. **Skip Behavior:**
   - **Intro Action:** `Show Prompt (Skip Button)`, `Automatically Skip`, or `Disabled`.
   - **Credits Action:** `Show Prompt`, `Automatically Skip`, `Play Next Episode`, or `Disabled`.
   - **Prompt Duration:** How many seconds the prompt stays visible.
3. **Path Mapping (Optional):**
   - **Client Path Prefix:** e.g., `smb://192.168.1.50/tv/`
   - **Server Path Prefix:** e.g., `/mnt/storage/tv/`
