# -*- coding: utf-8 -*-
# SPDX-FileCopyrightText: 2026 rlauuzo
# SPDX-License-Identifier: GPL-3.0-only

import json
import os
import re
import sys
import time
import urllib.error
import urllib.parse
import urllib.request
try:
    import xbmc
    import xbmcaddon
    import xbmcgui
except ImportError:
    xbmc = None
    xbmcaddon = None
    xbmcgui = None

try:
    import sources
except ImportError:
    try:
        from . import sources
    except ImportError:
        sys.path.append(os.path.dirname(__file__))
        import sources

ADDON_ID = 'service.introskipper'
if xbmcaddon:
    try:
        ADDON = xbmcaddon.Addon(ADDON_ID)
        ADDON_NAME = ADDON.getAddonInfo('name')
    except Exception:
        ADDON = None
        ADDON_NAME = "Intro Skipper"
else:
    ADDON = None
    ADDON_NAME = "Intro Skipper"


def log(msg, level=None):
    if xbmc:
        xbmc.log(f"[service.introskipper.main] {msg}", level=level or xbmc.LOGINFO)
    else:
        print(f"[service.introskipper.main] {msg}")


def get_setting(key, default=""):
    if ADDON:
        try:
            val = ADDON.getSetting(key)
            return val if val != "" else default
        except Exception:
            return default
    return default


def get_server_url():
    return get_setting('server_url', 'http://127.0.0.1:8037').rstrip('/')


def fetch_server_status():
    server_url = get_server_url()
    url = f"{server_url}/api/status"
    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'Kodi-IntroSkipper/1.0'})
        with urllib.request.urlopen(req, timeout=3) as resp:
            if resp.status == 200:
                data = resp.read().decode('utf-8')
                return json.loads(data)
    except Exception as ex:
        log(f"Failed to fetch status from {url}: {ex}", xbmc.LOGWARNING)
    return None


def show_status():
    status = fetch_server_status()
    if status is None:
        xbmcgui.Dialog().ok(
            ADDON_NAME,
            f"Could not connect to IntroSkipper server at:\n{get_server_url()}\n\nPlease verify that the server is running."
        )
        return

    st = status.get('status', 'Unknown')
    queued = status.get('queuedJobs', 0)
    db_path = status.get('segmentDatabasePath', 'Unknown')
    ffmpeg_valid = "Yes" if status.get('fFmpegValid', False) else "No (Chromaprint missing!)"
    ffmpeg_ver = status.get('fFmpegVersion', 'Unknown')

    enabled_paths = sources.get_enabled_paths()
    sources_summary = f"{len(enabled_paths)} enabled" if enabled_paths else "None (default: all disabled)"

    msg = (
        f"[B]Server Status:[/B] {st}\n"
        f"[B]Queued Jobs:[/B] {queued}\n"
        f"[B]Configured Scan Paths:[/B] {sources_summary}\n"
        f"[B]Chromaprint / FFmpeg:[/B] {ffmpeg_valid}\n"
        f"[B]FFmpeg Version:[/B] {ffmpeg_ver}\n"
        f"[B]Database:[/B] {db_path}"
    )
    if not xbmcgui:
        clean_msg = msg.replace('[B]', '').replace('[/B]', '')
        print("=== IntroSkipper Status ===")
        print(clean_msg)
        return

    xbmcgui.Dialog().ok("IntroSkipper Status", msg)


def manage_sources():
    all_sources = sources.get_video_sources()
    if not all_sources:
        if xbmcgui:
            xbmcgui.Dialog().ok(
                ADDON_NAME,
                "No video sources found in Kodi's sources.xml.\n\nPlease add your media folders under Settings -> Media -> Videos first."
            )
        else:
            print("No video sources found in Kodi's sources.xml.")
        return

    if not xbmcgui:
        print(f"=== Scan Sources ({len(all_sources)} found in sources.xml) ===")
        for s in all_sources:
            status_mark = "[X]" if s['enabled'] else "[ ]"
            print(f" {status_mark} {s['name']} ({s['path']})")
        return

    labels = []
    preselect = []
    for i, s in enumerate(all_sources):
        labels.append(f"{s['name']}  ({s['path']})")
        if s['enabled']:
            preselect.append(i)

    dialog = xbmcgui.Dialog()
    selected_indices = dialog.multiselect(
        f"Select Sources to Scan ({len(preselect)}/{len(all_sources)} enabled)",
        labels,
        preselect=preselect
    )

    if selected_indices is None:
        # User cancelled
        return

    states = {}
    enabled_count = 0
    for i, s in enumerate(all_sources):
        is_active = i in selected_indices
        states[s['path']] = is_active
        if is_active:
            enabled_count += 1

    sources.save_saved_states(states)
    dialog.notification(
        ADDON_NAME,
        f"Saved: {enabled_count} of {len(all_sources)} sources enabled",
        xbmcgui.NOTIFICATION_INFO,
        3000
    )


def map_path(path):
    client_prefix = get_setting('path_prefix_client', '').strip()
    server_prefix = get_setting('path_prefix_server', '').strip()
    if client_prefix and server_prefix and path.startswith(client_prefix):
        return server_prefix + path[len(client_prefix):]
    return path


def trigger_scan():
    enabled_paths = sources.get_enabled_paths()
    if not enabled_paths:
        if xbmcgui:
            dialog = xbmcgui.Dialog()
            if dialog.yesno(
                ADDON_NAME,
                "No scan sources are currently enabled.\n\nWould you like to configure scan sources from sources.xml now?"
            ):
                manage_sources()
        else:
            print("No scan sources are currently enabled. Configure scan sources first.")
        return

    if xbmcgui:
        dialog = xbmcgui.Dialog()
        path_list_text = "\n".join([f"- {p}" for p in enabled_paths[:5]])
        if len(enabled_paths) > 5:
            path_list_text += f"\n... and {len(enabled_paths) - 5} more"

        if not dialog.yesno(
            ADDON_NAME,
            f"Start scanning {len(enabled_paths)} enabled source(s)?\n\n{path_list_text}"
        ):
            return
    else:
        print(f"Triggering scan for {len(enabled_paths)} enabled source(s)...")

    server_url = get_server_url()
    scan_url = f"{server_url}/api/scan"
    success_count = 0
    failed_paths = []

    for path in enabled_paths:
        mapped = map_path(path)
        payload = json.dumps({'path': mapped, 'exportEdl': False}).encode('utf-8')
        try:
            req = urllib.request.Request(
                scan_url,
                data=payload,
                headers={'Content-Type': 'application/json', 'User-Agent': 'Kodi-IntroSkipper/1.0'}
            )
            with urllib.request.urlopen(req, timeout=5) as resp:
                if resp.status in (200, 202):
                    success_count += 1
                else:
                    failed_paths.append(f"{mapped} (HTTP {resp.status})")
        except urllib.error.HTTPError as he:
            try:
                err_body = he.read().decode('utf-8')
                err_json = json.loads(err_body)
                err_msg = err_json.get('error', str(he))
            except Exception:
                err_msg = str(he)
            failed_paths.append(f"{mapped}: {err_msg}")
            log(f"Server rejected scan for {mapped}: {err_msg}", xbmc.LOGWARNING if xbmc else None)
        except Exception as ex:
            failed_paths.append(f"{mapped}: {ex}")
            log(f"Failed to trigger scan for {mapped}: {ex}", xbmc.LOGWARNING if xbmc else None)

    if not xbmcgui:
        print(f"Successfully enqueued {success_count} source(s).")
        if failed_paths:
            print(f"Failed {len(failed_paths)} source(s):")
            for fp in failed_paths:
                print(f" - {fp}")
        return

    if failed_paths:
        dialog.ok(
            ADDON_NAME,
            f"Enqueued {success_count} source(s).\n\n{len(failed_paths)} failed:\n" + "\n".join(failed_paths[:4])
        )
    else:
        dialog.notification(
            ADDON_NAME,
            f"Enqueued {success_count} source(s) for scanning",
            xbmcgui.NOTIFICATION_INFO,
            3000
        )

    if success_count > 0 and dialog.yesno(ADDON_NAME, "Scan started. Would you like to view the live scan monitor?"):
        live_monitor()


def live_monitor():
    p_dialog = xbmcgui.DialogProgress()
    p_dialog.create("IntroSkipper Scan Monitor", "Connecting to server...")

    try:
        while not p_dialog.iscanceled():
            status = fetch_server_status()
            if status is None:
                p_dialog.update(0, "Error: Could not reach IntroSkipper server\nReconnecting...")
                time.sleep(2)
                continue

            current_status = status.get('status', 'Idle')
            queued = status.get('queuedJobs', 0)

            # Calculate progress percentage
            percent = 0
            if current_status == "Idle" and queued == 0:
                p_dialog.update(100, "Scan Complete!\nServer is Idle (0 queued jobs).")
                time.sleep(2)
                break
            elif "Scanning" in current_status:
                percent = 15
            else:
                m = re.search(r'season\s+(\d+)/(\d+)', current_status, re.IGNORECASE)
                if m:
                    idx = int(m.group(1))
                    tot = int(m.group(2))
                    percent = max(10, min(95, int((idx / max(1, tot)) * 100)))
                else:
                    percent = 50

            line1 = f"Current Activity: {current_status}"
            line2 = f"Queued jobs remaining: {queued}"
            p_dialog.update(percent, f"{line1}\n{line2}")

            time.sleep(1.5)
    finally:
        p_dialog.close()


def main():
    arg = sys.argv[1].lower() if len(sys.argv) > 1 else ""

    if arg == "manage_sources":
        manage_sources()
        return
    elif arg == "status":
        show_status()
        return
    elif arg == "scan":
        trigger_scan()
        return
    elif arg == "monitor":
        live_monitor()
        return

    # Default interactive menu when launched from Kodi UI
    status = fetch_server_status()
    st_label = status.get('status', 'Offline') if status else "Offline"
    queued_label = f" ({status.get('queuedJobs', 0)} queued)" if status and status.get('queuedJobs', 0) > 0 else ""

    enabled_count = len(sources.get_enabled_paths())
    all_sources = sources.get_video_sources()

    options = [
        f"1. Current Status: [{st_label}{queued_label}]",
        f"2. Start Scan on Enabled Sources ({enabled_count} enabled)",
        f"3. Configure Scan Sources ({enabled_count}/{len(all_sources)} enabled)...",
        "4. Live Scan Monitor",
        "5. Add-on Settings..."
    ]

    choice = xbmcgui.Dialog().select("Intro Skipper", options)
    if choice == 0:
        show_status()
    elif choice == 1:
        trigger_scan()
    elif choice == 2:
        manage_sources()
    elif choice == 3:
        live_monitor()
    elif choice == 4:
        if ADDON:
            ADDON.openSettings()


if __name__ == '__main__':
    main()

