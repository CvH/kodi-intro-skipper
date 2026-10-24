// SPDX-FileCopyrightText: 2026 CvH
// SPDX-License-Identifier: GPL-3.0-only

namespace IntroSkipper.Server.Web;

/// <summary>
/// Provides the standalone IntroSkipper web dashboard and settings interface.
/// </summary>
public static class DashboardPage
{
    private const string HtmlContent = """
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="utf-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <title>IntroSkipper Dashboard</title>
  <style>
    @font-face {
      font-family: 'Inter';
      font-style: normal;
      font-weight: 100 900;
      font-display: swap;
      src: url('/fonts/inter.woff2') format('woff2');
    }
    :root {
      --bg: #090d16;
      --bg-card: rgba(17, 24, 39, 0.85);
      --bg-card-hover: rgba(30, 41, 59, 0.9);
      --border: rgba(255, 255, 255, 0.08);
      --border-focus: #6366f1;
      --primary: #6366f1;
      --primary-hover: #4f46e5;
      --primary-glow: rgba(99, 102, 241, 0.25);
      --success: #10b981;
      --warning: #f59e0b;
      --danger: #ef4444;
      --text: #f3f4f6;
      --text-muted: #9ca3af;
      --text-dim: #6b7280;
      --radius: 12px;
      --radius-sm: 8px;
      --shadow: 0 10px 25px -5px rgba(0, 0, 0, 0.5), 0 8px 10px -6px rgba(0, 0, 0, 0.5);
    }
    * { box-sizing: border-box; margin: 0; padding: 0; }
    body {
      font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
      background: var(--bg);
      background-image: radial-gradient(at 10% 20%, rgba(99, 102, 241, 0.08) 0px, transparent 50%),
                        radial-gradient(at 90% 80%, rgba(168, 85, 247, 0.08) 0px, transparent 50%);
      color: var(--text);
      line-height: 1.5;
      min-height: 100vh;
      display: flex;
      flex-direction: column;
    }
    header {
      background: rgba(13, 18, 30, 0.8);
      backdrop-filter: blur(16px);
      border-bottom: 1px solid var(--border);
      position: sticky;
      top: 0;
      z-index: 50;
      padding: 0.85rem 1.5rem;
    }
    .header-content {
      max-width: 1200px;
      margin: 0 auto;
      display: flex;
      align-items: center;
      justify-content: space-between;
    }
    .brand {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      text-decoration: none;
      color: var(--text);
    }
    .brand-icon {
      width: 36px;
      height: 36px;
      background: linear-gradient(135deg, #6366f1, #a855f7);
      border-radius: 10px;
      display: flex;
      align-items: center;
      justify-content: center;
      color: #fff;
      font-weight: 700;
      box-shadow: 0 0 15px var(--primary-glow);
    }
    .brand-title {
      font-weight: 700;
      font-size: 1.15rem;
      letter-spacing: -0.02em;
    }
    .brand-tag {
      font-size: 0.75rem;
      background: rgba(99, 102, 241, 0.2);
      color: #a5b4fc;
      padding: 2px 8px;
      border-radius: 9999px;
      font-weight: 500;
      margin-left: 6px;
    }
    .status-badge {
      display: inline-flex;
      align-items: center;
      gap: 6px;
      padding: 4px 10px;
      border-radius: 9999px;
      font-size: 0.8rem;
      font-weight: 500;
      background: rgba(16, 185, 129, 0.15);
      color: #34d399;
      border: 1px solid rgba(16, 185, 129, 0.25);
    }
    .status-dot {
      width: 8px;
      height: 8px;
      background: #10b981;
      border-radius: 50%;
      animation: pulse 2s infinite;
    }
    .status-badge.scanning {
      background: rgba(245, 158, 11, 0.15);
      color: #fbbf24;
      border-color: rgba(245, 158, 11, 0.25);
    }
    .status-badge.scanning .status-dot { background: #f59e0b; }
    .status-badge.offline {
      background: rgba(239, 68, 68, 0.15);
      color: #f87171;
      border-color: rgba(239, 68, 68, 0.25);
    }
    .status-badge.offline .status-dot { background: #ef4444; animation: none; }
    @keyframes pulse { 0%, 100% { opacity: 1; transform: scale(1); } 50% { opacity: 0.4; transform: scale(0.9); } }
    
    .nav-tabs {
      background: rgba(13, 18, 30, 0.6);
      backdrop-filter: blur(12px);
      border-bottom: 1px solid var(--border);
      padding: 0 1.5rem;
    }
    .tabs-container {
      max-width: 1200px;
      margin: 0 auto;
      display: flex;
      gap: 0.5rem;
      overflow-x: auto;
      scrollbar-width: none;
    }
    .tabs-container::-webkit-scrollbar { display: none; }
    .tab-btn {
      background: none;
      border: none;
      color: var(--text-muted);
      font-size: 0.9rem;
      font-weight: 500;
      padding: 0.9rem 1rem;
      cursor: pointer;
      position: relative;
      white-space: nowrap;
      transition: color 0.15s ease;
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .tab-btn:hover { color: var(--text); }
    .tab-btn.active {
      color: #fff;
      font-weight: 600;
    }
    .tab-btn.active::after {
      content: "";
      position: absolute;
      bottom: -1px;
      left: 0;
      right: 0;
      height: 2px;
      background: linear-gradient(90deg, #6366f1, #a855f7);
      border-radius: 2px;
    }

    main {
      flex: 1;
      max-width: 1200px;
      width: 100%;
      margin: 1.5rem auto 5rem;
      padding: 0 1.5rem;
    }
    .tab-panel { display: none; animation: fadeIn 0.2s ease; }
    .tab-panel.active { display: block; }
    @keyframes fadeIn { from { opacity: 0; transform: translateY(4px); } to { opacity: 1; transform: translateY(0); } }

    .grid-kpi {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
      gap: 1rem;
      margin-bottom: 1.5rem;
    }
    .kpi-card {
      background: var(--bg-card);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      padding: 1.25rem;
      position: relative;
      overflow: hidden;
      box-shadow: var(--shadow);
    }
    .kpi-card::before {
      content: "";
      position: absolute;
      top: 0; left: 0; right: 0; height: 3px;
      background: linear-gradient(90deg, #6366f1, #a855f7);
      opacity: 0.8;
    }
    .kpi-label { font-size: 0.8rem; color: var(--text-muted); text-transform: uppercase; letter-spacing: 0.05em; font-weight: 600; }
    .kpi-value { font-size: 1.85rem; font-weight: 700; color: #fff; margin: 0.35rem 0 0.2rem; }
    .kpi-sub { font-size: 0.78rem; color: var(--text-dim); }

    .card {
      background: var(--bg-card);
      border: 1px solid var(--border);
      border-radius: var(--radius);
      padding: 1.5rem;
      margin-bottom: 1.5rem;
      box-shadow: var(--shadow);
    }
    .card-header {
      margin-bottom: 1.25rem;
      padding-bottom: 0.75rem;
      border-bottom: 1px solid var(--border);
      display: flex;
      justify-content: space-between;
      align-items: center;
    }
    .card-title { font-size: 1.15rem; font-weight: 700; color: #fff; display: flex; align-items: center; gap: 8px; }
    .card-desc { font-size: 0.85rem; color: var(--text-muted); margin-top: 2px; }

    .setting-row {
      display: flex;
      justify-content: space-between;
      align-items: flex-start;
      padding: 0.95rem 0;
      border-bottom: 1px solid rgba(255, 255, 255, 0.04);
      gap: 1.5rem;
    }
    .setting-row:last-child { border-bottom: none; }
    .setting-info { flex: 1; max-width: 650px; }
    .setting-label { font-size: 0.92rem; font-weight: 600; color: var(--text); display: block; margin-bottom: 2px; }
    .setting-desc { font-size: 0.8rem; color: var(--text-muted); line-height: 1.4; }
    .setting-ctrl { min-width: 200px; display: flex; justify-content: flex-end; align-items: center; }

    /* Switch */
    .switch {
      position: relative;
      display: inline-block;
      width: 46px;
      height: 26px;
    }
    .switch input { opacity: 0; width: 0; height: 0; }
    .slider {
      position: absolute;
      inset: 0;
      background-color: #374151;
      border-radius: 26px;
      cursor: pointer;
      transition: 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    }
    .slider:before {
      position: absolute;
      content: "";
      height: 20px;
      width: 20px;
      left: 3px;
      bottom: 3px;
      background-color: #ffffff;
      border-radius: 50%;
      transition: 0.2s cubic-bezier(0.4, 0, 0.2, 1);
      box-shadow: 0 2px 4px rgba(0, 0, 0, 0.2);
    }
    input:checked + .slider { background-color: var(--primary); }
    input:checked + .slider:before { transform: translateX(20px); }

    /* Inputs */
    input[type="text"], input[type="number"], select {
      background: rgba(15, 23, 42, 0.8);
      border: 1px solid var(--border);
      border-radius: var(--radius-sm);
      color: var(--text);
      font-size: 0.9rem;
      padding: 0.5rem 0.75rem;
      outline: none;
      transition: border-color 0.15s ease, box-shadow 0.15s ease;
      width: 100%;
      max-width: 260px;
    }
    input[type="text"]:focus, input[type="number"]:focus, select:focus {
      border-color: var(--border-focus);
      box-shadow: 0 0 0 3px var(--primary-glow);
    }
    .input-unit {
      display: flex;
      align-items: center;
      gap: 6px;
    }
    .unit-tag {
      font-size: 0.8rem;
      color: var(--text-dim);
      min-width: 30px;
    }

    /* Range slider */
    .range-box {
      display: flex;
      align-items: center;
      gap: 12px;
      width: 100%;
      max-width: 260px;
    }
    input[type="range"] {
      flex: 1;
      accent-color: var(--primary);
      cursor: pointer;
    }
    .range-val {
      font-size: 0.85rem;
      font-weight: 600;
      color: #a5b4fc;
      min-width: 40px;
      text-align: right;
    }

    /* Tags editor */
    .tag-container {
      display: flex;
      flex-wrap: wrap;
      gap: 6px;
      margin-bottom: 8px;
    }
    .tag {
      background: rgba(99, 102, 241, 0.2);
      border: 1px solid rgba(99, 102, 241, 0.35);
      color: #e0e7ff;
      border-radius: 9999px;
      padding: 3px 10px;
      font-size: 0.8rem;
      display: inline-flex;
      align-items: center;
      gap: 6px;
    }
    .tag-remove {
      cursor: pointer;
      color: #a5b4fc;
      font-weight: 700;
    }
    .tag-remove:hover { color: #f87171; }

    /* Buttons */
    .btn {
      display: inline-flex;
      align-items: center;
      justify-content: center;
      gap: 6px;
      padding: 0.55rem 1.15rem;
      border-radius: var(--radius-sm);
      font-size: 0.88rem;
      font-weight: 600;
      cursor: pointer;
      border: 1px solid transparent;
      transition: all 0.15s ease;
      text-decoration: none;
    }
    .btn-primary {
      background: linear-gradient(135deg, var(--primary), #8b5cf6);
      color: #ffffff;
      box-shadow: 0 4px 12px var(--primary-glow);
    }
    .btn-primary:hover {
      background: linear-gradient(135deg, var(--primary-hover), #7c3aed);
      transform: translateY(-1px);
    }
    .btn-secondary {
      background: rgba(255, 255, 255, 0.06);
      color: var(--text);
      border-color: var(--border);
    }
    .btn-secondary:hover { background: rgba(255, 255, 255, 0.1); }
    .btn-danger {
      background: rgba(239, 68, 68, 0.15);
      color: #f87171;
      border-color: rgba(239, 68, 68, 0.3);
    }
    .btn-danger:hover { background: rgba(239, 68, 68, 0.25); }

    /* Floating Action Bar */
    .save-bar {
      position: fixed;
      bottom: 1.5rem;
      left: 50%;
      transform: translateX(-50%) translateY(100px);
      background: rgba(17, 24, 39, 0.95);
      backdrop-filter: blur(16px);
      border: 1px solid var(--border);
      border-radius: 9999px;
      padding: 0.75rem 1.5rem;
      display: flex;
      align-items: center;
      gap: 1.25rem;
      box-shadow: 0 20px 30px rgba(0, 0, 0, 0.6);
      z-index: 100;
      transition: transform 0.25s cubic-bezier(0.4, 0, 0.2, 1);
    }
    .save-bar.visible {
      transform: translateX(-50%) translateY(0);
    }
    .save-bar-text {
      font-size: 0.9rem;
      color: #fbbf24;
      display: flex;
      align-items: center;
      gap: 6px;
      font-weight: 500;
    }

    /* Toast */
    .toast {
      position: fixed;
      top: 1.5rem;
      right: 1.5rem;
      background: rgba(17, 24, 39, 0.95);
      border: 1px solid var(--border);
      border-radius: var(--radius-sm);
      padding: 0.85rem 1.25rem;
      display: flex;
      align-items: center;
      gap: 8px;
      font-size: 0.88rem;
      color: #fff;
      box-shadow: var(--shadow);
      z-index: 200;
      transform: translateY(-50px);
      opacity: 0;
      transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
    }
    .toast.show {
      transform: translateY(0);
      opacity: 1;
    }
    .toast.success { border-left: 4px solid var(--success); }
    .toast.error { border-left: 4px solid var(--danger); }

    @keyframes modalFadeIn {
      from { opacity: 0; transform: scale(0.96); }
      to { opacity: 1; transform: scale(1); }
    }

    /* Scan progress bar */
    .progress-bar-wrap {
      width: 100%;
      height: 8px;
      background: rgba(255, 255, 255, 0.08);
      border-radius: 4px;
      overflow: hidden;
      margin: 0.75rem 0;
    }
    .progress-bar-fill {
      height: 100%;
      width: 0%;
      background: linear-gradient(90deg, #6366f1, #a855f7);
      transition: width 0.3s ease;
    }
  </style>
</head>
<body>
  <header>
    <div class="header-content">
      <a href="#" class="brand">
        <div class="brand-icon">IS</div>
        <div>
          <span class="brand-title">IntroSkipper</span>
          <span class="brand-tag">Standalone</span>
        </div>
      </a>
      <div style="display: flex; align-items: center; gap: 12px;">
        <div id="statusBadge" class="status-badge">
          <span class="status-dot"></span>
          <span id="statusText">Connecting...</span>
        </div>
        <button id="resetDefaultsBtn" class="btn btn-secondary" style="font-size: 0.8rem; padding: 0.35rem 0.75rem;">Reset Defaults</button>
      </div>
    </div>
  </header>

  <nav class="nav-tabs">
    <div class="tabs-container">
      <button class="tab-btn active" data-tab="tab-overview">📊 Overview</button>
      <button class="tab-btn" data-tab="tab-kodi">📺 Kodi & Playback</button>
      <button class="tab-btn" data-tab="tab-general">⚙️ General</button>
      <button class="tab-btn" data-tab="tab-audio">🎵 Audio & Fingerprint</button>
      <button class="tab-btn" data-tab="tab-detection">⏱️ Detection Rules</button>
      <button class="tab-btn" data-tab="tab-chapters">📑 Chapters & Patterns</button>
      <button class="tab-btn" data-tab="tab-visuals">🎬 Visuals & Black Frame</button>
    </div>
  </nav>

  <main>
    <!-- TAB 1: OVERVIEW -->
    <div id="tab-overview" class="tab-panel active">
      <div class="grid-kpi">
        <div class="kpi-card">
          <div class="kpi-label">Indexed Shows</div>
          <div id="kpiSeries" class="kpi-value">0</div>
          <div class="kpi-sub">Total series in library</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Analyzed Episodes</div>
          <div id="kpiEpisodes" class="kpi-value">0</div>
          <div class="kpi-sub">Episodes with segments</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Detected Segments</div>
          <div id="kpiSegments" class="kpi-value">0</div>
          <div id="kpiSegmentBreakdown" class="kpi-sub">0 Intros • 0 Credits</div>
        </div>
        <div class="kpi-card">
          <div class="kpi-label">Time Saved</div>
          <div id="kpiTimeSaved" class="kpi-value">0s</div>
          <div class="kpi-sub">Accumulated skip time</div>
        </div>
      </div>

      <!-- Scan status card -->
      <div id="scanStatusCard" class="card" style="display: none;">
        <div class="card-header">
          <div>
            <div class="card-title">🚀 Active Library Scan</div>
            <div id="scanStageLabel" class="card-desc">Analyzing...</div>
          </div>
          <button id="stopScanBtn" class="btn btn-danger">Stop Scan</button>
        </div>
        <div id="scanFileLabel" style="font-size: 0.85rem; color: #a5b4fc; word-break: break-all;"></div>
        <div class="progress-bar-wrap">
          <div id="scanProgressFill" class="progress-bar-fill"></div>
        </div>
        <div id="scanCountsLabel" style="font-size: 0.78rem; color: var(--text-dim);"></div>
      </div>

      <!-- Configured Media Sources -->
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">📂 Configured Media Sources</div>
            <div class="card-desc">Folders and network shares scanned for intro &amp; credit detection</div>
          </div>
          <button id="scanAllSourcesBtn" class="btn btn-primary" style="font-size: 0.85rem; padding: 6px 14px;">▶ Scan All Enabled</button>
        </div>
        
        <div id="sourcesList" style="display: flex; flex-direction: column; gap: 8px; margin-bottom: 16px;">
          <div style="color: var(--text-muted); font-size: 0.85rem;">Loading sources...</div>
        </div>

        <div style="background: rgba(255, 255, 255, 0.03); border: 1px dashed var(--border); border-radius: var(--radius-sm); padding: 12px;">
          <div style="font-size: 0.85rem; font-weight: 600; margin-bottom: 8px; color: var(--text-muted);">Add New Source Path</div>
          <div style="display: flex; gap: 8px; flex-wrap: wrap;">
            <input type="text" id="newSourceName" placeholder="Name (e.g. TV Shows)" style="max-width: 170px;" />
            <input type="text" id="newSourcePath" placeholder="Path (e.g. /storage/tvshows or smb://server/TV)" style="max-width: none; flex: 1; min-width: 200px;" />
            <select id="newSourceContentType" style="max-width: 130px; font-size: 0.85rem;">
              <option value="tvshows">📺 TV Shows</option>
              <option value="movies">🎬 Movies</option>
              <option value="unset">❓ Unset</option>
            </select>
            <button id="addSourceBtn" class="btn btn-secondary">+ Add Source</button>
          </div>
        </div>
      </div>

      <!-- Trigger scan card -->
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">🔍 Scan Folder or Network Share</div>
            <div class="card-desc">Enqueue a local path or network URI (e.g. smb://server/share/TV) for analysis</div>
          </div>
        </div>
        <div style="display: flex; gap: 8px; flex-wrap: wrap; margin-bottom: 10px;">
          <input type="text" id="quickScanPath" placeholder="/storage/tvshows or smb://192.168.0.99/Media/TV" style="max-width: none; flex: 1;" />
          <button id="quickScanBtn" class="btn btn-primary">Start Scan</button>
        </div>
        <div style="display: flex; align-items: center; gap: 8px; font-size: 0.85rem; color: var(--text-muted);">
          <input type="checkbox" id="quickScanEdl" id="quickScanEdl" />
          <label for="quickScanEdl">Export EDL (.edl) cut files alongside media for native Kodi player</label>
        </div>
      </div>

      <!-- Server info -->
      <div class="card">
        <div class="card-header">
          <div class="card-title">ℹ️ Server Environment</div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <span class="setting-label">Segment Database</span>
            <span id="dbPathLabel" class="setting-desc">Loading...</span>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <span class="setting-label">FFmpeg & Chromaprint</span>
            <span id="ffmpegLabel" class="setting-desc">Loading...</span>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <span class="setting-label">Kodi VFS Bridge</span>
            <span id="kodiVfsLabel" class="setting-desc">Loading...</span>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB: KODI & PLAYBACK -->
    <div id="tab-kodi" class="tab-panel">
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Kodi Add-on Playback Behavior</div>
            <div class="card-desc">Configure how the Kodi player handles detected intro and credit markers</div>
          </div>
        </div>

        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_kodiIntroAction" class="setting-label">Introduction Action</label>
            <span class="setting-desc">Action to perform when playback enters a detected intro sequence.</span>
          </div>
          <select id="cfg_kodiIntroAction">
            <option value="prompt">Show On-Screen Skip Button</option>
            <option value="autoskip">Automatically Skip</option>
            <option value="disabled">Disabled (Do nothing)</option>
          </select>
        </div>

        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_kodiCreditsAction" class="setting-label">Credits Action</label>
            <span class="setting-desc">Action to perform when playback reaches ending credits.</span>
          </div>
          <select id="cfg_kodiCreditsAction">
            <option value="prompt">Show On-Screen Skip / Next Button</option>
            <option value="autoskip">Automatically Skip Credits</option>
            <option value="nextEpisode">Jump to Next Episode Immediately</option>
            <option value="disabled">Disabled (Do nothing)</option>
          </select>
        </div>

        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_kodiCreditsPromptDuration" class="setting-label">Credits Prompt Duration</label>
            <span class="setting-desc">Seconds to display the credits skip prompt before hiding.</span>
          </div>
          <div class="input-unit">
            <input type="number" id="cfg_kodiCreditsPromptDuration" min="3" max="60" value="10" />
            <span class="unit-tag">sec</span>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Path Prefix Translation</div>
            <div class="card-desc">Optional prefix mapping between Kodi client mount points and IntroSkipper server paths</div>
          </div>
        </div>

        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_kodiPathPrefixClient" class="setting-label">Client Path Prefix</label>
            <span class="setting-desc">Path prefix as seen by Kodi (e.g. smb://192.168.0.100/Media/ or /storage/recordings/)</span>
          </div>
          <input type="text" id="cfg_kodiPathPrefixClient" placeholder="e.g. smb://192.168.0.100/Media/" style="max-width: none; flex: 1;" />
        </div>

        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_kodiPathPrefixServer" class="setting-label">Server Path Prefix</label>
            <span class="setting-desc">Corresponding path on the IntroSkipper server (e.g. /media/ or smb://192.168.0.100/Media/)</span>
          </div>
          <input type="text" id="cfg_kodiPathPrefixServer" placeholder="e.g. /mnt/media/" style="max-width: none; flex: 1;" />
        </div>
      </div>
    </div>

    <!-- TAB 2: GENERAL -->
    <div id="tab-general" class="tab-panel">
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Automated Detection</div>
            <div class="card-desc">Configure automated background library scanning and season re-analysis</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_autoDetectIntros" class="setting-label">Automatically Analyze New Media</label>
            <span class="setting-desc">Automatically analyze newly indexed media files for skippable segments.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_autoDetectIntros" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_analyzeSeasonZero" class="setting-label">Analyze Season 0 (Specials / Extras)</label>
            <span class="setting-desc">Identify and analyze episodes placed in Specials / Season 0 folders.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_analyzeSeasonZero" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_reanalyzeSettledSeasons" class="setting-label">Re-analyze Settled Seasons</label>
            <span class="setting-desc">Recompute segments against complete season fingerprints once no new episodes are added for the configured delay.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_reanalyzeSettledSeasons" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_settledSeasonDelayHours" class="setting-label">Settled Season Delay</label>
            <span class="setting-desc">Hours without newly added episodes before a season is treated as settled.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_settledSeasonDelayHours" min="0" max="8760" />
              <span class="unit-tag">hrs</span>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_useSettledSeasonChromaprintConsensus" class="setting-label">Require Chromaprint Consensus</label>
            <span class="setting-desc">Require local consensus groups of episodes to confirm shared intro/credit patterns.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_useSettledSeasonChromaprintConsensus" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_settledSeasonChromaprintConsensusEpisodes" class="setting-label">Consensus Episode Threshold</label>
            <span class="setting-desc">Target number of distinct episodes required to confirm a shared consensus pattern.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_settledSeasonChromaprintConsensusEpisodes" min="2" max="1000" />
              <span class="unit-tag">eps</span>
            </div>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Target Segment Modes</div>
            <div class="card-desc">Select which types of media segments should be analyzed and indexed</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info"><span class="setting-label">Introductions</span><span class="setting-desc">Opening title and theme sequences</span></div>
          <div class="setting-ctrl"><label class="switch"><input type="checkbox" id="cfg_scanIntroduction" /><span class="slider"></span></label></div>
        </div>
        <div class="setting-row">
          <div class="setting-info"><span class="setting-label">Credits</span><span class="setting-desc">Ending credits rolls and title cards</span></div>
          <div class="setting-ctrl"><label class="switch"><input type="checkbox" id="cfg_scanCredits" /><span class="slider"></span></label></div>
        </div>
        <div class="setting-row">
          <div class="setting-info"><span class="setting-label">Recaps</span><span class="setting-desc">"Previously on..." recap sequences</span></div>
          <div class="setting-ctrl"><label class="switch"><input type="checkbox" id="cfg_scanRecap" /><span class="slider"></span></label></div>
        </div>
        <div class="setting-row">
          <div class="setting-info"><span class="setting-label">Previews</span><span class="setting-desc">"Next time on..." teaser / preview sequences</span></div>
          <div class="setting-ctrl"><label class="switch"><input type="checkbox" id="cfg_scanPreview" /><span class="slider"></span></label></div>
        </div>
        <div class="setting-row">
          <div class="setting-info"><span class="setting-label">Commercials</span><span class="setting-desc">Commercial breaks and interstitial segments</span></div>
          <div class="setting-ctrl"><label class="switch"><input type="checkbox" id="cfg_scanCommercial" /><span class="slider"></span></label></div>
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Exclusions</div>
            <div class="card-desc">Exclude specific series names or filesystem paths from automatic detection</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <span class="setting-label">Excluded Series</span>
            <span class="setting-desc">Series titles to skip during scanning (case-insensitive). Type name and press Enter.</span>
            <div id="seriesTags" class="tag-container" style="margin-top: 8px;"></div>
            <div style="display: flex; gap: 6px; max-width: 350px;">
              <input type="text" id="addSeriesInput" placeholder="Add series name..." />
              <button id="addSeriesBtn" class="btn btn-secondary" style="padding: 0.4rem 0.8rem;">Add</button>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <span class="setting-label">Excluded Paths</span>
            <span class="setting-desc">Directories or network shares to ignore. Type path and press Enter.</span>
            <div id="pathTags" class="tag-container" style="margin-top: 8px;"></div>
            <div style="display: flex; gap: 6px; max-width: 350px;">
              <input type="text" id="addPathInput" placeholder="/path/to/ignore or smb://..." />
              <button id="addPathBtn" class="btn btn-secondary" style="padding: 0.4rem 0.8rem;">Add</button>
            </div>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB 3: AUDIO & FINGERPRINT -->
    <div id="tab-audio" class="tab-panel">
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Audio Track Analysis</div>
            <div class="card-desc">Chromaprint audio fingerprinting parameters and stream selection</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_analysisPercent" class="setting-label">Audio Track Analysis Percentage</label>
            <span class="setting-desc">Percentage of each episode's audio track analyzed for common intros.</span>
          </div>
          <div class="setting-ctrl">
            <div class="range-box">
              <input type="range" id="cfg_analysisPercent" min="1" max="50" step="1" />
              <span id="analysisPercentVal" class="range-val">25%</span>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_analysisLengthLimit" class="setting-label">Analysis Length Limit</label>
            <span class="setting-desc">Upper limit on the length of audio analyzed per episode.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_analysisLengthLimit" min="1" max="60" />
              <span class="unit-tag">min</span>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_firstEpisodeIntroMode" class="setting-label">First Episode Intro Handling</label>
            <span class="setting-desc">How to handle the first episode of a season (pilot episodes often lack standard theme songs).</span>
          </div>
          <div class="setting-ctrl">
            <select id="cfg_firstEpisodeIntroMode">
              <option value="Analyze">Analyze Normally</option>
              <option value="Ignore">Ignore First Episode</option>
              <option value="IgnoreAnime">Ignore First Episode for Anime</option>
            </select>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_preferChromaprint" class="setting-label">Prefer Chromaprint over Chapters</label>
            <span class="setting-desc">Run Chromaprint ahead of embedded chapter marks for intros and recaps.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_preferChromaprint" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_probeAudioDuration" class="setting-label">Probe Audio Duration with FFmpeg</label>
            <span class="setting-desc">Directly probe the audio stream duration to avoid inflated timestamps caused by subtitle streams.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_probeAudioDuration" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_preferredAudioLanguage" class="setting-label">Preferred Audio Language</label>
            <span class="setting-desc">Language tag (e.g. jpn, eng, ger) to select for fingerprinting when multiple audio streams exist.</span>
          </div>
          <div class="setting-ctrl">
            <input type="text" id="cfg_preferredAudioLanguage" placeholder="e.g. jpn, eng" />
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_preferAudioStreamWithMostChannels" class="setting-label">Prefer Stream with Most Channels</label>
            <span class="setting-desc">Use standard FFmpeg default-disposition or highest channel count stream selection.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_preferAudioStreamWithMostChannels" /><span class="slider"></span></label>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB 4: DETECTION RULES -->
    <div id="tab-detection" class="tab-panel">
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Introduction Boundaries</div>
            <div class="card-desc">Duration constraints for accepted opening sequences</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_minimumIntroDuration" class="setting-label">Minimum Intro Duration</label>
            <span class="setting-desc">Minimum length of similar audio to be considered an introduction.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_minimumIntroDuration" min="1" max="600" />
              <span class="unit-tag">sec</span>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_maximumIntroDuration" class="setting-label">Maximum Intro Duration</label>
            <span class="setting-desc">Maximum allowable length for an introduction segment.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_maximumIntroDuration" min="10" max="600" />
              <span class="unit-tag">sec</span>
            </div>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Credits Boundaries</div>
            <div class="card-desc">Duration thresholds and visual refinement for ending sequences</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_minimumCreditsDuration" class="setting-label">Minimum Credits Duration</label>
            <span class="setting-desc">Minimum duration of ending credits sequence.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_minimumCreditsDuration" min="1" max="600" />
              <span class="unit-tag">sec</span>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_maximumCreditsDuration" class="setting-label">Maximum Credits Duration</label>
            <span class="setting-desc">Upper limit on the credits sequence length analyzed.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_maximumCreditsDuration" min="10" max="1800" />
              <span class="unit-tag">sec</span>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_refineCreditsBoundary" class="setting-label">Refine Credits Boundary (Frame Accuracy)</label>
            <span class="setting-desc">Decode frames between keyframes to pinpoint exact frame cut to credits.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_refineCreditsBoundary" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_detectNonBlackCredits" class="setting-label">Detect Low-Saturation Card Credits</label>
            <span class="setting-desc">Detect text on uniform grey, white, or muted colour cards from keyframe scan.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_detectNonBlackCredits" /><span class="slider"></span></label>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Recaps & Previews</div>
            <div class="card-desc">Settings for teaser and recap detection</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_minimumRecapDuration" class="setting-label">Minimum Recap Duration</label>
            <span class="setting-desc">Minimum length of similar audio considered a recap.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_minimumRecapDuration" min="1" max="600" />
              <span class="unit-tag">sec</span>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_maximumRecapDuration" class="setting-label">Maximum Recap Duration</label>
            <span class="setting-desc">Maximum allowable duration for a recap.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_maximumRecapDuration" min="10" max="600" />
              <span class="unit-tag">sec</span>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_animePreviewFromCreditsEnd" class="setting-label">Anime Preview From Credits End</label>
            <span class="setting-desc">Automatically mark content between ending credits and file end as Preview for anime.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_animePreviewFromCreditsEnd" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_enableSubtitleRecapDetection" class="setting-label">Detect Subtitle Recaps</label>
            <span class="setting-desc">Detect recaps from text subtitle cues matching recap phrases.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_enableSubtitleRecapDetection" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_enableSubtitlePreviewDetection" class="setting-label">Detect Subtitle Previews</label>
            <span class="setting-desc">Detect previews from text subtitle cues matching preview phrases.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_enableSubtitlePreviewDetection" /><span class="slider"></span></label>
          </div>
        </div>
      </div>
    </div>

    <!-- TAB 5: CHAPTERS & PATTERNS -->
    <div id="tab-chapters" class="tab-panel">
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Chapter Marker Rules</div>
            <div class="card-desc">Rules for reading and matching chapter cues</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_fullLengthChapters" class="setting-label">Full Length Chapters</label>
            <span class="setting-desc">Do not enforce minimum or maximum duration limits on matched chapter markers.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_fullLengthChapters" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_enableSponsorBlockChapterDetection" class="setting-label">Enable SponsorBlock Labels</label>
            <span class="setting-desc">Detect known SponsorBlock chapter names in addition to regular expressions.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_enableSponsorBlockChapterDetection" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_useEmbeddedChaptersForAnalysis" class="setting-label">Use Embedded File Chapters</label>
            <span class="setting-desc">Always read chapters directly embedded in the media file via ffprobe.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_useEmbeddedChaptersForAnalysis" /><span class="slider"></span></label>
          </div>
        </div>
      </div>

      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Chapter Regular Expressions</div>
            <div class="card-desc">Regular expression patterns matched against chapter labels</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_chapterAnalyzerIntroductionPattern" class="setting-label">Introduction Pattern</label>
            <span class="setting-desc">Regex matched for opening titles and themes.</span>
          </div>
          <div class="setting-ctrl" style="min-width: 320px;">
            <input type="text" id="cfg_chapterAnalyzerIntroductionPattern" style="max-width: none;" />
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_chapterAnalyzerEndCreditsPattern" class="setting-label">Credits Pattern</label>
            <span class="setting-desc">Regex matched for ending credits chapters.</span>
          </div>
          <div class="setting-ctrl" style="min-width: 320px;">
            <input type="text" id="cfg_chapterAnalyzerEndCreditsPattern" style="max-width: none;" />
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_chapterAnalyzerRecapPattern" class="setting-label">Recap Pattern</label>
            <span class="setting-desc">Regex matched for recap chapters.</span>
          </div>
          <div class="setting-ctrl" style="min-width: 320px;">
            <input type="text" id="cfg_chapterAnalyzerRecapPattern" style="max-width: none;" />
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_chapterAnalyzerPreviewPattern" class="setting-label">Preview Pattern</label>
            <span class="setting-desc">Regex matched for preview / teaser chapters.</span>
          </div>
          <div class="setting-ctrl" style="min-width: 320px;">
            <input type="text" id="cfg_chapterAnalyzerPreviewPattern" style="max-width: none;" />
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_chapterAnalyzerCommercialPattern" class="setting-label">Commercial Pattern</label>
            <span class="setting-desc">Regex matched for commercial break chapters.</span>
          </div>
          <div class="setting-ctrl" style="min-width: 320px;">
            <input type="text" id="cfg_chapterAnalyzerCommercialPattern" style="max-width: none;" />
          </div>
        </div>
      </div>
    </div>

    <!-- TAB 6: VISUALS & BLACK FRAMES -->
    <div id="tab-visuals" class="tab-panel">
      <div class="card">
        <div class="card-header">
          <div>
            <div class="card-title">Black Frame & Visual Boundaries</div>
            <div class="card-desc">Visual analyzer thresholds for credits rolls and segment alignment</div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_useLegacyBlackFrameAnalyzer" class="setting-label">Use Legacy Black Frame Analyzer</label>
            <span class="setting-desc">Fallback to legacy analyzer for systems where keyframe analyzer is unsupported.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_useLegacyBlackFrameAnalyzer" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_blackFrameMinimumPercentage" class="setting-label">Minimum Black Pixel Percentage</label>
            <span class="setting-desc">Minimum percentage of pixels in a frame that must be black.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_blackFrameMinimumPercentage" min="50" max="100" />
              <span class="unit-tag">%</span>
            </div>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_blackFrameThreshold" class="setting-label">Black Pixel Luminance Threshold</label>
            <span class="setting-desc">Luminance threshold (0-255) below which a pixel is considered black (default: 28).</span>
          </div>
          <div class="setting-ctrl">
            <input type="number" id="cfg_blackFrameThreshold" min="1" max="100" />
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_adjustIntroBasedOnSilence" class="setting-label">Adjust Boundaries Based on Silence</label>
            <span class="setting-desc">Snap intro and credits boundaries to audio silence points.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_adjustIntroBasedOnSilence" /><span class="slider"></span></label>
          </div>
        </div>
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_snapToKeyframe" class="setting-label">Snap to Nearest Keyframe</label>
            <span class="setting-desc">Align cut boundaries to nearest video keyframe for seamless visual skipping.</span>
          </div>
          <div class="setting-ctrl">
            <label class="switch"><input type="checkbox" id="cfg_snapToKeyframe" /><span class="slider"></span></label>
          </div>
        </div>
      </div>
    </div>
  </main>

  <!-- Floating Save Bar -->
  <div id="saveBar" class="save-bar">
    <div class="save-bar-text">
      <span>●</span>
      <span>You have unsaved changes</span>
    </div>
    <div style="display: flex; gap: 8px;">
      <button id="discardChangesBtn" class="btn btn-secondary">Discard</button>
      <button id="saveChangesBtn" class="btn btn-primary">Save Changes</button>
    </div>
  </div>

  <!-- Toast Notification -->
  <div id="toast" class="toast">
    <span id="toastIcon">✔</span>
    <span id="toastMsg">Notification</span>
  </div>

  <!-- Confirm Enable Non-TV Source Modal -->
  <div id="confirmSourceModal" style="display: none; position: fixed; inset: 0; background: rgba(0, 0, 0, 0.75); backdrop-filter: blur(4px); z-index: 9999; align-items: center; justify-content: center; padding: 20px;">
    <div style="background: #18181b; border: 1px solid rgba(245, 158, 11, 0.4); border-radius: var(--radius-md); max-width: 460px; width: 100%; padding: 22px; box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.6); animation: modalFadeIn 0.15s ease-out;">
      <div style="display: flex; align-items: center; gap: 10px; margin-bottom: 14px;">
        <span style="font-size: 1.5rem; line-height: 1;">⚠️</span>
        <h3 style="font-size: 1.05rem; font-weight: 700; color: #fbbf24; margin: 0;">Enable Non-TV Show Source?</h3>
      </div>
      <p style="font-size: 0.88rem; color: var(--text-main); line-height: 1.5; margin-bottom: 12px;">
        The source <strong id="confirmModalSourceName" style="color: #fff;"></strong> is configured as <span id="confirmModalContentType" style="display: inline-block; font-size: 0.75rem; padding: 1px 7px; border-radius: 9999px; background: rgba(245, 158, 11, 0.2); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.4); font-weight: 600;"></span> in Kodi.
      </p>
      <div style="background: rgba(245, 158, 11, 0.08); border-left: 3px solid #f59e0b; padding: 10px 12px; border-radius: 4px; font-size: 0.82rem; color: #fbbf24; line-height: 1.45; margin-bottom: 18px;">
        Intro and credit detection requires episodic television series. Scanning movies or non-episodic media will not detect intro sequences and will waste analysis time.
      </div>
      <p style="font-size: 0.85rem; color: var(--text-dim); margin-bottom: 20px;">
        Are you really sure you want to enable automatic scanning for this source?
      </p>
      <div style="display: flex; justify-content: flex-end; gap: 10px;">
        <button id="confirmModalCancelBtn" class="btn btn-secondary" style="padding: 7px 16px;">Cancel</button>
        <button id="confirmModalOkBtn" class="btn btn-primary" style="background: #f59e0b; border-color: #d97706; color: #000; font-weight: 600; padding: 7px 16px;">Enable Anyway</button>
      </div>
    </div>
  </div>

  <script>
    let activeConfig = {};
    let isDirty = false;
    let scanPollTimer = null;

    // Elements
    const saveBar = document.getElementById('saveBar');
    const toast = document.getElementById('toast');
    const toastMsg = document.getElementById('toastMsg');
    const toastIcon = document.getElementById('toastIcon');

    function showToast(msg, isSuccess = true) {
      toast.className = 'toast ' + (isSuccess ? 'success' : 'error') + ' show';
      toastMsg.textContent = msg;
      toastIcon.textContent = isSuccess ? '✔' : '✖';
      setTimeout(() => { toast.classList.remove('show'); }, 3500);
    }

    function markDirty() {
      if (!isDirty) {
        isDirty = true;
        saveBar.classList.add('visible');
      }
    }

    function clearDirty() {
      isDirty = false;
      saveBar.classList.remove('visible');
    }

    // Tab Navigation
    document.querySelectorAll('.tab-btn').forEach(btn => {
      btn.addEventListener('click', () => {
        document.querySelectorAll('.tab-btn').forEach(b => b.classList.remove('active'));
        document.querySelectorAll('.tab-panel').forEach(p => p.classList.remove('active'));
        btn.classList.add('active');
        document.getElementById(btn.dataset.tab).classList.add('active');
      });
    });

    // Range slider live feedback
    const rangeSlider = document.getElementById('cfg_analysisPercent');
    const rangeVal = document.getElementById('analysisPercentVal');
    rangeSlider.addEventListener('input', () => {
      rangeVal.textContent = rangeSlider.value + '%';
      markDirty();
    });

    // Tag list helpers
    function renderTags(containerId, list, onChange) {
      const el = document.getElementById(containerId);
      el.innerHTML = '';
      list.forEach((item, idx) => {
        const tag = document.createElement('span');
        tag.className = 'tag';
        tag.innerHTML = `${item} <span class="tag-remove" data-idx="${idx}">×</span>`;
        tag.querySelector('.tag-remove').addEventListener('click', () => {
          list.splice(idx, 1);
          renderTags(containerId, list, onChange);
          onChange();
        });
        el.appendChild(tag);
      });
    }

    let seriesExclusions = [];
    let pathExclusions = [];

    document.getElementById('addSeriesBtn').addEventListener('click', () => {
      const inp = document.getElementById('addSeriesInput');
      const val = inp.value.trim();
      if (val && !seriesExclusions.includes(val)) {
        seriesExclusions.push(val);
        inp.value = '';
        renderTags('seriesTags', seriesExclusions, markDirty);
        markDirty();
      }
    });
    document.getElementById('addSeriesInput').addEventListener('keydown', e => {
      if (e.key === 'Enter') { e.preventDefault(); document.getElementById('addSeriesBtn').click(); }
    });

    document.getElementById('addPathBtn').addEventListener('click', () => {
      const inp = document.getElementById('addPathInput');
      const val = inp.value.trim();
      if (val && !pathExclusions.includes(val)) {
        pathExclusions.push(val);
        inp.value = '';
        renderTags('pathTags', pathExclusions, markDirty);
        markDirty();
      }
    });
    document.getElementById('addPathInput').addEventListener('keydown', e => {
      if (e.key === 'Enter') { e.preventDefault(); document.getElementById('addPathBtn').click(); }
    });

    // Populate inputs from config object
    function populateForm(cfg) {
      activeConfig = JSON.parse(JSON.stringify(cfg));
      clearDirty();

      // Checkboxes
      const boolKeys = [
        'autoDetectIntros', 'analyzeSeasonZero', 'reanalyzeSettledSeasons',
        'useSettledSeasonChromaprintConsensus', 'scanIntroduction', 'scanCredits',
        'scanRecap', 'scanPreview', 'scanCommercial', 'preferChromaprint',
        'probeAudioDuration', 'preferAudioStreamWithMostChannels',
        'refineCreditsBoundary', 'detectNonBlackCredits', 'animePreviewFromCreditsEnd',
        'enableSubtitleRecapDetection', 'enableSubtitlePreviewDetection',
        'fullLengthChapters', 'enableSponsorBlockChapterDetection',
        'useEmbeddedChaptersForAnalysis', 'useLegacyBlackFrameAnalyzer',
        'adjustIntroBasedOnSilence', 'snapToKeyframe'
      ];
      boolKeys.forEach(k => {
        const el = document.getElementById('cfg_' + k);
        if (el) {
          el.checked = Boolean(cfg[k]);
          el.onchange = markDirty;
        }
      });

      // Numbers & Text
      const valKeys = [
        'settledSeasonDelayHours', 'settledSeasonChromaprintConsensusEpisodes',
        'analysisLengthLimit', 'preferredAudioLanguage', 'minimumIntroDuration',
        'maximumIntroDuration', 'minimumCreditsDuration', 'maximumCreditsDuration',
        'minimumRecapDuration', 'maximumRecapDuration', 'chapterAnalyzerIntroductionPattern',
        'chapterAnalyzerEndCreditsPattern', 'chapterAnalyzerRecapPattern',
        'chapterAnalyzerPreviewPattern', 'chapterAnalyzerCommercialPattern',
        'blackFrameMinimumPercentage', 'blackFrameThreshold'
      ];
      valKeys.forEach(k => {
        const el = document.getElementById('cfg_' + k);
        if (el) {
          el.value = cfg[k] ?? '';
          el.oninput = markDirty;
        }
      });

      // Select
      const modeEl = document.getElementById('cfg_firstEpisodeIntroMode');
      if (modeEl) {
        modeEl.value = cfg.firstEpisodeIntroMode || 'Analyze';
        modeEl.onchange = markDirty;
      }

      // Kodi Settings
      const kodiIntroEl = document.getElementById('cfg_kodiIntroAction');
      if (kodiIntroEl) {
        kodiIntroEl.value = cfg.kodiIntroAction || 'prompt';
        kodiIntroEl.onchange = markDirty;
      }
      const kodiCreditsEl = document.getElementById('cfg_kodiCreditsAction');
      if (kodiCreditsEl) {
        kodiCreditsEl.value = cfg.kodiCreditsAction || 'prompt';
        kodiCreditsEl.onchange = markDirty;
      }
      const kodiDurationEl = document.getElementById('cfg_kodiCreditsPromptDuration');
      if (kodiDurationEl) {
        kodiDurationEl.value = cfg.kodiCreditsPromptDuration ?? 10;
        kodiDurationEl.oninput = markDirty;
      }
      const kodiClientPrefixEl = document.getElementById('cfg_kodiPathPrefixClient');
      if (kodiClientPrefixEl) {
        kodiClientPrefixEl.value = cfg.kodiPathPrefixClient || '';
        kodiClientPrefixEl.oninput = markDirty;
      }
      const kodiServerPrefixEl = document.getElementById('cfg_kodiPathPrefixServer');
      if (kodiServerPrefixEl) {
        kodiServerPrefixEl.value = cfg.kodiPathPrefixServer || '';
        kodiServerPrefixEl.oninput = markDirty;
      }

      // Range
      if (cfg.analysisPercent) {
        rangeSlider.value = cfg.analysisPercent;
        rangeVal.textContent = cfg.analysisPercent + '%';
      }

      // Lists
      seriesExclusions = Array.isArray(cfg.seriesExclusions) ? [...cfg.seriesExclusions] : [];
      renderTags('seriesTags', seriesExclusions, markDirty);

      pathExclusions = Array.isArray(cfg.pathExclusions) ? [...cfg.pathExclusions] : [];
      renderTags('pathTags', pathExclusions, markDirty);
    }

    // Collect config object from form
    function collectForm() {
      const cfg = { ...activeConfig };
      const boolKeys = [
        'autoDetectIntros', 'analyzeSeasonZero', 'reanalyzeSettledSeasons',
        'useSettledSeasonChromaprintConsensus', 'scanIntroduction', 'scanCredits',
        'scanRecap', 'scanPreview', 'scanCommercial', 'preferChromaprint',
        'probeAudioDuration', 'preferAudioStreamWithMostChannels',
        'refineCreditsBoundary', 'detectNonBlackCredits', 'animePreviewFromCreditsEnd',
        'enableSubtitleRecapDetection', 'enableSubtitlePreviewDetection',
        'fullLengthChapters', 'enableSponsorBlockChapterDetection',
        'useEmbeddedChaptersForAnalysis', 'useLegacyBlackFrameAnalyzer',
        'adjustIntroBasedOnSilence', 'snapToKeyframe'
      ];
      boolKeys.forEach(k => {
        const el = document.getElementById('cfg_' + k);
        if (el) cfg[k] = el.checked;
      });

      const numKeys = [
        'settledSeasonDelayHours', 'settledSeasonChromaprintConsensusEpisodes',
        'analysisLengthLimit', 'minimumIntroDuration', 'maximumIntroDuration',
        'minimumCreditsDuration', 'maximumCreditsDuration', 'minimumRecapDuration',
        'maximumRecapDuration', 'blackFrameMinimumPercentage', 'blackFrameThreshold'
      ];
      numKeys.forEach(k => {
        const el = document.getElementById('cfg_' + k);
        if (el) cfg[k] = parseInt(el.value, 10) || 0;
      });

      const strKeys = [
        'preferredAudioLanguage', 'chapterAnalyzerIntroductionPattern',
        'chapterAnalyzerEndCreditsPattern', 'chapterAnalyzerRecapPattern',
        'chapterAnalyzerPreviewPattern', 'chapterAnalyzerCommercialPattern'
      ];
      strKeys.forEach(k => {
        const el = document.getElementById('cfg_' + k);
        if (el) cfg[k] = el.value;
      });

      cfg.analysisPercent = parseInt(rangeSlider.value, 10) || 25;
      cfg.firstEpisodeIntroMode = document.getElementById('cfg_firstEpisodeIntroMode').value;
      cfg.seriesExclusions = [...seriesExclusions];
      cfg.pathExclusions = [...pathExclusions];

      const kIntro = document.getElementById('cfg_kodiIntroAction');
      if (kIntro) cfg.kodiIntroAction = kIntro.value;
      const kCredits = document.getElementById('cfg_kodiCreditsAction');
      if (kCredits) cfg.kodiCreditsAction = kCredits.value;
      const kDuration = document.getElementById('cfg_kodiCreditsPromptDuration');
      if (kDuration) cfg.kodiCreditsPromptDuration = parseInt(kDuration.value, 10) || 10;
      const kClientPrefix = document.getElementById('cfg_kodiPathPrefixClient');
      if (kClientPrefix) cfg.kodiPathPrefixClient = kClientPrefix.value;
      const kServerPrefix = document.getElementById('cfg_kodiPathPrefixServer');
      if (kServerPrefix) cfg.kodiPathPrefixServer = kServerPrefix.value;

      return cfg;
    }

    // Fetch config
    async function loadConfig() {
      try {
        const res = await fetch('/api/config');
        if (res.ok) {
          const data = await res.json();
          populateForm(data);
        } else {
          showToast('Failed to load configuration from server', false);
        }
      } catch (err) {
        showToast('Network error loading configuration: ' + err, false);
      }
    }

    // Save config
    async function saveConfig() {
      const payload = collectForm();
      try {
        const res = await fetch('/api/config', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify(payload)
        });
        if (res.ok) {
          const updated = await res.json();
          populateForm(updated);
          showToast('Configuration saved successfully!');
        } else {
          showToast('Failed to save configuration', false);
        }
      } catch (err) {
        showToast('Error saving configuration: ' + err, false);
      }
    }

    // Reset defaults
    async function resetConfig() {
      if (!confirm('Are you sure you want to reset all settings to defaults?')) return;
      try {
        const res = await fetch('/api/config/reset', { method: 'POST' });
        if (res.ok) {
          const defaults = await res.json();
          populateForm(defaults);
          showToast('Configuration reset to default values');
        }
      } catch (err) {
        showToast('Error resetting configuration: ' + err, false);
      }
    }

    document.getElementById('saveChangesBtn').addEventListener('click', saveConfig);
    document.getElementById('discardChangesBtn').addEventListener('click', () => {
      populateForm(activeConfig);
      showToast('Changes discarded');
    });
    document.getElementById('resetDefaultsBtn').addEventListener('click', resetConfig);

    // Fetch Stats & Server Status
    async function updateStatus() {
      try {
        const statusRes = await fetch('/api/status');
        if (statusRes.ok) {
          const s = await statusRes.json();
          const badge = document.getElementById('statusBadge');
          const txt = document.getElementById('statusText');
          if (s.isScanning) {
            badge.className = 'status-badge scanning';
            txt.textContent = 'Scanning Library';
          } else {
            badge.className = 'status-badge';
            txt.textContent = s.status || 'Online';
          }

          document.getElementById('dbPathLabel').textContent = s.segmentDatabasePath || 'None';
          document.getElementById('ffmpegLabel').textContent = s.fFmpegValid ? s.fFmpegVersion : 'FFmpeg invalid or missing';
          document.getElementById('kodiVfsLabel').textContent = s.kodiVfsConnected ? `Connected (${s.kodiVfsUrl})` : `Offline (${s.kodiVfsUrl})`;
        }

        const statsRes = await fetch('/api/stats');
        if (statsRes.ok) {
          const st = await statsRes.json();
          document.getElementById('kpiSeries').textContent = st.totalSeries;
          document.getElementById('kpiEpisodes').textContent = st.totalEpisodes;
          document.getElementById('kpiSegments').textContent = st.totalSegments;
          document.getElementById('kpiSegmentBreakdown').textContent = `${st.introCount} Intros • ${st.creditsCount} Credits`;
          document.getElementById('kpiTimeSaved').textContent = st.formattedSkippedTime;
        }

        const scanRes = await fetch('/api/scan/status');
        if (scanRes.ok) {
          const scan = await scanRes.json();
          const card = document.getElementById('scanStatusCard');
          if (scan.isScanning) {
            card.style.display = 'block';
            document.getElementById('scanStageLabel').textContent = scan.currentStage || 'Processing...';
            document.getElementById('scanFileLabel').textContent = scan.currentFile || '';
            const percent = scan.totalFiles > 0 ? Math.round((scan.fileIndex / scan.totalFiles) * 100) : 0;
            document.getElementById('scanProgressFill').style.width = percent + '%';
            document.getElementById('scanCountsLabel').textContent = `File ${scan.fileIndex} of ${scan.totalFiles} (${percent}%)`;
          } else {
            card.style.display = 'none';
          }
        }
      } catch (err) {
        document.getElementById('statusBadge').className = 'status-badge offline';
        document.getElementById('statusText').textContent = 'Disconnected';
      }
    }

    // Quick scan buttons
    document.getElementById('quickScanBtn').addEventListener('click', async () => {
      const path = document.getElementById('quickScanPath').value.trim();
      if (!path) { alert('Please enter a directory or network share path.'); return; }
      const exportEdl = document.getElementById('quickScanEdl').checked;
      try {
        const res = await fetch('/api/scan', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ path, exportEdl })
        });
        if (res.ok) {
          showToast('Scan enqueued successfully!');
          document.getElementById('quickScanPath').value = '';
          updateStatus();
        } else {
          showToast('Failed to start scan', false);
        }
      } catch (err) {
        showToast('Error: ' + err, false);
      }
    });

    document.getElementById('stopScanBtn').addEventListener('click', async () => {
      if (!confirm('Are you sure you want to stop the active scan?')) return;
      try {
        const res = await fetch('/api/scan/stop', { method: 'POST' });
        if (res.ok) {
          showToast('Scan stopped');
          updateStatus();
        }
      } catch (err) {
        showToast('Error stopping scan: ' + err, false);
      }
    });

    // Sources Management
    let currentSources = [];

    function escapeHtml(str) {
      if (!str) return '';
      return String(str).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/>/g, "&gt;").replace(/"/g, "&quot;").replace(/'/g, "&#039;");
    }

    function escapeAttr(str) {
      if (!str) return '';
      return String(str).replace(/"/g, "&quot;");
    }

    async function loadSources() {
      try {
        const res = await fetch('/api/sources');
        if (res.ok) {
          currentSources = await res.json();
          renderSources(currentSources);
        }
      } catch (err) {
        console.error('Failed to load sources:', err);
      }
    }

    function showConfirmEnableModal(src, onConfirm) {
      const modal = document.getElementById('confirmSourceModal');
      const nameEl = document.getElementById('confirmModalSourceName');
      const typeEl = document.getElementById('confirmModalContentType');
      if (!modal) {
        if (confirm(`Warning: "${src?.name || src?.path}" is configured as "${src?.contentType}" (not TV Shows). Intro and credit detection only works for TV shows.\n\nAre you sure you want to enable this source?`)) {
          onConfirm();
        }
        return;
      }

      nameEl.textContent = src ? (src.name || src.path) : 'Media Source';
      const cType = (src?.contentType || 'unset').toLowerCase();
      typeEl.textContent = cType === 'movies' ? 'Movies' : (src?.contentType || 'Not Set');
      modal.style.display = 'flex';

      const cancelBtn = document.getElementById('confirmModalCancelBtn');
      const okBtn = document.getElementById('confirmModalOkBtn');

      const cleanup = () => {
        modal.style.display = 'none';
        cancelBtn.removeEventListener('click', onCancel);
        okBtn.removeEventListener('click', onOk);
        modal.removeEventListener('click', onBackdrop);
        document.removeEventListener('keydown', onKeyDown);
      };

      const onCancel = () => {
        cleanup();
      };

      const onOk = () => {
        cleanup();
        onConfirm();
      };

      const onBackdrop = (e) => {
        if (e.target === modal) cleanup();
      };

      const onKeyDown = (e) => {
        if (e.key === 'Escape') cleanup();
      };

      cancelBtn.addEventListener('click', onCancel);
      okBtn.addEventListener('click', onOk);
      modal.addEventListener('click', onBackdrop);
      document.addEventListener('keydown', onKeyDown);
    }

    function renderSources(sources) {
      const container = document.getElementById('sourcesList');
      if (!container) return;
      if (!sources || sources.length === 0) {
        container.innerHTML = '<div style="color: var(--text-dim); font-size: 0.85rem; padding: 8px 0;">No media sources configured yet. Add a directory or network share below.</div>';
        return;
      }

      const hasNonTvEnabled = sources.some(s => (s.contentType || 'tvshows').toLowerCase() !== 'tvshows' && s.enabled);
      let bannerHtml = '';
      if (hasNonTvEnabled) {
        bannerHtml = `
          <div style="padding: 10px 14px; background: rgba(245, 158, 11, 0.12); border: 1px solid rgba(245, 158, 11, 0.35); border-radius: var(--radius-sm); color: #fbbf24; font-size: 0.82rem; margin-bottom: 8px; display: flex; align-items: flex-start; gap: 10px;">
            <span style="font-size: 1.1rem; line-height: 1;">⚠️</span>
            <div>
              <strong>Warning:</strong> One or more enabled sources are not set to <strong>TV Shows</strong> in Kodi. IntroSkipper is designed for television series; scanning movie or other non-episodic sources will not detect intro sequences.
            </div>
          </div>
        `;
      }

      const rowsHtml = sources.map(src => {
        const cType = (src.contentType || 'tvshows').toLowerCase();
        const isTvShow = cType === 'tvshows';
        const typeBadge = isTvShow
          ? '<span style="font-size: 0.72rem; padding: 2px 8px; border-radius: 9999px; background: rgba(99, 102, 241, 0.18); color: #a5b4fc; border: 1px solid rgba(99, 102, 241, 0.3);">📺 TV Shows</span>'
          : `<span style="font-size: 0.72rem; padding: 2px 8px; border-radius: 9999px; background: rgba(245, 158, 11, 0.2); color: #fbbf24; border: 1px solid rgba(245, 158, 11, 0.4); font-weight: 600;">⚠️ ${escapeHtml(cType === 'movies' ? 'Movies' : (src.contentType || 'Not Set'))}</span>`;

        const rowOpacity = isTvShow ? '1' : (src.enabled ? '0.75' : '0.45');
        const rowFilter = isTvShow ? 'none' : 'grayscale(0.4)';
        const rowBorder = (!isTvShow && src.enabled)
          ? 'border: 1px solid rgba(245, 158, 11, 0.45); background: rgba(245, 158, 11, 0.04);'
          : 'border: 1px solid var(--border); background: rgba(255, 255, 255, 0.03);';

        return `
          <div style="display: flex; flex-direction: column; padding: 10px 14px; border-radius: var(--radius-sm); opacity: ${rowOpacity}; filter: ${rowFilter}; ${rowBorder} transition: opacity 0.2s ease, border 0.2s ease;">
            <div style="display: flex; align-items: center; justify-content: space-between;">
              <div style="display: flex; flex-direction: column; gap: 2px; overflow: hidden; margin-right: 12px;">
                <div style="display: flex; align-items: center; gap: 8px; flex-wrap: wrap;">
                  <span style="font-weight: 600; font-size: 0.95rem;">${escapeHtml(src.name || 'Media Source')}</span>
                  ${typeBadge}
                  <span style="font-size: 0.72rem; padding: 1px 7px; border-radius: 9999px; background: ${src.enabled ? 'rgba(16, 185, 129, 0.15); color: #34d399;' : 'rgba(156, 163, 175, 0.15); color: #9ca3af;'}">${src.enabled ? 'Enabled' : 'Disabled'}</span>
                </div>
                <span style="font-size: 0.82rem; color: var(--text-dim); word-break: break-all; font-family: monospace;">${escapeHtml(src.path)}</span>
              </div>
              <div style="display: flex; align-items: center; gap: 8px; flex-shrink: 0;">
                <label class="switch" title="Toggle scanning for this source">
                  <input type="checkbox" class="source-toggle" data-path="${escapeAttr(src.path)}" ${src.enabled ? 'checked' : ''}>
                  <span class="slider"></span>
                </label>
                <button class="btn btn-secondary source-scan-btn" data-path="${escapeAttr(src.path)}" title="Scan this source" style="padding: 5px 9px; font-size: 0.8rem;">▶</button>
                <button class="btn btn-danger source-del-btn" data-path="${escapeAttr(src.path)}" title="Remove source" style="padding: 5px 9px; font-size: 0.8rem;">🗑</button>
              </div>
            </div>
          </div>
        `;
      }).join('');

      container.innerHTML = bannerHtml + rowsHtml;

      container.querySelectorAll('.source-toggle').forEach(el => {
        el.addEventListener('change', async () => {
          const path = el.dataset.path;
          const enabled = el.checked;
          const src = currentSources.find(s => s.path === path);
          const isTv = (src?.contentType || 'tvshows').toLowerCase() === 'tvshows';

          const applyToggle = async () => {
            try {
              const res = await fetch('/api/sources', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                  name: src ? src.name : '',
                  path,
                  enabled,
                  contentType: src ? src.contentType : 'tvshows'
                })
              });
              if (res.ok) {
                if (enabled && !isTv) {
                  showToast(`Source enabled (Warning: Content is ${src?.contentType || 'not TV Shows'})`, false);
                } else {
                  showToast(`Source ${enabled ? 'enabled' : 'disabled'}`);
                }
                loadSources();
              } else {
                showToast('Failed to update source', false);
                el.checked = !enabled;
              }
            } catch (e) {
              showToast('Error: ' + e, false);
              el.checked = !enabled;
            }
          };

          if (enabled && !isTv) {
            el.checked = false;
            showConfirmEnableModal(src, () => {
              el.checked = true;
              applyToggle();
            });
            return;
          }

          applyToggle();
        });
      });

      container.querySelectorAll('.source-scan-btn').forEach(btn => {
        btn.addEventListener('click', async () => {
          const path = btn.dataset.path;
          const exportEdl = document.getElementById('quickScanEdl').checked;
          try {
            const res = await fetch('/api/scan', {
              method: 'POST',
              headers: { 'Content-Type': 'application/json' },
              body: JSON.stringify({ path, exportEdl })
            });
            if (res.ok) {
              showToast('Scan enqueued for: ' + path);
              updateStatus();
            } else {
              showToast('Failed to start scan', false);
            }
          } catch (e) {
            showToast('Error: ' + e, false);
          }
        });
      });

      container.querySelectorAll('.source-del-btn').forEach(btn => {
        btn.addEventListener('click', async () => {
          const path = btn.dataset.path;
          if (!confirm('Remove scan source:\n' + path + '?')) return;
          try {
            const res = await fetch('/api/sources?path=' + encodeURIComponent(path), { method: 'DELETE' });
            if (res.ok) {
              showToast('Source removed');
              loadSources();
            } else {
              showToast('Failed to remove source', false);
            }
          } catch (e) {
            showToast('Error: ' + e, false);
          }
        });
      });
    }

    document.getElementById('addSourceBtn').addEventListener('click', async () => {
      const nameInp = document.getElementById('newSourceName');
      const pathInp = document.getElementById('newSourcePath');
      const typeInp = document.getElementById('newSourceContentType');
      const name = nameInp.value.trim();
      const path = pathInp.value.trim();
      const contentType = typeInp ? typeInp.value : 'tvshows';
      if (!path) {
        alert('Please enter a source folder or network share path.');
        return;
      }
      try {
        const res = await fetch('/api/sources', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ name, path, enabled: true, contentType })
        });
        if (res.ok) {
          showToast('Source added successfully');
          nameInp.value = '';
          pathInp.value = '';
          loadSources();
        } else {
          showToast('Failed to add source', false);
        }
      } catch (e) {
        showToast('Error: ' + e, false);
      }
    });

    document.getElementById('scanAllSourcesBtn').addEventListener('click', async () => {
      const enabled = currentSources.filter(s => s.enabled);
      if (enabled.length === 0) {
        alert('No scan sources are currently enabled.');
        return;
      }
      const exportEdl = document.getElementById('quickScanEdl').checked;
      for (const src of enabled) {
        try {
          await fetch('/api/scan', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ path: src.path, exportEdl })
          });
        } catch (e) {
          console.error(e);
        }
      }
      showToast(`Enqueued ${enabled.length} source(s) for scanning`);
      updateStatus();
    });

    // Init
    loadConfig();
    loadSources();
    updateStatus();
    setInterval(updateStatus, 3000);
  </script>
</body>
</html>
""";

    private static readonly byte[] FontBytes = LoadEmbeddedFont();

    /// <summary>
    /// Returns the full HTML string for the web dashboard.
    /// </summary>
    /// <returns>HTML document string.</returns>
    public static string GetHtml() => HtmlContent;

    /// <summary>
    /// Returns the bundled Inter WOFF2 font binary.
    /// </summary>
    /// <returns>Raw byte array of the font file.</returns>
    public static byte[] GetFontBytes() => FontBytes;

    private static byte[] LoadEmbeddedFont()
    {
        var assembly = typeof(DashboardPage).Assembly;
        using var stream = assembly.GetManifestResourceStream("IntroSkipper.Server.Web.Fonts.inter.woff2");
        if (stream is null)
        {
            return [];
        }

        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }
}
