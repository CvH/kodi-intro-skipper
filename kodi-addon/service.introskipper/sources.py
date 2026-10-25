# -*- coding: utf-8 -*-
# SPDX-FileCopyrightText: 2026 rlauuzo
# SPDX-License-Identifier: GPL-3.0-only

import json
import os
import xml.etree.ElementTree as ET
try:
    import xbmc
    import xbmcgui
    import xbmcvfs
except ImportError:
    xbmc = None
    xbmcgui = None
    xbmcvfs = None


def log(msg, level=None):
    if xbmc:
        xbmc.log(f"[service.introskipper.sources] {msg}", level or xbmc.LOGINFO)
    else:
        print(f"[service.introskipper.sources] {msg}")


def get_sources_xml_path():
    """
    Finds the Kodi sources.xml file.
    """
    candidates = []
    try:
        translated = xbmcvfs.translatePath('special://userdata/sources.xml')
        if translated:
            candidates.append(translated)
    except Exception:
        pass

    candidates.extend([
        '/storage/.kodi/userdata/sources.xml',
        os.path.expanduser('~/.kodi/userdata/sources.xml'),
    ])

    for path in candidates:
        if path and os.path.exists(path):
            return path
    return candidates[0] if candidates else '/storage/.kodi/userdata/sources.xml'


def get_source_content_types():
    """
    Queries Kodi's video database (MyVideosXXX.db) to extract content types (e.g. 'tvshows', 'movies').
    """
    content_map = {}
    try:
        import glob
        import sqlite3
        db_dirs = []
        if xbmcvfs:
            try:
                trans = xbmcvfs.translatePath('special://database')
                if trans and os.path.exists(trans):
                    db_dirs.append(trans)
            except Exception:
                pass

        db_dirs.extend([
            '/storage/.kodi/userdata/Database',
            os.path.expanduser('~/.kodi/userdata/Database')
        ])

        for db_dir in db_dirs:
            if db_dir and os.path.exists(db_dir):
                db_files = sorted(glob.glob(os.path.join(db_dir, 'MyVideos*.db')))
                if db_files:
                    latest_db = db_files[-1]
                    conn = sqlite3.connect(latest_db)
                    c = conn.cursor()
                    c.execute("SELECT strPath, strContent FROM path WHERE strContent IS NOT NULL AND strContent != ''")
                    for path_str, content_str in c.fetchall():
                        if path_str:
                            content_map[path_str] = content_str
                            content_map[path_str.rstrip('/\\')] = content_str
                    conn.close()
                    break
    except Exception as ex:
        log(f"Failed to query Kodi video database for content types: {ex}")

    return content_map


def get_video_sources():
    """
    Parses sources.xml and returns all video source entries with their enable/disable status
    and their Kodi content type (e.g. 'tvshows', 'movies', 'unset').
    """
    xml_path = get_sources_xml_path()
    sources = []
    if not os.path.exists(xml_path):
        log(f"sources.xml not found at {xml_path}")
        return sources

    content_map = get_source_content_types()

    try:
        tree = ET.parse(xml_path)
        root = tree.getroot()
        video_node = root.find('video')
        if video_node is not None:
            for source in video_node.findall('source'):
                name = source.findtext('name') or "Unnamed Source"
                for path_elem in source.findall('path'):
                    path = (path_elem.text or '').strip()
                    if path:
                        norm = path.rstrip('/\\')
                        content_type = content_map.get(path, content_map.get(norm, 'unset'))
                        sources.append({
                            'name': name,
                            'path': path,
                            'enabled': False,
                            'contentType': content_type
                        })
    except Exception as ex:
        log(f"Error parsing sources.xml: {ex}")

    return sources


def sync_sources_with_server(server_url, timeout=3):
    """
    Synchronizes local sources (from sources.xml) with the server's scan sources list.
    Sends local sources via POST /api/sources/sync.
    Receives the merged server sources, updates local saved_states with server states,
    and returns the server's list of sources.
    """
    if not server_url:
        return get_video_sources()

    server_url = server_url.rstrip('/')
    local_sources = get_video_sources()
    payload = {
        'sources': [
            {
                'name': s['name'],
                'path': s['path'],
                'enabled': s['enabled'],
                'contentType': s.get('contentType', 'unset')
            }
            for s in local_sources
        ]
    }

    try:
        import urllib.request
        data = json.dumps(payload).encode('utf-8')
        req = urllib.request.Request(
            f"{server_url}/api/sources/sync",
            data=data,
            headers={'Content-Type': 'application/json', 'User-Agent': 'Kodi-IntroSkipper/1.0'}
        )
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            if resp.status == 200:
                server_sources = json.loads(resp.read().decode('utf-8'))
                log(f"Synchronized {len(server_sources)} sources with server.")
                return server_sources
    except Exception as ex:
        log(f"Failed to sync sources with server: {ex}", xbmc.LOGWARNING if xbmc else None)

    return local_sources


def fetch_server_sources(server_url, timeout=3):
    """
    Fetches the configured media sources from the server.
    """
    if not server_url:
        return get_video_sources()

    server_url = server_url.rstrip('/')
    try:
        import urllib.request
        req = urllib.request.Request(
            f"{server_url}/api/sources",
            headers={'User-Agent': 'Kodi-IntroSkipper/1.0'}
        )
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            if resp.status == 200:
                return json.loads(resp.read().decode('utf-8'))
    except Exception as ex:
        log(f"Failed to fetch sources from server: {ex}", xbmc.LOGDEBUG if xbmc else None)

    return get_video_sources()


def get_enabled_paths(server_url=None):
    """
    Returns all media source paths that are currently enabled for scanning.
    If server_url is supplied and reachable, queries server sources directly.
    """
    if server_url:
        try:
            sources_list = fetch_server_sources(server_url)
            return [s['path'] for s in sources_list if s.get('enabled', False)]
        except Exception:
            pass

    return []


def get_lan_ip(fallback='127.0.0.1'):
    """
    Detects the current device LAN IP address (excluding loopback).
    """
    if xbmc:
        try:
            kodi_ip = xbmc.getIPAddress().strip()
            if kodi_ip and kodi_ip not in ('127.0.0.1', '0.0.0.0', ''):
                return kodi_ip
        except Exception:
            pass

    try:
        import socket
        s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        s.settimeout(0.5)
        s.connect(('10.255.255.255', 1))
        ip = s.getsockname()[0]
        s.close()
        if ip and ip not in ('127.0.0.1', '0.0.0.0', ''):
            return ip
    except Exception:
        pass

    return fallback


def get_webinterface_info(server_url='http://127.0.0.1:8037'):
    """
    Extracts the webinterface LAN IP, port, and full URL.
    Resolves localhost/127.0.0.1 to the device's actual LAN IP.
    """
    try:
        import urllib.parse
        parsed = urllib.parse.urlsplit(server_url)
        host = parsed.hostname or '127.0.0.1'
        port = parsed.port or 8037
    except Exception:
        host = '127.0.0.1'
        port = 8037

    if host.lower() in ('127.0.0.1', 'localhost', '0.0.0.0', ''):
        lan_ip = get_lan_ip(fallback=host)
    else:
        lan_ip = host

    full_url = f"http://{lan_ip}:{port}"
    return lan_ip, port, full_url


MEDIA_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), 'resources', 'media')
CARD_BG_TEX = os.path.join(MEDIA_DIR, 'card_bg.png')
INNER_BOX_TEX = os.path.join(MEDIA_DIR, 'inner_box.png')
BUTTON_FOCUS_TEX = os.path.join(MEDIA_DIR, 'button_focus.png')
BUTTON_NOFOCUS_TEX = os.path.join(MEDIA_DIR, 'button_nofocus.png')


class WebInterfaceDialog(xbmcgui.WindowDialog if xbmcgui else object):
    """
    Dedicated wide modal card overlay window displaying LAN IP, Port, and Web Dashboard URL.
    """
    def __init__(self, lan_ip, port, full_url, title="IntroSkipper Web Dashboard"):
        if not xbmcgui:
            return
        super().__init__()
        self.lan_ip = str(lan_ip)
        self.port = str(port)
        self.full_url = str(full_url)
        self.title_text = title

        # Layout dimensions for 1280x720 canvas
        card_w, card_h = 740, 460
        card_x = (1280 - card_w) // 2
        card_y = (720 - card_h) // 2

        # 1. Card background image
        card_bg = CARD_BG_TEX if os.path.exists(CARD_BG_TEX) else ""
        self.bg_img = xbmcgui.ControlImage(card_x, card_y, card_w, card_h, card_bg)
        self.addControl(self.bg_img)

        # 2. Header title and subtitle
        self.title_lbl = xbmcgui.ControlLabel(
            card_x + 30, card_y + 25, card_w - 60, 36,
            f"[B]{self.title_text}[/B]",
            font='font16',
            textColor='0xFFFFFFFF',
            alignment=2
        )
        self.addControl(self.title_lbl)

        self.subtitle_lbl = xbmcgui.ControlLabel(
            card_x + 30, card_y + 66, card_w - 60, 26,
            "Connect from any web browser on your home network",
            font='font12',
            textColor='0xFF99AABB',
            alignment=2
        )
        self.addControl(self.subtitle_lbl)

        # 3. Two side-by-side cards: LAN IP and PORT
        box_y = card_y + 105
        box_h = 95
        box1_x = card_x + 40
        box_w = 315
        box2_x = card_x + 385
        inner_bg = INNER_BOX_TEX if os.path.exists(INNER_BOX_TEX) else ""

        # LAN IP Box
        self.box_ip = xbmcgui.ControlImage(box1_x, box_y, box_w, box_h, inner_bg)
        self.addControl(self.box_ip)

        self.lbl_ip_header = xbmcgui.ControlLabel(
            box1_x + 20, box_y + 14, box_w - 40, 20,
            "LOCAL NETWORK (LAN) IP",
            font='font10',
            textColor='0xFF7E98B4'
        )
        self.addControl(self.lbl_ip_header)

        self.lbl_ip_val = xbmcgui.ControlLabel(
            box1_x + 20, box_y + 38, box_w - 40, 42,
            f"[B]{self.lan_ip}[/B]",
            font='font16',
            textColor='0xFFFFFFFF'
        )
        self.addControl(self.lbl_ip_val)

        # PORT Box
        self.box_port = xbmcgui.ControlImage(box2_x, box_y, box_w, box_h, inner_bg)
        self.addControl(self.box_port)

        self.lbl_port_header = xbmcgui.ControlLabel(
            box2_x + 20, box_y + 14, box_w - 40, 20,
            "SERVER PORT",
            font='font10',
            textColor='0xFF7E98B4'
        )
        self.addControl(self.lbl_port_header)

        self.lbl_port_val = xbmcgui.ControlLabel(
            box2_x + 20, box_y + 38, box_w - 40, 42,
            f"[B]{self.port}[/B]",
            font='font16',
            textColor='0xFFFFFFFF'
        )
        self.addControl(self.lbl_port_val)

        # 4. Full URL Box
        url_y = card_y + 215
        url_w = 660
        url_h = 92
        self.box_url = xbmcgui.ControlImage(box1_x, url_y, url_w, url_h, inner_bg)
        self.addControl(self.box_url)

        self.lbl_url_header = xbmcgui.ControlLabel(
            box1_x + 20, url_y + 14, url_w - 40, 20,
            "WEB DASHBOARD URL",
            font='font10',
            textColor='0xFF7E98B4'
        )
        self.addControl(self.lbl_url_header)

        self.lbl_url_val = xbmcgui.ControlLabel(
            box1_x + 20, url_y + 38, url_w - 40, 38,
            f"[COLOR deepskyblue][B]{self.full_url}[/B][/COLOR]",
            font='font14',
            textColor='0xFF00BFFF'
        )
        self.addControl(self.lbl_url_val)

        # 5. Helper instructions note
        self.note_lbl = xbmcgui.ControlLabel(
            card_x + 40, card_y + 322, card_w - 80, 24,
            "Open this link in any browser (phone, PC, tablet) to manage IntroSkipper.",
            font='font11',
            textColor='0xFF7A8899',
            alignment=2
        )
        self.addControl(self.note_lbl)

        # 6. OK Button
        btn_w, btn_h = 200, 48
        btn_x = card_x + (card_w - btn_w) // 2
        btn_y = card_y + 375
        focus_tex = BUTTON_FOCUS_TEX if os.path.exists(BUTTON_FOCUS_TEX) else ""
        nofocus_tex = BUTTON_NOFOCUS_TEX if os.path.exists(BUTTON_NOFOCUS_TEX) else ""

        self.ok_btn = xbmcgui.ControlButton(
            btn_x, btn_y, btn_w, btn_h,
            "OK",
            focusTexture=focus_tex,
            noFocusTexture=nofocus_tex,
            alignment=6,
            textColor="0xFFFFFFFF",
            focusedColor="0xFFFFFFFF"
        )
        self.addControl(self.ok_btn)
        self.setFocus(self.ok_btn)

    def onClick(self, controlId):
        self.close()

    def onAction(self, action):
        aid = action.getId()
        # 7: SELECT_ITEM, 11: ENTER, 100: MOUSE_LEFT_CLICK
        # 9: NAV_BACK, 10: PREVIOUS_MENU, 92: BACK, 216: BACK
        if aid in (7, 11, 100, 9, 10, 92, 216):
            self.close()
        else:
            try:
                super().onAction(action)
            except Exception:
                pass


def show_webinterface_dialog(server_url='http://127.0.0.1:8037', title="IntroSkipper Web Dashboard"):
    """
    Displays a prominent popup dialog with the current LAN IP and port of the web interface.
    Uses the custom wide WindowDialog with dedicated IP/port cards, falling back to textviewer.
    """
    lan_ip, port, full_url = get_webinterface_info(server_url)

    if xbmcgui:
        try:
            dialog = WebInterfaceDialog(lan_ip, port, full_url, title)
            dialog.doModal()
            del dialog
            return
        except Exception as ex:
            log(f"Custom WebInterfaceDialog error, falling back to textviewer: {ex}")

        msg = (
            f"[B]IntroSkipper Web Dashboard[/B]\n\n"
            f"Connect from any web browser on your home network:\n\n"
            f"  • [B]LAN IP:[/B]  {lan_ip}\n"
            f"  • [B]Port:[/B]    {port}\n\n"
            f"Web Dashboard URL:\n"
            f"[COLOR deepskyblue][B]{full_url}[/B][/COLOR]\n\n"
            f"Open this URL in any browser on your phone, PC, or tablet to access settings and scan management."
        )
        try:
            xbmcgui.Dialog().textviewer(title, msg)
        except Exception:
            xbmcgui.Dialog().ok(title, msg)
    else:
        print(f"=== {title} ===\nLAN IP: {lan_ip}\nPort: {port}\nURL: {full_url}")


