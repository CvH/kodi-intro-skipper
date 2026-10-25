# -*- coding: utf-8 -*-
# SPDX-FileCopyrightText: 2026 CvH
# SPDX-License-Identifier: GPL-3.0-only

import json
import urllib.request
try:
    import xbmc
    import xbmcaddon
    import xbmcgui
except ImportError:
    xbmc = None
    xbmcaddon = None
    xbmcgui = None

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


def format_time(seconds):
    mins = int(seconds) // 60
    secs = int(seconds) % 60
    return f"{mins:02d}:{secs:02d}"


def analyze_item():
    if not xbmc:
        print("Kodi xbmc environment not found.")
        return

    # Retrieve metadata from the selected list item
    path = xbmc.getInfoLabel('ListItem.FileNameAndPath')
    if not path:
        path = xbmc.getInfoLabel('ListItem.Path')
    if not path:
        if xbmcgui:
            xbmcgui.Dialog().notification(ADDON_NAME, "No media file selected", xbmcgui.NOTIFICATION_WARNING, 3000)
        return

    series = xbmc.getInfoLabel('ListItem.TVShowTitle')
    title = xbmc.getInfoLabel('ListItem.Title')
    season_str = xbmc.getInfoLabel('ListItem.Season')
    episode_str = xbmc.getInfoLabel('ListItem.Episode')

    season = int(season_str) if season_str and season_str.isdigit() else None
    episode = int(episode_str) if episode_str and episode_str.isdigit() else None

    tag = f"S{season:02d}E{episode:02d}" if season is not None and episode is not None else ""
    display_title = f"{series} {tag}".strip() if series else (title or path)

    p_dialog = xbmcgui.DialogProgress() if xbmcgui else None
    if p_dialog:
        p_dialog.create("Intro Skipper", f"Analyzing {display_title}...\nPlease wait...")
        p_dialog.update(25, "Sending media to IntroSkipper server...")

    server_url = get_server_url()
    url = f"{server_url}/api/scan/item"
    payload = {
        'path': path,
        'series': series if series else None,
        'season': season,
        'episode': episode,
        'title': title if title else None,
        'wait': True
    }

    try:
        data = json.dumps(payload).encode('utf-8')
        req = urllib.request.Request(
            url,
            data=data,
            headers={'Content-Type': 'application/json', 'User-Agent': 'Kodi-IntroSkipper/1.0'}
        )
        if p_dialog:
            p_dialog.update(50, "Analyzing media chapters, fingerprints & credits...")

        with urllib.request.urlopen(req, timeout=120) as resp:
            if p_dialog:
                p_dialog.update(90, "Processing results...")
            if resp.status == 200:
                body = resp.read().decode('utf-8')
                result = json.loads(body)
                segments = result.get('segments', [])

                if p_dialog:
                    p_dialog.close()

                if segments:
                    summary_lines = []
                    for s in segments:
                        st = format_time(s['start'])
                        et = format_time(s['end'])
                        summary_lines.append(f"{s['type']}: {st} - {et} ({s['source']})")

                    msg = f"Detected {len(segments)} segment(s) for {display_title}:\n\n" + "\n".join(summary_lines)
                    if xbmcgui:
                        xbmcgui.Dialog().ok(ADDON_NAME, msg)
                    else:
                        print(msg)
                else:
                    if xbmcgui:
                        xbmcgui.Dialog().notification(
                            ADDON_NAME,
                            f"Analysis complete: No intro or credits detected for {display_title}",
                            xbmcgui.NOTIFICATION_INFO,
                            4000
                        )
                    else:
                        print(f"Analysis complete: No segments detected for {display_title}")
            else:
                if p_dialog:
                    p_dialog.close()
                if xbmcgui:
                    xbmcgui.Dialog().notification(ADDON_NAME, f"Server error: HTTP {resp.status}", xbmcgui.NOTIFICATION_ERROR, 3000)
    except Exception as ex:
        if p_dialog:
            p_dialog.close()
        xbmc.log(f"[service.introskipper.context] Failed to analyze {path}: {ex}", xbmc.LOGWARNING)
        if xbmcgui:
            xbmcgui.Dialog().ok(ADDON_NAME, f"Failed to analyze item:\n{ex}")
        else:
            print(f"Failed to analyze item: {ex}")


if __name__ == '__main__':
    analyze_item()
