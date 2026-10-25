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

