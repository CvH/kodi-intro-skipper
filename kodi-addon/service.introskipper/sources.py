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


def get_video_sources():
    """
    Parses sources.xml and returns all video source entries with their enable/disable status.
    Default status is disabled (False).
    """
    xml_path = get_sources_xml_path()
    sources = []
    if not os.path.exists(xml_path):
        log(f"sources.xml not found at {xml_path}")
        return sources

    saved_states = load_saved_states()

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
                        # Default is False unless explicitly saved as True
                        is_enabled = bool(saved_states.get(path, False))
                        sources.append({
                            'name': name,
                            'path': path,
                            'enabled': is_enabled
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


def get_enabled_paths():
    """
    Returns only the paths that are currently enabled for scanning.
    """
    sources = get_video_sources()
    return [s['path'] for s in sources if s['enabled']]
