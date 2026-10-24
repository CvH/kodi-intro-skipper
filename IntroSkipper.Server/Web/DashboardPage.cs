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
      display: flex;
      align-items: center;
      justify-content: center;
      flex-shrink: 0;
      filter: drop-shadow(0 0 10px rgba(19, 166, 224, 0.35));
    }
    .brand-icon svg {
      width: 36px;
      height: 36px;
      display: block;
    }
    .brand-text {
      height: 22px;
      width: auto;
      display: block;
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
        <div class="brand-icon">
          <svg width="100%" height="100%" viewBox="0 0 512 512" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xml:space="preserve" xmlns:affinity="https://www.affinity.studio/" style="fill-rule:evenodd;clip-rule:evenodd;stroke-linejoin:round;stroke-miterlimit:2;">
            <g transform="matrix(0.808504,0,0,0.808504,-59.98895,-55.14606)">
              <clipPath id="_clip1">
                <path d="M192,269C204.667,256.333 211,259 211,277L211,495C211,513 204.667,515.667 192,503L101,412C83.667,394.667 83.667,377.333 101,360L192,269Z"/>
              </clipPath>
              <g clip-path="url(#_clip1)">
                <rect x="60" y="235" width="165" height="300" style="fill:rgb(65,197,244);"/>
              </g>
            </g>
            <g transform="matrix(0.847506,0,0,0.847506,-83.956849,-50.154903)">
              <clipPath id="_clip2">
                <path d="M380,81C393.333,67.667 406.667,67.667 420,81L516,177C529.333,190.333 529.333,203.667 516,217L304,429C291.333,441.667 285,439 285,421L285,198C285,183.333 290.333,170.667 301,160L380,81Z"/>
              </clipPath>
              <g clip-path="url(#_clip2)">
                <rect x="275" y="50" width="125" height="410" style="fill:rgb(65,197,244);"/>
                <rect x="400" y="50" width="150" height="410" style="fill:rgb(19,166,224);"/>
              </g>
            </g>
            <g transform="matrix(0.905622,0,0,0.905622,-106.808846,-150.971416)">
              <clipPath id="_clip3">
                <path d="M380,511C393.333,497.667 406.667,497.667 420,511L500,591C513.333,604.333 513.333,617.667 500,631L420,711C406.667,724.333 393.333,724.333 380,711L300,631C286.667,617.667 286.667,604.333 300,591L380,511Z"/>
              </clipPath>
              <g clip-path="url(#_clip3)">
                <rect x="270" y="480" width="130" height="260" style="fill:rgb(6,166,222);"/>
                <rect x="400" y="480" width="130" height="260" style="fill:rgb(6,141,196);"/>
              </g>
            </g>
            <g transform="matrix(0.905537,0,0,0.905537,-120.357583,-92.484582)">
              <clipPath id="_clip4">
                <path d="M556,286C569.333,272.667 582.667,272.667 596,286L676,366C689.333,379.333 689.333,392.667 676,406L596,486C582.667,499.333 569.333,499.333 556,486L476,406C462.667,392.667 462.667,379.333 476,366L556,286Z"/>
              </clipPath>
              <g clip-path="url(#_clip4)">
                <rect x="445" y="255" width="260" height="260" style="fill:rgb(4,136,190);"/>
              </g>
            </g>
            <g transform="matrix(0.905537,0,0,0.905537,-133.724027,-92.484582)">
              <g transform="matrix(0.888982,0,0,0.888982,66.150417,41.85296)">
                <path d="M520,342L539,323L602,386L539,449L520,430L564,386L520,342Z" style="fill:white;fill-rule:nonzero;"/>
              </g>
              <g transform="matrix(0.888982,0,0,0.888982,66.150417,41.85296)">
                <path d="M576,342L595,323L658,386L595,449L576,430L620,386L576,342Z" style="fill:white;fill-rule:nonzero;"/>
              </g>
            </g>
          </svg>
        </div>
        <svg class="brand-text" height="22" viewBox="0 0 2048 243" version="1.1" xmlns="http://www.w3.org/2000/svg" xmlns:xlink="http://www.w3.org/1999/xlink" xml:space="preserve" xmlns:affinity="https://www.affinity.studio/" style="fill-rule:evenodd;clip-rule:evenodd;stroke-linejoin:round;stroke-miterlimit:2;">
          <g transform="matrix(0.957143,0,0,0.964578,227.598021,4.994139)">
            <rect x="1367.193" y="43.312" width="111.782" height="97.676" style="fill:rgb(4,136,190);"/>
          </g>
          <g transform="matrix(0.957143,0,0,0.964578,63.384606,4.994139)">
            <rect x="1367.193" y="43.312" width="111.782" height="97.676" style="fill:rgb(4,136,190);"/>
          </g>
          <path d="M757,216.27L739,217.43L721,216.15L712,214.23L701,210.81L692.33,207L683.3,202L678,198.4L669,191.02L661.97,184L656.41,177L649.13,165L644,153.08L642.21,147L640.48,139L638.95,126L638.89,118L640.1,105L642,95L643.99,88L651,72.69L656.59,64L662,57.25L669,49.96L675.93,44L683.27,39L694,33.35L702.17,30L708.92,28L719,25.86L729,24.48L739,24.11L753,24.72L764,26.55L776,29.8L785,33.47L791.88,37L798,40.86L807,47.85L816,56.88L823,65.79L828.47,75L832,82.27L837.13,98L839.09,110L839.47,122L839.1,132L837.39,143L835.55,150L832.01,160L828.21,168L823,176.67L816,185.51L808.49,193L802,198.22L794,203.46L787,207.25L778,211L767,214.34L757,216.27ZM1024,214.36L1013,216.27L995,216.9L978,216.17L966,214.31L953,210.8L939,205.29L929.02,200L921.43,194L921.33,192L932,170.76L934,167.76L935,167.42L940,171.41L946,175.11L962,182.27L976,185.91L991,187.68L1009,187.42L1016.11,186L1021.62,184L1025.44,182L1029.36,179L1033,174.93L1035.04,171L1036.01,164L1035,157.67L1033.22,154L1031,151.13L1027,147.82L1020,143.98L1010,140.29L972,130.45L961.3,127L953,123.43L944,118.29L937.63,113L932.72,107L928.77,100L926.6,93L925.39,84L925.29,77L927,65.25L930.48,57L934,51.35L938,46.56L946.62,39L956,33.45L965,29.79L979,26.4L994,24.69L1007,24.66L1022,26.24L1031.1,28L1043,31.58L1055.44,37L1061.64,41L1061.64,43L1052.7,66L1051,68.67L1049,68.44L1037.92,62L1023,56.66L1015,54.76L1004,53.44L997,53.19L983,54.78L973,58.43L967.7,62L964.63,65L962.52,68L960.71,73L960.27,79L960.77,84L962.82,88L968,93.03L971.09,95L979,98.49L990,102L1026,111.72L1037,115.35L1045,118.91L1053.02,124L1059.74,130L1063.52,135L1067,141.32L1068.88,147L1070.35,154L1070.59,167L1068.38,178L1067,182.16L1063.46,189L1056,198.34L1047,205.3L1036,210.71L1024,214.36ZM62,215.02L31,215.02L29,214.66L28.12,213L27.99,29L28.34,28L30,26.96L62,26.81L63,27.29L63.63,29L63.71,213L63,214.69L62,215.02ZM102,214.27L101.58,213L101.58,29L102.69,27L104,26.64L129,26.88L132,28.03L187.34,96L233,153.11L234,152.71L234.71,28L237,26.81L267,26.91L269,27.49L269.55,29L269,214.2L267,214.93L243,214.94L240.37,214L139.74,92L137,89.3L136.34,91L136.41,214L135,215.02L104,215.09L102,214.27ZM350.92,215L349.58,214L350,57.75L349,56.49L293,56.5L290.05,56L289.54,54L289.85,28L291,27.12L293,26.85L444,27.25L445.25,29L445.52,55L444.93,56L443,56.49L387,56.46L385.66,57L385.27,59L384.36,214L382,214.98L350.92,215ZM623.36,215L588,215.06L586,214.58L550.35,160L549,158.79L547,158.32L501,158.31L500,158.75L499.49,161L499.48,214L498,214.98L467,214.98L464.61,214L464.24,29L465,27.35L467,26.74L548,26.86L563.13,28L572,29.77L578,31.66L589.98,37L600.17,44L606,49.76L613,59.47L618.43,72L620.18,80L620.83,89L620.8,97L620,105.84L618.3,113L614.81,122L611.02,129L607.45,134L604,137.85L598.19,143L590,148.41L584,151.05L582.96,152L582.8,153L595.83,173L624.06,214L623.36,215ZM1266.22,215L1225,214.71L1161,135.88L1159,136.76L1135,161.73L1131.5,166L1131.55,213L1131,214.7L1130,215.13L1099,215.1L1096.59,214L1096.33,29L1097,27.49L1099,26.76L1130.19,27L1131.2,28L1131.38,119L1132,119.73L1220,27.95L1223,26.82L1259.15,27L1259.31,29L1204.88,87L1185,108.76L1183.94,111L1266.61,213L1266.98,214L1266.22,215ZM1315,215.02L1284,215.02L1282,214.2L1281.64,213L1281.61,29L1282,27.9L1284,26.84L1315,26.88L1316,27.13L1316.98,29L1317.28,213L1317,214.09L1315,215.02ZM1350.98,215L1349.78,214L1350,27.95L1351,26.86L1431,26.89L1448,27.8L1458,29.72L1464,31.87L1471,35L1478,38.97L1488.74,48L1496,57L1499.53,63L1503.02,71L1505.99,84L1506.6,98L1504.78,109L1501.12,120L1497.52,127L1494,132.39L1484.25,143L1475,150.1L1464,155.7L1453.81,159L1437,160.99L1386,161.31L1385.12,162L1384.72,214L1383,215.06L1350.98,215ZM1512,214.81L1511,214.02L1511.04,153L1512.88,149L1522.53,135L1528,123.03L1531.52,108L1532.05,95L1531.33,79L1529.63,70L1528,64.52L1524.49,56L1521,49.72L1510.69,36L1509.66,29L1510,27.7L1511,26.73L1530,26.56L1593,26.84L1609,27.84L1619.96,30L1625,31.71L1636,36.88L1642,40.95L1651,49.08L1656.02,55L1659.94,61L1665.08,72L1667,78.55L1668.27,86L1668.66,100L1666.87,111L1663,122L1656,133.58L1647.38,143L1638,150.14L1627,155.69L1616.28,159L1604,160.51L1595,160.98L1549,161.1L1547,161.76L1546.69,214L1545,215.02L1512,214.81ZM1828,214.82L1690,215.14L1688,214.7L1687.35,214L1687.32,29L1688,27.52L1690,26.77L1823.15,27L1824.02,29L1823.9,55L1823,56.23L1725,56.29L1723.17,57L1722.89,103L1723.32,104L1725,104.81L1812,105.03L1813.25,107L1813.3,131L1813,132.21L1811,132.83L1724,133.09L1723,133.68L1722.7,135L1722.71,183L1723.4,185L1725,185.61L1828,185.86L1828.97,187L1828.98,213L1828,214.82ZM2015.21,215L1980,215.16L1978,214.71L1942.46,161L1941,159.29L1939,158.54L1891,158.57L1889.97,161L1890.14,214L1889,215.03L1857,215.11L1855.16,214L1855.35,29L1856,27.41L1858,26.75L1943,27.09L1953.57,28L1963,29.83L1969,31.69L1981,36.87L1991.22,44L1998,50.75L2005.17,61L2008.4,68L2010.5,75L2012.05,84L2012.4,93L2012.11,100L2010.5,109L2008.38,116L2005,123.67L1998,134.09L1988.79,143L1981,148.21L1975.27,151L1974.39,152L1974.35,153L1982.78,166L2016.08,214L2015.21,215ZM1387,139.89L1389,139.01L1432,96.08L1432.91,95L1432.98,94L1426.71,87L1390,49.93L1389,49.02L1388,48.91L1386.3,50L1376,60.44L1375.45,62L1375.79,63L1406.6,94L1406.72,95L1379.33,121L1374.75,126L1374.69,127L1376,128.92L1387,139.89ZM1431,139.45L1432,139.13L1476.83,95L1476.85,94L1444.38,60L1433.39,49L1432,48.47L1431,48.75L1420,59.37L1419.07,61L1420,62.73L1446,88.9L1450.44,94L1450,95.38L1427.09,117L1418.42,126L1418.46,127L1431,139.45ZM1551,139.85L1596.13,95L1596.15,94L1565.34,62L1553,49.87L1551,48.85L1550,49.3L1539.76,60L1538.9,62L1539.22,63L1569.59,94L1569,95.56L1537.98,126L1537.88,127L1539.37,129L1550,139.69L1551,139.85ZM1595,139.52L1601,134.31L1640.79,95L1640,92.96L1597,49.1L1595,48.37L1582.53,61L1583.69,63L1614.14,94L1614,95.04L1582.07,126L1582.17,127L1593,138.6L1595,139.52ZM751,185.91L759.54,184L765,182.08L772,178.73L777.87,175L782,171.68L787.61,166L794.64,156L800,144.5L801.67,139L803.45,129L803.62,113L801.73,102L798,91.13L791,79.51L783.22,71L776,65.47L770.41,62L757,56.68L748,54.85L733,54.78L722,56.64L716,58.49L708.2,62L697.57,69L690.25,76L683.12,86L678,97.16L675.74,106L674.47,120L676,135.24L679.57,147L683,154.26L687,160.83L694,169.24L703.71,177L714,182.38L719,184.12L726,185.67L737,186.63L751,185.91ZM579,114.47L582.64,108L584.3,103L585.4,94L585.14,86L584.27,81L581,73.35L576,67.27L572,63.92L568.98,62L560,58.47L550,56.41L501,56.21L499.6,57L499.39,126L500,128.61L537,129.11L552,128.6L559.47,127L568,123.54L573.46,120L579,114.47ZM1971.1,113L1974.49,106L1976.26,99L1976.75,92L1976.4,85L1974.46,78L1971.37,72L1967.11,67L1963.52,64L1956,60L1951,58.3L1943,56.58L1893,56.2L1890.52,57L1890.43,128L1891,128.69L1893,129.17L1933,129.16L1943,128.51L1950.33,127L1960,123.03L1966.88,118L1971.1,113Z" style="fill:white;"/>
        </svg>

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
          <div style="display: flex; gap: 8px;">
            <button id="syncKodiSourcesBtn" class="btn btn-secondary" style="font-size: 0.85rem; padding: 6px 14px;" title="Fetch latest sources from Kodi">🔄 Sync from Kodi</button>
            <button id="scanAllSourcesBtn" class="btn btn-primary" style="font-size: 0.85rem; padding: 6px 14px;">▶ Scan All Enabled</button>
          </div>
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
        <div class="setting-row">
          <div class="setting-info">
            <label for="cfg_maxParallelism" class="setting-label">Maximum Parallel Analysis Tasks</label>
            <span class="setting-desc">Number of concurrent FFmpeg media probing and audio fingerprinting tasks (default: 2). Higher values speed up analysis on multi-core CPUs.</span>
          </div>
          <div class="setting-ctrl">
            <div class="input-unit">
              <input type="number" id="cfg_maxParallelism" min="1" max="16" />
              <span class="unit-tag">tasks</span>
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
        'maxParallelism',
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
        'maxParallelism',
        'analysisLengthLimit', 'minimumIntroDuration', 'maximumIntroDuration',
        'minimumCreditsDuration', 'maximumCreditsDuration', 'minimumRecapDuration',
        'maximumRecapDuration', 'blackFrameMinimumPercentage', 'blackFrameThreshold'
      ];
      numKeys.forEach(k => {
        const el = document.getElementById('cfg_' + k);
        if (el) {
          const parsed = parseInt(el.value, 10);
          cfg[k] = k === 'maxParallelism' ? Math.max(1, parsed || 2) : (parsed || 0);
        }
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
                  ${src.isKodiSource ? '<span style="font-size: 0.72rem; padding: 2px 7px; border-radius: 9999px; background: rgba(59, 130, 246, 0.15); color: #60a5fa; border: 1px solid rgba(59, 130, 246, 0.3);">Kodi</span>' : ''}
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
                ${!src.isKodiSource ? `<button class="btn btn-danger source-del-btn" data-path="${escapeAttr(src.path)}" title="Remove source" style="padding: 5px 9px; font-size: 0.8rem;">🗑</button>` : ''}
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

    const syncKodiBtn = document.getElementById('syncKodiSourcesBtn');
    if (syncKodiBtn) {
      syncKodiBtn.addEventListener('click', async () => {
        syncKodiBtn.disabled = true;
        syncKodiBtn.textContent = '🔄 Syncing...';
        try {
          await loadSources();
          showToast('Sources synced from Kodi');
        } catch (e) {
          showToast('Failed to sync sources: ' + e, false);
        } finally {
          syncKodiBtn.disabled = false;
          syncKodiBtn.textContent = '🔄 Sync from Kodi';
        }
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
