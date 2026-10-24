# -*- coding: utf-8 -*-
# SPDX-FileCopyrightText: 2026 rlauuzo
# SPDX-License-Identifier: GPL-3.0-only

import json
import os
import xml.etree.ElementTree as ET
try:
    import xbmc
    import xbmcaddon
    import xbmcvfs
except ImportError:
    xbmc = None
    xbmcaddon = None
    xbmcvfs = None

ADDON_ID = 'service.introskipper'
if xbmcaddon:
    try:
        ADDON = xbmcaddon.Addon(ADDON_ID)
    except Exception:
        ADDON = None
else:
    ADDON = None


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


def get_config_file_path():
    """
    Path to scan_sources.json inside the addon's profile folder.
    """
    profile_dir = None
    if ADDON:
        try:
            profile_dir = xbmcvfs.translatePath(ADDON.getAddonInfo('profile'))
        except Exception:
            pass

    if not profile_dir:
        profile_dir = os.path.expanduser('~/.kodi/userdata/addon_data/service.introskipper')

    if not os.path.exists(profile_dir):
        try:
            os.makedirs(profile_dir, exist_ok=True)
        except Exception:
            pass
    return os.path.join(profile_dir, 'scan_sources.json')


def load_saved_states():
    """
    Loads saved {path: is_enabled} mapping. Defaults to empty (all disabled).
    """
    config_path = get_config_file_path()
    if os.path.exists(config_path):
        try:
            with open(config_path, 'r', encoding='utf-8') as f:
                return json.load(f)
        except Exception as ex:
            log(f"Failed to load {config_path}: {ex}")
    return {}


def save_saved_states(states):
    """
    Saves {path: is_enabled} mapping to disk.
    """
    config_path = get_config_file_path()
    try:
        with open(config_path, 'w', encoding='utf-8') as f:
            json.dump(states, f, indent=2)
    except Exception as ex:
        log(f"Failed to save {config_path}: {ex}")


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

    saved_states = load_saved_states()
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
                        is_enabled = bool(saved_states.get(path, False))
                        norm = path.rstrip('/\\')
                        content_type = content_map.get(path, content_map.get(norm, 'unset'))
                        sources.append({
                            'name': name,
                            'path': path,
                            'enabled': is_enabled,
                            'contentType': content_type
                        })
    except Exception as ex:
        log(f"Error parsing sources.xml: {ex}")

    return sources


def set_source_enabled(path, enabled):
    """
    Updates the enabled status for a specific path.
    """
    states = load_saved_states()
    states[path] = enabled
    save_saved_states(states)


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
                states = {}
                for s in server_sources:
                    p = s.get('path')
                    if p:
                        states[p] = bool(s.get('enabled', False))
                save_saved_states(states)
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


def push_sources_to_server(server_url, sources_list, timeout=3):
    """
    Pushes an explicit list of sources with their enabled statuses to the server.
    """
    if not server_url:
        return False

    server_url = server_url.rstrip('/')
    payload = {
        'sources': [
            {
                'name': s.get('name', ''),
                'path': s.get('path', ''),
                'enabled': bool(s.get('enabled', False)),
                'contentType': s.get('contentType', 'tvshows')
            }
            for s in sources_list
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
            return resp.status == 200
    except Exception as ex:
        log(f"Failed to push sources to server: {ex}", xbmc.LOGWARNING if xbmc else None)
        return False


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

    sources_list = get_video_sources()
    return [s['path'] for s in sources_list if s.get('enabled', False)]

