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

ADDON_ID = 'service.introskipper'
try:
    ADDON = xbmcaddon.Addon(ADDON_ID)
    ADDON_NAME = ADDON.getAddonInfo('name')
except Exception:
    ADDON = None
    ADDON_NAME = "Intro Skipper"


def log(msg, level=xbmc.LOGINFO):
    xbmc.log(f"[{ADDON_ID}] {msg}", level=level)


def get_setting(key, default=""):
    if ADDON:
        try:
            val = ADDON.getSetting(key)
            return val if val != "" else default
        except Exception:
            return default
    return default


def get_setting_int(key, default=0):
    if ADDON:
        try:
            val = ADDON.getSettingInt(key)
            return val
        except Exception:
            try:
                return int(ADDON.getSetting(key))
            except Exception:
                return default
    return default


def get_localized_string(string_id, default=""):
    if ADDON:
        try:
            val = ADDON.getLocalizedString(string_id)
            if val:
                return val
        except Exception:
            pass
    return default


def get_intro_action():
    val = get_setting('intro_action', '0')
    mapping = {
        '0': 'prompt',
        '1': 'autoskip',
        '2': 'disabled',
        'prompt': 'prompt',
        'autoskip': 'autoskip',
        'disabled': 'disabled'
    }
    return mapping.get(str(val), 'prompt')


def get_credits_action():
    val = get_setting('credits_action', '0')
    mapping = {
        '0': 'prompt',
        '1': 'autoskip',
        '2': 'next_episode',
        '3': 'disabled',
        'prompt': 'prompt',
        'autoskip': 'autoskip',
        'next_episode': 'next_episode',
        'disabled': 'disabled'
    }
    return mapping.get(str(val), 'prompt')


class SkipOverlay(xbmcgui.WindowDialog):
    """
    Non-blocking on-screen Skip button overlay displayed during intro / credits scenes.
    Allows the user to press Select / OK on their remote, or click with a mouse, to skip.
    Pressing Back / Dismiss hides the button for this scene.
    """
    def __init__(self, title, seek_target, duration=None, notify_text="", on_skip=None):
        super().__init__()
        self.seek_target = seek_target
        # If duration > 0, set wall-clock expiration; if None or <= 0, stay visible as long as the segment runs
        self.expires_at = (time.time() + duration) if (duration is not None and duration > 0) else None
        self.notify_text = notify_text
        self.on_skip = on_skip
        self.action_taken = None
        self.is_active = True

        # In standard 1280x720 reference grid:
        # Position button at bottom-right (x: 940, y: 600, w: 280, h: 60)
        self.btn = xbmcgui.ControlButton(
            940, 600, 280, 60,
            f"{title} >>",
            alignment=6,
            textColor="0xFFFFFFFF",
            focusedColor="0xFF111111"
        )
        self.addControl(self.btn)
        self.setFocus(self.btn)

    def trigger_skip(self):
        if not self.is_active:
            return
        self.action_taken = 'skip'
        self.is_active = False
        if self.on_skip:
            try:
                self.on_skip(self.seek_target, self.notify_text)
            except Exception as ex:
                log(f"Error executing on_skip callback: {ex}", xbmc.LOGWARNING)
        self.close()

    def dismiss(self):
        if not self.is_active:
            return
        self.action_taken = 'dismiss'
        self.is_active = False
        self.close()

    def onClick(self, controlId):
        log(f"SkipOverlay clicked (controlId={controlId})")
        self.trigger_skip()

    def onAction(self, action):
        aid = action.getId()
        # ACTION_SELECT_ITEM = 7, ACTION_MOUSE_LEFT_CLICK = 100, ACTION_ENTER = 11
        if aid in (7, 100, 11):
            log(f"SkipOverlay action skip triggered (actionId={aid})")
            self.trigger_skip()
        # ACTION_PREVIOUS_MENU = 10, ACTION_NAV_BACK = 92, ACTION_BACK = 216, ACTION_STOP = 13
        elif aid in (9, 10, 92, 216):
            log(f"SkipOverlay dismissed by user (actionId={aid})")
            self.dismiss()
        else:
            try:
                super().onAction(action)
            except Exception:
                pass


class IntroSkipperPlayer(xbmc.Player):
    def __init__(self, monitor):
        super().__init__()
        self.monitor = monitor
        self.active_segments = []
        self.handled_segments = set()
        self.is_tracking = False
        self.current_dialog = None

    def onAVStarted(self):
        if not self.isPlayingVideo():
            return

        self.cleanup_dialog()
        self.active_segments = []
        self.handled_segments.clear()
        self.is_tracking = False

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
                        self.is_tracking = True
                else:
                    log(f"Server returned status {resp.status}", xbmc.LOGWARNING)
        except Exception as ex:
            log(f"Failed to query segments from {url}: {ex}", xbmc.LOGWARNING)

    def onPlayBackStopped(self):
        self.is_tracking = False
        self.cleanup_dialog()
        self.active_segments = []
        self.handled_segments.clear()

    def onPlayBackEnded(self):
        self.is_tracking = False
        self.cleanup_dialog()
        self.active_segments = []
        self.handled_segments.clear()

    def cleanup_dialog(self):
        if self.current_dialog is not None:
            try:
                self.current_dialog.close()
            except Exception:
                pass
            self.current_dialog = None

    def on_user_skip(self, target_time, notify_text):
        log(f"Applying skip to {target_time:.1f}s")
        self.seekTime(target_time)
        if notify_text:
            xbmcgui.Dialog().notification(
                ADDON_NAME,
                notify_text,
                xbmcgui.NOTIFICATION_INFO,
                2000
            )

    def check_playback(self, current_time):
        # 1. Manage active prompt dialog if visible
        if self.current_dialog is not None:
            if not self.current_dialog.is_active:
                self.current_dialog = None
                return

            # Close overlay if playback has passed the segment end
            if current_time >= self.current_dialog.seek_target:
                log(f"Playback passed segment end ({self.current_dialog.seek_target:.1f}s), closing overlay")
                self.cleanup_dialog()
                return

            # Check if optional wall-clock duration expired
            if self.current_dialog.expires_at is not None and time.time() >= self.current_dialog.expires_at:
                log("Skip prompt duration expired, closing overlay")
                self.cleanup_dialog()
                return

            # Dialog is actively showing; wait for user interaction or segment end
            return

        # Reset handled state if the user seeks backwards before a segment start
        for seg in self.active_segments:
            seg_id = seg.get('id', '')
            start = float(seg.get('start', 0.0))
            if current_time < start - 1.0 and seg_id in self.handled_segments:
                self.handled_segments.remove(seg_id)

        # 2. Check unhandled segments against current playback time
        for seg in self.active_segments:
            seg_id = seg.get('id', '')
            if seg_id in self.handled_segments:
                continue

            seg_type = seg.get('type', '')
            start = float(seg.get('start', 0.0))
            end = float(seg.get('end', 0.0))

            if start <= current_time < end:
                self.handled_segments.add(seg_id)
                log(f"Reached segment '{seg_type}' ({start:.1f}s - {end:.1f}s) at {current_time:.1f}s")

                if seg_type == 'Introduction':
                    action = get_intro_action()
                    # The intro skip button is displayed for the entire duration of the intro
                    duration = None
                    title = get_localized_string(30050, "Skip Intro")
                    notify = get_localized_string(30052, "Intro skipped")
                elif seg_type == 'Credits':
                    action = get_credits_action()
                    duration = get_setting_int('credits_prompt_duration', 8)
                    title = get_localized_string(30051, "Skip Credits")
                    notify = get_localized_string(30053, "Credits skipped")
                else:
                    continue

                if action == 'disabled':
                    log(f"Action for {seg_type} is disabled")
                    continue
                elif action == 'autoskip':
                    log(f"Auto-skipping {seg_type} from {current_time:.1f}s to {end:.1f}s")
                    self.seekTime(end)
                    xbmcgui.Dialog().notification(ADDON_NAME, notify, xbmcgui.NOTIFICATION_INFO, 2000)
                elif action == 'next_episode':
                    log(f"Action for {seg_type} is next_episode: playing next")
                    xbmc.executebuiltin("PlayerControl(Next)")
                elif action == 'prompt':
                    log(f"Displaying on-screen skip button for {seg_type} (active until {end:.1f}s)")
                    self.current_dialog = SkipOverlay(
                        title,
                        end,
                        duration=duration,
                        notify_text=notify,
                        on_skip=self.on_user_skip
                    )
                    self.current_dialog.show()
                    break


class IntroSkipperMonitor(xbmc.Monitor):
    def __init__(self):
        super().__init__()


def run():
    log("Starting IntroSkipper Kodi service...")
    monitor = IntroSkipperMonitor()
    player = IntroSkipperPlayer(monitor)

    while not monitor.abortRequested():
        if monitor.waitForAbort(0.25):
            break

        if not player.is_tracking:
            continue

        try:
            if not player.isPlayingVideo():
                continue
            current_time = player.getTime()
        except Exception:
            # Player may be starting, seeking or transitioning
            continue

        player.check_playback(current_time)

    player.cleanup_dialog()
    log("IntroSkipper Kodi service stopped.")


if __name__ == '__main__':
    run()
