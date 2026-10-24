# -*- coding: utf-8 -*-
# SPDX-FileCopyrightText: 2026 rlauuzo
# SPDX-License-Identifier: GPL-3.0-only

import json
import time
import urllib.parse
import urllib.request
import xbmc
import xbmcaddon
import xbmcgui

ADDON = xbmcaddon.Addon()
ADDON_ID = ADDON.getAddonInfo('id')
ADDON_NAME = ADDON.getAddonInfo('name')


def log(msg, level=xbmc.LOGINFO):
    xbmc.log(f"[{ADDON_ID}] {msg}", level=level)


def get_setting(key, default=""):
    try:
        val = ADDON.getSetting(key)
        return val if val != "" else default
    except Exception:
        return default


def get_setting_int(key, default=0):
    try:
        val = ADDON.getSettingInt(key)
        return val
    except Exception:
        try:
            return int(ADDON.getSetting(key))
        except Exception:
            return default


class SkipPromptDialog(xbmcgui.WindowXMLDialog):
    """
    On-screen skip prompt dialog allowing the user to press Select/OK or click to skip.
    """
    def __init__(self, *args, **kwargs):
        super().__init__(*args, **kwargs)
        self.confirmed = False

    def onAction(self, action):
        # ACTION_SELECT_ITEM = 7, ACTION_MOUSE_LEFT_CLICK = 100
        if action.getId() in (7, 100):
            self.confirmed = True
            self.close()
        # ACTION_PREVIOUS_MENU = 10, ACTION_NAV_BACK = 92
        elif action.getId() in (10, 92):
            self.confirmed = False
            self.close()


class IntroSkipperPlayer(xbmc.Player):
    def __init__(self, monitor):
        super().__init__()
        self.monitor = monitor
        self.active_segments = []
        self.handled_segments = set()
        self.is_monitoring = False

    def onAVStarted(self):
        if not self.isPlayingVideo():
            return

        self.active_segments = []
        self.handled_segments.clear()

        # Extract playback metadata
        tag = self.getVideoInfoTag()
        series = tag.getTVShowTitle()
        season = tag.getSeason()
        episode = tag.getEpisode()
        playing_file = self.getPlayingFile()

        log(f"Playback started: file='{playing_file}', series='{series}', S{season}E{episode}")

        # Apply path mapping if configured
        client_prefix = get_setting('path_prefix_client', '')
        server_prefix = get_setting('path_prefix_server', '')
        query_path = playing_file
        if client_prefix and server_prefix and query_path.startswith(client_prefix):
            query_path = server_prefix + query_path[len(client_prefix):]
            log(f"Mapped client path to server path: '{query_path}'")

        # Query standalone IntroSkipper server
        server_url = get_setting('server_url', 'http://127.0.0.1:8037').rstrip('/')
        timeout = get_setting_int('request_timeout', 3)

        params = {}
        if query_path:
            params['path'] = query_path
        if series:
            params['series'] = series
        if season > 0:
            params['season'] = str(season)
        if episode > 0:
            params['episode'] = str(episode)

        url = f"{server_url}/api/segments?{urllib.parse.urlencode(params)}"
        try:
            log(f"Requesting segments: {url}")
            req = urllib.request.Request(url, headers={'User-Agent': 'Kodi-IntroSkipper/1.0'})
            with urllib.request.urlopen(req, timeout=timeout) as resp:
                if resp.status == 200:
                    data = resp.read().decode('utf-8')
                    self.active_segments = json.loads(data)
                    log(f"Loaded {len(self.active_segments)} segments from server: {self.active_segments}")
                    if self.active_segments:
                        self.start_playback_loop()
                else:
                    log(f"Server returned status {resp.status}", xbmc.LOGWARNING)
        except Exception as ex:
            log(f"Failed to query segments from {url}: {ex}", xbmc.LOGWARNING)

    def onPlayBackStopped(self):
        self.is_monitoring = False
        self.active_segments = []
        self.handled_segments.clear()

    def onPlayBackEnded(self):
        self.is_monitoring = False
        self.active_segments = []
        self.handled_segments.clear()

    def start_playback_loop(self):
        self.is_monitoring = True
        intro_action = get_setting('intro_action', 'prompt')
        credits_action = get_setting('credits_action', 'prompt')
        intro_duration = get_setting_int('intro_prompt_duration', 6)
        credits_duration = get_setting_int('credits_prompt_duration', 8)

        while self.is_monitoring and self.isPlaying() and not self.monitor.abortRequested():
            try:
                current_time = self.getTime()
            except Exception:
                break

            for seg in self.active_segments:
                seg_id = seg.get('id', '')
                seg_type = seg.get('type', '')
                start = float(seg.get('start', 0.0))
                end = float(seg.get('end', 0.0))

                if seg_id in self.handled_segments:
                    continue

                # Check if current playback is within segment boundaries
                if start <= current_time < end:
                    self.handled_segments.add(seg_id)

                    if seg_type == 'Introduction':
                        self.handle_intro(intro_action, intro_duration, start, end)
                    elif seg_type == 'Credits':
                        self.handle_credits(credits_action, credits_duration, start, end)

            # Monitor loop sleep interval
            if self.monitor.waitForAbort(0.25):
                break

    def handle_intro(self, action, prompt_duration, start, end):
        if action == 'autoskip':
            log(f"Auto-skipping intro from {start:.1f}s to {end:.1f}s")
            self.seekTime(end)
            xbmcgui.Dialog().notification(ADDON_NAME, "Intro skipped", xbmcgui.NOTIFICATION_INFO, 2000)
        elif action == 'prompt':
            log(f"Prompting to skip intro to {end:.1f}s")
            # Show interactive confirmation dialog or notification
            dialog = xbmcgui.Dialog()
            # If user confirms or auto-confirms
            if dialog.yesno(ADDON_NAME, "Skip intro?", yeslabel="Skip", nolabel="Keep Watching", autoclose=prompt_duration * 1000):
                log(f"User skipped intro to {end:.1f}s")
                self.seekTime(end)

    def handle_credits(self, action, prompt_duration, start, end):
        if action == 'autoskip':
            log(f"Auto-skipping credits from {start:.1f}s to {end:.1f}s")
            self.seekTime(end)
            xbmcgui.Dialog().notification(ADDON_NAME, "Credits skipped", xbmcgui.NOTIFICATION_INFO, 2000)
        elif action == 'next_episode':
            log("Credits reached: playing next episode")
            xbmc.executebuiltin("PlayerControl(Next)")
        elif action == 'prompt':
            log(f"Prompting to skip credits to {end:.1f}s")
            dialog = xbmcgui.Dialog()
            if dialog.yesno(ADDON_NAME, "Skip credits?", yeslabel="Skip", nolabel="Keep Watching", autoclose=prompt_duration * 1000):
                log(f"User skipped credits to {end:.1f}s")
                self.seekTime(end)


class IntroSkipperMonitor(xbmc.Monitor):
    def __init__(self):
        super().__init__()


def run():
    log("Starting IntroSkipper Kodi service...")
    monitor = IntroSkipperMonitor()
    player = IntroSkipperPlayer(monitor)

    while not monitor.abortRequested():
        if monitor.waitForAbort(1):
            break

    log("IntroSkipper Kodi service stopped.")


if __name__ == '__main__':
    run()
