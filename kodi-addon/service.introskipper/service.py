# -*- coding: utf-8 -*-
# SPDX-FileCopyrightText: 2026 rlauuzo
# SPDX-License-Identifier: GPL-3.0-only

import json
import math
import os
import sys
import time
import urllib.parse
import urllib.request
import xbmc
import xbmcaddon
import xbmcgui

try:
    import sources
except ImportError:
    try:
        from . import sources
    except ImportError:
        sys.path.append(os.path.dirname(__file__))
        import sources


ADDON_ID = 'service.introskipper'
try:
    ADDON = xbmcaddon.Addon(ADDON_ID)
    ADDON_NAME = ADDON.getAddonInfo('name')
    ADDON_PATH = ADDON.getAddonInfo('path')
except Exception:
    ADDON = None
    ADDON_NAME = "Intro Skipper"
    ADDON_PATH = os.path.dirname(os.path.abspath(__file__))

MEDIA_DIR = os.path.join(ADDON_PATH, 'resources', 'media')
RING_DIR = os.path.join(MEDIA_DIR, 'ring')
BUTTON_FOCUS_TEX = os.path.join(MEDIA_DIR, 'button_focus.png')
BUTTON_NOFOCUS_TEX = os.path.join(MEDIA_DIR, 'button_nofocus.png')


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


def get_server_url():
    return get_setting('server_url', 'http://127.0.0.1:8037').rstrip('/')


# Server <-> Kodi Settings synchronization
_syncing_from_server = False
_path_prefix_client = ""
_path_prefix_server = ""


def pull_settings_from_server(server_url=None):
    """
    Pulls Kodi playback settings from the IntroSkipper server and applies them locally.
    """
    global _syncing_from_server
    if not server_url:
        server_url = get_server_url()
    url = f"{server_url}/api/kodi/settings"

    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'Kodi-IntroSkipper/1.0'})
        with urllib.request.urlopen(req, timeout=3) as resp:
            if resp.status == 200:
                data = json.loads(resp.read().decode('utf-8'))
                intro_act = data.get('introAction', 'prompt')
                credits_act = data.get('creditsAction', 'prompt')
                credits_dur = data.get('creditsPromptDuration', 10)
                client_pfx = data.get('pathPrefixClient', '')
                server_pfx = data.get('pathPrefixServer', '')

                intro_map = {'prompt': '0', 'autoskip': '1', 'disabled': '2'}
                credits_map = {'prompt': '0', 'autoskip': '1', 'nextEpisode': '2', 'next_episode': '2', 'disabled': '3'}

                new_intro = intro_map.get(intro_act, '0')
                new_credits = credits_map.get(credits_act, '0')

                global _path_prefix_client, _path_prefix_server
                _path_prefix_client = client_pfx
                _path_prefix_server = server_pfx

                _syncing_from_server = True
                try:
                    if ADDON:
                        if get_setting('intro_action') != new_intro:
                            ADDON.setSetting('intro_action', new_intro)
                        if get_setting('credits_action') != new_credits:
                            ADDON.setSetting('credits_action', new_credits)
                        if get_setting_int('credits_prompt_duration') != credits_dur:
                            ADDON.setSettingInt('credits_prompt_duration', int(credits_dur))
                finally:
                    _syncing_from_server = False

                log(f"Synchronized playback settings from server (Intro: {intro_act}, Credits: {credits_act})")
                return True
    except Exception as ex:
        log(f"Failed to pull settings from server: {ex}", xbmc.LOGDEBUG)
    return False


def push_settings_to_server(server_url=None):
    """
    Pushes local Kodi playback settings to the IntroSkipper server.
    """
    if _syncing_from_server:
        return False

    if not server_url:
        server_url = get_server_url()
    url = f"{server_url}/api/kodi/settings"

    intro_val = get_setting('intro_action', '0')
    credits_val = get_setting('credits_action', '0')
    intro_map = {'0': 'prompt', '1': 'autoskip', '2': 'disabled'}
    credits_map = {'0': 'prompt', '1': 'autoskip', '2': 'nextEpisode', '3': 'disabled'}

    payload = {
        'introAction': intro_map.get(str(intro_val), 'prompt'),
        'creditsAction': credits_map.get(str(credits_val), 'prompt'),
        'creditsPromptDuration': get_setting_int('credits_prompt_duration', 10)
    }

    try:
        data = json.dumps(payload).encode('utf-8')
        req = urllib.request.Request(
            url,
            data=data,
            headers={'Content-Type': 'application/json', 'User-Agent': 'Kodi-IntroSkipper/1.0'}
        )
        with urllib.request.urlopen(req, timeout=3) as resp:
            if resp.status == 200:
                log("Pushed Kodi playback settings to server successfully.")
                return True
    except Exception as ex:
        log(f"Failed to push settings to server: {ex}", xbmc.LOGWARNING)
    return False



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
    Renders an animated circular countdown ring and remaining seconds inside the button pill.
    Allows the user to press Select / OK on their remote, or click with a mouse, to skip.
    Pressing Back / Dismiss hides the button for this scene.
    """
    def __init__(self, title, start, end, duration=None, notify_text="", on_skip=None):
        super().__init__()
        self.title = title
        self.start = float(start)
        self.end = float(end)
        self.seek_target = float(end)
        self.total_span = max(1.0, self.end - self.start)
        self.expires_at = (time.time() + duration) if (duration is not None and duration > 0) else None
        self.notify_text = notify_text
        self.on_skip = on_skip
        self.action_taken = None
        self.is_active = True
        self.last_frame = -1
        self.last_sec = -1

        # Position button in bottom right corner (1280x720 canvas)
        # width: 330, height: 56, right margin: 50, bottom margin: 50
        btn_w = 330
        btn_h = 56
        x = 1280 - btn_w - 50
        y = 720 - btn_h - 50

        # Check for custom button textures
        focus_tex = BUTTON_FOCUS_TEX if os.path.exists(BUTTON_FOCUS_TEX) else ""
        nofocus_tex = BUTTON_NOFOCUS_TEX if os.path.exists(BUTTON_NOFOCUS_TEX) else ""

        self.btn = xbmcgui.ControlButton(
            x, y, btn_w, btn_h,
            "",
            focusTexture=focus_tex,
            noFocusTexture=nofocus_tex,
            alignment=4,  # Left-aligned, vertically centered
            textColor="0xFFFFFFFF",
            focusedColor="0xFFFFFFFF"
        )
        self.addControl(self.btn)

        # Circle countdown ring image positioned inside the button on the left
        circle_size = 32
        circle_x = x + 16
        circle_y = y + (btn_h - circle_size) // 2
        initial_ring = os.path.join(RING_DIR, "ring_36.png") if os.path.exists(RING_DIR) else ""
        self.circle_img = xbmcgui.ControlImage(
            circle_x, circle_y, circle_size, circle_size,
            initial_ring
        )
        self.addControl(self.circle_img)

        self.setFocus(self.btn)
        # Initialize label and ring frame for the initial start position
        self.update_playback(self.start)

    def update_playback(self, current_time):
        if not self.is_active:
            return

        remaining = max(0.0, self.end - current_time)
        remaining_sec = int(math.ceil(remaining))
        fraction = max(0.0, min(1.0, remaining / self.total_span))

        # Update remaining seconds text on button
        if remaining_sec != self.last_sec:
            self.last_sec = remaining_sec
            self.btn.setLabel(f"           {remaining_sec}s   {self.title} >>")

        # Update circular progress ring frame (starts 100% full circle -> 0% empty circle)
        frame_idx = int(round(fraction * 36))
        if frame_idx != self.last_frame:
            self.last_frame = frame_idx
            ring_file = os.path.join(RING_DIR, f"ring_{frame_idx:02d}.png")
            if os.path.exists(ring_file):
                self.circle_img.setImage(ring_file)

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

        # Apply path mapping if configured from server
        query_path = playing_file
        if _path_prefix_client and _path_prefix_server and query_path.startswith(_path_prefix_client):
            query_path = _path_prefix_server + query_path[len(_path_prefix_client):]
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

            # Update spinning circle and remaining time countdown
            self.current_dialog.update_playback(current_time)
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
                        start,
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

    def onSettingsChanged(self):
        super().onSettingsChanged()
        if not _syncing_from_server:
            log("Kodi settings changed; pushing updates to IntroSkipper server...")
            push_settings_to_server()


def run():
    log("Starting IntroSkipper Kodi service...")
    monitor = IntroSkipperMonitor()
    player = IntroSkipperPlayer(monitor)

    server_url = get_server_url()

    # Initial bidirectional sync on startup
    pull_settings_from_server(server_url)
    try:
        sources.sync_sources_with_server(server_url)
    except Exception as ex:
        log(f"Error during startup source sync: {ex}", xbmc.LOGDEBUG)

    last_periodic_sync = time.time()
    sync_interval = 60.0

    while not monitor.abortRequested():
        if monitor.waitForAbort(0.25):
            break

        now = time.time()
        if now - last_periodic_sync >= sync_interval:
            last_periodic_sync = now
            if not player.is_tracking:
                pull_settings_from_server(server_url)

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
