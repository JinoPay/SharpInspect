using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SharpInspect.Server.StaticFiles;

/// <summary>
///     임베디드 리소스에서 정적 파일 콘텐츠를 제공합니다.
/// </summary>
public class EmbeddedResourceProvider
{
    private readonly Assembly _assembly;
    private readonly Dictionary<string, byte[]> _cache;
    private readonly object _cacheLock = new();
    private readonly string _resourcePrefix;

    /// <summary>
    ///     새 EmbeddedResourceProvider를 생성합니다.
    /// </summary>
    public EmbeddedResourceProvider()
    {
        _cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        _assembly = typeof(EmbeddedResourceProvider).Assembly;
        _resourcePrefix = "SharpInspect.Server.wwwroot";

        // 임베디드 리소스 사전 로드
        LoadEmbeddedResources();
    }

    /// <summary>
    ///     경로의 콘텐츠를 가져오거나, 찾을 수 없으면 null 반환.
    /// </summary>
    public byte[] GetContent(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // 경로 정규화
        path = path.TrimStart('/').Replace('/', '.').Replace('\\', '.');

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(path, out var content)) return content;
        }

        // 캐시에 없으면 임베디드 리소스에서 로드 시도
        var resourceName = _resourcePrefix + "." + path;
        using var stream = _assembly.GetManifestResourceStream(resourceName);
        if (stream != null)
        {
            var content = ReadStream(stream);
            lock (_cacheLock)
            {
                _cache[path] = content;
            }

            return content;
        }

        return null;
    }

    /// <summary>
    ///     파일 경로의 콘텐츠 타입을 가져옵니다.
    /// </summary>
    public string GetContentType(string path)
    {
        var ext = Path.GetExtension(path).ToLowerInvariant();

        switch (ext)
        {
            case ".html":
                return "text/html; charset=utf-8";
            case ".css":
                return "text/css; charset=utf-8";
            case ".js":
                return "application/javascript; charset=utf-8";
            case ".json":
                return "application/json; charset=utf-8";
            case ".png":
                return "image/png";
            case ".jpg":
            case ".jpeg":
                return "image/jpeg";
            case ".gif":
                return "image/gif";
            case ".svg":
                return "image/svg+xml";
            case ".ico":
                return "image/x-icon";
            case ".woff":
                return "font/woff";
            case ".woff2":
                return "font/woff2";
            case ".ttf":
                return "font/ttf";
            case ".eot":
                return "application/vnd.ms-fontobject";
            default:
                return "application/octet-stream";
        }
    }

    private byte[] ReadStream(Stream stream)
    {
        using (var ms = new MemoryStream())
        {
            var buffer = new byte[4096];
            int read;
            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0) ms.Write(buffer, 0, read);
            return ms.ToArray();
        }
    }

    private string GetDefaultIndexHtml()
    {
        // 실제 UI 빌드로 대체될 플레이스홀더 HTML 반환
        return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>SharpInspect DevTools</title>
    <style>
        /* 테마 CSS 변수 */
        :root {
            --bg-primary: #1e1e1e;
            --bg-secondary: #252526;
            --bg-hover: #2a2d2e;
            --bg-active: #37373d;
            --bg-input: #3c3c3c;
            --bg-selected: #094771;
            --border-primary: #3c3c3c;
            --border-secondary: #2d2d2d;
            --text-primary: #d4d4d4;
            --text-secondary: #969696;
            --text-header: #cccccc;
            --text-white: #ffffff;
            --accent-primary: #0e639c;
            --accent-hover: #1177bb;
            --status-2xx: #4ec9b0;
            --status-3xx: #569cd6;
            --status-4xx: #ce9178;
            --status-5xx: #f14c4c;
            --chart-grid: #333333;
        }
        [data-theme=""light""] {
            --bg-primary: #ffffff;
            --bg-secondary: #f3f3f3;
            --bg-hover: #e8e8e8;
            --bg-active: #d4d4d4;
            --bg-input: #ffffff;
            --bg-selected: #add6ff;
            --border-primary: #cecece;
            --border-secondary: #e0e0e0;
            --text-primary: #333333;
            --text-secondary: #616161;
            --text-header: #1e1e1e;
            --text-white: #ffffff;
            --accent-primary: #007acc;
            --accent-hover: #0098ff;
            --status-2xx: #008000;
            --status-3xx: #0070c1;
            --status-4xx: #a31515;
            --status-5xx: #d32f2f;
            --chart-grid: #e0e0e0;
        }
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            background: var(--bg-primary);
            color: var(--text-primary);
            height: 100vh;
            display: flex;
            flex-direction: column;
        }
        .header {
            background: var(--bg-secondary);
            border-bottom: 1px solid var(--border-primary);
            padding: 8px 16px;
            display: flex;
            align-items: center;
            gap: 16px;
        }
        .header h1 {
            font-size: 14px;
            font-weight: 500;
            color: var(--text-header);
        }
        .tabs {
            display: flex;
            gap: 4px;
        }
        .tab {
            padding: 6px 12px;
            background: transparent;
            border: none;
            color: var(--text-secondary);
            cursor: pointer;
            font-size: 12px;
            border-radius: 4px;
        }
        .tab:hover {
            background: var(--bg-hover);
            color: var(--text-primary);
        }
        .tab.active {
            background: var(--bg-active);
            color: var(--text-white);
        }
        .content {
            flex: 1;
            display: flex;
            flex-direction: column;
            overflow: hidden;
        }
        .toolbar {
            background: var(--bg-secondary);
            border-bottom: 1px solid var(--border-primary);
            padding: 4px 8px;
            display: flex;
            gap: 8px;
            align-items: center;
        }
        .toolbar input {
            background: var(--bg-input);
            border: 1px solid var(--border-primary);
            padding: 4px 8px;
            color: var(--text-primary);
            border-radius: 4px;
            font-size: 12px;
            width: 200px;
        }
        .toolbar button {
            background: var(--accent-primary);
            border: none;
            padding: 4px 12px;
            color: white;
            border-radius: 4px;
            font-size: 12px;
            cursor: pointer;
        }
        .toolbar button:hover {
            background: var(--accent-hover);
        }
        .list-container {
            flex: 1;
            overflow: auto;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            font-size: 12px;
        }
        th {
            background: var(--bg-secondary);
            text-align: left;
            padding: 4px 8px;
            font-weight: 500;
            color: var(--text-secondary);
            position: sticky;
            top: 0;
            border-bottom: 1px solid var(--border-primary);
        }
        td {
            padding: 4px 8px;
            border-bottom: 1px solid var(--border-secondary);
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            max-width: 300px;
        }
        tr:hover {
            background: var(--bg-hover);
        }
        tr.selected {
            background: var(--bg-selected);
        }
        .status-2xx { color: var(--status-2xx); }
        .status-3xx { color: var(--status-3xx); }
        .status-4xx { color: var(--status-4xx); }
        .status-5xx { color: var(--status-5xx); }
        .status-error { color: var(--status-5xx); }
        /* 콘솔 로그 레벨 색상 */
        .console-trace { color: var(--text-secondary); }
        .console-debug { color: var(--status-3xx); }
        .console-info { color: var(--status-2xx); }
        .console-warning { color: var(--status-4xx); }
        .console-error { color: var(--status-5xx); }
        .console-critical { color: var(--status-5xx); font-weight: bold; }
        .detail-panel {
            height: 40%;
            border-top: 1px solid var(--border-primary);
            background: var(--bg-primary);
            overflow: auto;
        }
        .detail-tabs {
            display: flex;
            background: var(--bg-secondary);
            border-bottom: 1px solid var(--border-primary);
        }
        .detail-tab {
            padding: 6px 12px;
            background: transparent;
            border: none;
            color: var(--text-secondary);
            cursor: pointer;
            font-size: 12px;
        }
        .detail-tab.active {
            color: var(--text-white);
            border-bottom: 2px solid var(--accent-primary);
        }
        .detail-content {
            padding: 8px;
            font-family: 'Consolas', 'Monaco', monospace;
            font-size: 12px;
            white-space: pre-wrap;
            word-break: break-all;
        }
        .copy-dropdown {
            position: relative;
            margin-left: auto;
            padding-right: 8px;
        }
        .copy-btn {
            padding: 4px 10px;
            background: var(--bg-tertiary);
            border: 1px solid var(--border-primary);
            color: var(--text-secondary);
            cursor: pointer;
            font-size: 11px;
            border-radius: 3px;
        }
        .copy-btn:hover {
            background: var(--bg-hover);
            color: var(--text-white);
        }
        .copy-menu {
            display: none;
            position: absolute;
            top: 100%;
            right: 0;
            background: var(--bg-secondary);
            border: 1px solid var(--border-primary);
            border-radius: 4px;
            box-shadow: 0 4px 12px rgba(0,0,0,0.4);
            z-index: 1000;
            min-width: 180px;
            margin-top: 2px;
        }
        .copy-menu.show {
            display: block;
        }
        .copy-menu-item {
            display: block;
            width: 100%;
            padding: 8px 12px;
            background: transparent;
            border: none;
            color: var(--text-secondary);
            cursor: pointer;
            font-size: 12px;
            text-align: left;
        }
        .copy-menu-item:hover {
            background: var(--bg-hover);
            color: var(--text-white);
        }
        .copy-toast {
            position: fixed;
            bottom: 20px;
            left: 50%;
            transform: translateX(-50%);
            background: var(--bg-tertiary);
            color: var(--text-white);
            padding: 10px 20px;
            border-radius: 4px;
            font-size: 13px;
            z-index: 9999;
            opacity: 0;
            transition: opacity 0.3s;
            pointer-events: none;
        }
        .copy-toast.show {
            opacity: 1;
        }
        .empty-state {
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100%;
            color: var(--text-secondary);
            font-size: 14px;
        }
        .ws-indicator {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: var(--status-5xx);
        }
        .ws-indicator.connected {
            background: var(--status-2xx);
        }
        #app {
            height: 100%;
            display: flex;
            flex-direction: column;
        }
        .perf-card {
            background: var(--bg-secondary);
            border: 1px solid var(--border-primary);
            border-radius: 6px;
            padding: 12px;
        }
        .perf-card-title {
            font-size: 11px;
            color: var(--text-secondary);
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 4px;
        }
        .perf-card-value {
            font-size: 24px;
            font-weight: 600;
            color: var(--text-primary);
            margin-bottom: 8px;
        }
        .perf-card canvas {
            width: 100%;
            display: block;
        }
        /* 테마 토글 버튼 */
        .theme-toggle {
            background: transparent;
            border: 1px solid var(--border-primary);
            font-size: 14px;
            cursor: pointer;
            padding: 4px 8px;
            border-radius: 4px;
            margin-right: 8px;
        }
        .theme-toggle:hover {
            background: var(--bg-hover);
        }
    </style>
</head>
<body>
    <div id=""app"">
        <div class=""header"">
            <h1>SharpInspect</h1>
            <div class=""tabs"">
                <button class=""tab active"" data-tab=""network"">Network</button>
                <button class=""tab"" data-tab=""console"">Console</button>
                <button class=""tab"" data-tab=""performance"">Performance</button>
                <button class=""tab"" data-tab=""application"">Application</button>
            </div>
            <div style=""flex: 1""></div>
            <button id=""theme-toggle"" class=""theme-toggle"" title=""Toggle theme"">🌙</button>
            <div class=""ws-indicator"" id=""ws-status"" title=""WebSocket disconnected""></div>
        </div>
        <div class=""content"" id=""network-panel"">
            <div class=""toolbar"">
                <input type=""text"" placeholder=""Filter..."" id=""filter-input"">
                <button id=""clear-btn"">Clear</button>
                <button id=""export-har-btn"">Export HAR</button>
            </div>
            <div class=""list-container"">
                <table>
                    <thead>
                        <tr>
                            <th style=""width: 60px"">Status</th>
                            <th style=""width: 60px"">Method</th>
                            <th>URL</th>
                            <th style=""width: 80px"">Size</th>
                            <th style=""width: 80px"">Time</th>
                        </tr>
                    </thead>
                    <tbody id=""network-list"">
                    </tbody>
                </table>
            </div>
            <div class=""detail-panel"" id=""detail-panel"" style=""display: none"">
                <div class=""detail-tabs"">
                    <button class=""detail-tab active"" data-detail=""headers"">Headers</button>
                    <button class=""detail-tab"" data-detail=""request"">Request</button>
                    <button class=""detail-tab"" data-detail=""response"">Response</button>
                    <button class=""detail-tab"" data-detail=""timing"">Timing</button>
                    <div class=""copy-dropdown"">
                        <button class=""copy-btn"" id=""copy-btn"">Copy &#9660;</button>
                        <div class=""copy-menu"" id=""copy-menu"">
                            <button class=""copy-menu-item"" data-format=""curl-cmd"">Copy as cURL (cmd)</button>
                            <button class=""copy-menu-item"" data-format=""curl-bash"">Copy as cURL (bash)</button>
                            <button class=""copy-menu-item"" data-format=""powershell"">Copy as PowerShell</button>
                            <button class=""copy-menu-item"" data-format=""fetch"">Copy as fetch</button>
                            <button class=""copy-menu-item"" data-format=""csharp"">Copy as C# HttpClient</button>
                        </div>
                    </div>
                </div>
                <div class=""detail-content"" id=""detail-content""></div>
            </div>
        </div>
        <div class=""content"" id=""console-panel"" style=""display: none"">
            <div class=""toolbar"">
                <input type=""text"" placeholder=""Filter..."" id=""console-filter-input"">
                <button id=""console-clear-btn"">Clear</button>
            </div>
            <div class=""list-container"" id=""console-list"">
            </div>
        </div>
        <div class=""content"" id=""performance-panel"" style=""display: none"">
            <div class=""toolbar"">
                <button id=""perf-clear-btn"">Clear</button>
                <span id=""perf-status"" style=""font-size: 12px; color: var(--text-secondary); margin-left: 8px""></span>
            </div>
            <div class=""list-container"" style=""padding: 12px; overflow: auto"">
                <div style=""display: grid; grid-template-columns: 1fr 1fr; gap: 12px"">
                    <div class=""perf-card"">
                        <div class=""perf-card-title"">CPU Usage</div>
                        <div class=""perf-card-value"" id=""perf-cpu"">-</div>
                        <canvas id=""chart-cpu"" height=""120""></canvas>
                    </div>
                    <div class=""perf-card"">
                        <div class=""perf-card-title"">Memory (Working Set)</div>
                        <div class=""perf-card-value"" id=""perf-memory"">-</div>
                        <canvas id=""chart-memory"" height=""120""></canvas>
                    </div>
                    <div class=""perf-card"">
                        <div class=""perf-card-title"">GC Heap (Managed)</div>
                        <div class=""perf-card-value"" id=""perf-gc-heap"">-</div>
                        <canvas id=""chart-gc-heap"" height=""120""></canvas>
                    </div>
                    <div class=""perf-card"">
                        <div class=""perf-card-title"">Thread Count</div>
                        <div class=""perf-card-value"" id=""perf-threads"">-</div>
                        <canvas id=""chart-threads"" height=""120""></canvas>
                    </div>
                </div>
                <div style=""margin-top: 12px"">
                    <div class=""perf-card"">
                        <div class=""perf-card-title"">GC Collections</div>
                        <div style=""display: flex; gap: 24px; margin-top: 8px"">
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: var(--status-2xx)"" id=""perf-gen0"">-</div>
                                <div style=""font-size: 11px; color: var(--text-secondary)"">Gen 0</div>
                            </div>
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: var(--status-4xx)"" id=""perf-gen1"">-</div>
                                <div style=""font-size: 11px; color: var(--text-secondary)"">Gen 1</div>
                            </div>
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: var(--status-4xx)"" id=""perf-gen2"">-</div>
                                <div style=""font-size: 11px; color: var(--text-secondary)"">Gen 2</div>
                            </div>
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: var(--status-3xx)"" id=""perf-gc-pause"">-</div>
                                <div style=""font-size: 11px; color: var(--text-secondary)"">GC Pause %</div>
                            </div>
                        </div>
                    </div>
                </div>
                <div style=""margin-top: 12px"">
                    <div class=""perf-card"">
                        <div class=""perf-card-title"">Thread Pool</div>
                        <div style=""display: flex; gap: 24px; margin-top: 8px"">
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: var(--status-2xx)"" id=""perf-tp-worker"">-</div>
                                <div style=""font-size: 11px; color: var(--text-secondary)"">Worker Threads</div>
                            </div>
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: var(--status-3xx)"" id=""perf-tp-io"">-</div>
                                <div style=""font-size: 11px; color: var(--text-secondary)"">IO Threads</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
        <div class=""content"" id=""application-panel"" style=""display: none"">
            <div class=""toolbar"">
                <button id=""app-refresh-btn"">Refresh</button>
                <span id=""app-status"" style=""font-size: 12px; color: var(--text-secondary); margin-left: 8px""></span>
            </div>
            <div class=""list-container"" style=""padding: 12px; overflow: auto"">
                <div class=""perf-card"" style=""margin-bottom: 12px"">
                    <div class=""perf-card-title"">App Info</div>
                    <div id=""app-info-content"" class=""detail-content"" style=""padding: 8px 0""></div>
                </div>
                <div class=""perf-card"" style=""margin-bottom: 12px"">
                    <div class=""perf-card-title"" style=""display: flex; align-items: center; gap: 8px"">
                        Environment Variables (<span id=""env-count"">0</span>)
                        <input type=""text"" placeholder=""Filter..."" id=""env-filter""
                            style=""background: var(--bg-input); border: 1px solid var(--border-primary); padding: 2px 6px;
                            color: var(--text-primary); border-radius: 4px; font-size: 11px; width: 150px"">
                    </div>
                    <div id=""env-vars-content"" style=""max-height: 300px; overflow: auto; margin-top: 8px""></div>
                </div>
                <div class=""perf-card"">
                    <div class=""perf-card-title"" style=""display: flex; align-items: center; gap: 8px"">
                        Loaded Assemblies (<span id=""assembly-count"">0</span>)
                        <input type=""text"" placeholder=""Filter..."" id=""asm-filter""
                            style=""background: var(--bg-input); border: 1px solid var(--border-primary); padding: 2px 6px;
                            color: var(--text-primary); border-radius: 4px; font-size: 11px; width: 150px"">
                    </div>
                    <div id=""assemblies-content"" style=""max-height: 400px; overflow: auto; margin-top: 8px"">
                        <table>
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Version</th>
                                    <th>Location</th>
                                    <th style=""width: 60px"">Dynamic</th>
                                </tr>
                            </thead>
                            <tbody id=""assembly-list""></tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
        // 테마 매니저 (IIFE 외부에서 즉시 실행)
        const ThemeManager = {
            STORAGE_KEY: 'sharpinspect-theme',

            getSystemTheme() {
                return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
            },

            getSavedTheme() {
                return localStorage.getItem(this.STORAGE_KEY) || 'system';
            },

            getEffectiveTheme() {
                const saved = this.getSavedTheme();
                return saved === 'system' ? this.getSystemTheme() : saved;
            },

            apply(theme) {
                const effective = theme === 'system' ? this.getSystemTheme() : theme;
                document.documentElement.setAttribute('data-theme', effective);
                this.updateButton();
            },

            toggle() {
                const current = this.getSavedTheme();
                const next = current === 'dark' ? 'light' : current === 'light' ? 'system' : 'dark';
                localStorage.setItem(this.STORAGE_KEY, next);
                this.apply(next);
            },

            updateButton() {
                const btn = document.getElementById('theme-toggle');
                if (!btn) return;
                const icons = { dark: '🌙', light: '☀️', system: '💻' };
                const saved = this.getSavedTheme();
                const effective = this.getEffectiveTheme();
                btn.textContent = icons[saved];
                btn.title = 'Theme: ' + saved + (saved === 'system' ? ' (' + effective + ')' : '');
            },

            init() {
                this.apply(this.getSavedTheme());
                window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
                    if (this.getSavedTheme() === 'system') this.apply('system');
                });
                const btn = document.getElementById('theme-toggle');
                if (btn) btn.addEventListener('click', () => this.toggle());
            }
        };
        // 즉시 테마 적용 (깜빡임 방지)
        ThemeManager.apply(ThemeManager.getSavedTheme());

        // SharpInspect DevTools UI
        (function() {
            const API_BASE = window.location.origin;
            let ws = null;
            let networkEntries = [];
            let consoleEntries = [];
            let selectedEntry = null;
            let currentDetailTab = 'headers';
            let currentTab = 'network';

            // Performance chart data (keep last 60 data points)
            const PERF_MAX_POINTS = 60;
            let perfCpuData = [];
            let perfMemoryData = [];
            let perfGcHeapData = [];
            let perfThreadData = [];

            // DOM elements
            const wsStatus = document.getElementById('ws-status');
            const networkList = document.getElementById('network-list');
            const consoleList = document.getElementById('console-list');
            const filterInput = document.getElementById('filter-input');
            const consoleFilterInput = document.getElementById('console-filter-input');
            const detailPanel = document.getElementById('detail-panel');
            const detailContent = document.getElementById('detail-content');
            const networkPanel = document.getElementById('network-panel');
            const consolePanel = document.getElementById('console-panel');
            const performancePanel = document.getElementById('performance-panel');
            const applicationPanel = document.getElementById('application-panel');

            // Tab switching
            const panels = { network: networkPanel, console: consolePanel, performance: performancePanel, application: applicationPanel };
            document.querySelectorAll('.tab').forEach(tab => {
                tab.addEventListener('click', () => {
                    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                    tab.classList.add('active');
                    currentTab = tab.dataset.tab;
                    Object.keys(panels).forEach(k => {
                        panels[k].style.display = k === currentTab ? 'flex' : 'none';
                    });
                    if (currentTab === 'performance') {
                        setTimeout(renderPerfCharts, 50);
                    }
                });
            });

            // Detail tab switching
            document.querySelectorAll('.detail-tab').forEach(tab => {
                tab.addEventListener('click', () => {
                    document.querySelectorAll('.detail-tab').forEach(t => t.classList.remove('active'));
                    tab.classList.add('active');
                    currentDetailTab = tab.dataset.detail;
                    renderDetail();
                });
            });

            // Clear buttons
            document.getElementById('clear-btn').addEventListener('click', () => {
                fetch(API_BASE + '/api/network/clear', { method: 'POST' })
                    .then(() => {
                        networkEntries = [];
                        selectedEntry = null;
                        renderNetworkList();
                        detailPanel.style.display = 'none';
                    });
            });

            // Export HAR button
            document.getElementById('export-har-btn').addEventListener('click', () => {
                window.location.href = API_BASE + '/api/network/export/har';
            });

            document.getElementById('console-clear-btn').addEventListener('click', () => {
                fetch(API_BASE + '/api/console/clear', { method: 'POST' })
                    .then(() => {
                        consoleEntries = [];
                        renderConsoleList();
                    });
            });

            document.getElementById('perf-clear-btn').addEventListener('click', () => {
                fetch(API_BASE + '/api/performance/clear', { method: 'POST', headers: {'Content-Length': '0'} })
                    .then(() => {
                        perfCpuData = [];
                        perfMemoryData = [];
                        perfGcHeapData = [];
                        perfThreadData = [];
                        renderPerfCharts();
                        document.getElementById('perf-status').textContent = 'Cleared';
                    });
            });

            // Mini chart drawing
            // CSS 변수에서 차트 그리드 색상 가져오기
            function getChartGridColor() {
                return getComputedStyle(document.documentElement).getPropertyValue('--chart-grid').trim() || '#333';
            }

            function drawChart(canvasId, data, color, formatLabel) {
                const canvas = document.getElementById(canvasId);
                if (!canvas) return;
                const ctx = canvas.getContext('2d');
                const w = canvas.width = canvas.offsetWidth || canvas.parentElement.offsetWidth || 300;
                const h = canvas.height = 120;
                if (w <= 0) return;
                ctx.clearRect(0, 0, w, h);

                if (data.length < 2) return;

                const max = Math.max(...data) * 1.1 || 1;
                const min = 0;
                const range = max - min || 1;

                // Grid lines
                ctx.strokeStyle = getChartGridColor();
                ctx.lineWidth = 0.5;
                for (let i = 0; i <= 4; i++) {
                    const y = (h / 4) * i;
                    ctx.beginPath();
                    ctx.moveTo(0, y);
                    ctx.lineTo(w, y);
                    ctx.stroke();
                }

                // Data line
                ctx.strokeStyle = color;
                ctx.lineWidth = 1.5;
                ctx.beginPath();
                const step = w / (PERF_MAX_POINTS - 1);
                const offset = PERF_MAX_POINTS - data.length;
                for (let i = 0; i < data.length; i++) {
                    const x = (offset + i) * step;
                    const y = h - ((data[i] - min) / range) * (h - 4) - 2;
                    if (i === 0) ctx.moveTo(x, y);
                    else ctx.lineTo(x, y);
                }
                ctx.stroke();

                // Fill under line
                ctx.lineTo((offset + data.length - 1) * step, h);
                ctx.lineTo(offset * step, h);
                ctx.closePath();
                ctx.fillStyle = color.replace(')', ', 0.1)').replace('rgb', 'rgba');
                ctx.fill();
            }

            function renderPerfCharts() {
                drawChart('chart-cpu', perfCpuData, 'rgb(78, 201, 176)', v => v.toFixed(1) + '%');
                drawChart('chart-memory', perfMemoryData, 'rgb(86, 156, 214)', formatBytes);
                drawChart('chart-gc-heap', perfGcHeapData, 'rgb(220, 220, 170)', formatBytes);
                drawChart('chart-threads', perfThreadData, 'rgb(206, 145, 120)', v => v.toString());
            }

            function updatePerformanceUI(entry) {
                // Push data into chart arrays
                perfCpuData.push(entry.cpuUsagePercent);
                perfMemoryData.push(entry.workingSetBytes);
                perfGcHeapData.push(entry.totalMemoryBytes);
                perfThreadData.push(entry.threadCount);

                if (perfCpuData.length > PERF_MAX_POINTS) perfCpuData.shift();
                if (perfMemoryData.length > PERF_MAX_POINTS) perfMemoryData.shift();
                if (perfGcHeapData.length > PERF_MAX_POINTS) perfGcHeapData.shift();
                if (perfThreadData.length > PERF_MAX_POINTS) perfThreadData.shift();

                // Update value labels
                document.getElementById('perf-cpu').textContent = entry.cpuUsagePercent.toFixed(1) + '%';
                document.getElementById('perf-memory').textContent = formatBytes(entry.workingSetBytes);
                document.getElementById('perf-gc-heap').textContent = formatBytes(entry.totalMemoryBytes);
                document.getElementById('perf-threads').textContent = entry.threadCount;

                // GC collections
                document.getElementById('perf-gen0').textContent = entry.gen0Collections;
                document.getElementById('perf-gen1').textContent = entry.gen1Collections;
                document.getElementById('perf-gen2').textContent = entry.gen2Collections;
                document.getElementById('perf-gc-pause').textContent =
                    entry.gcPauseTimePercent >= 0 ? entry.gcPauseTimePercent.toFixed(2) + '%' : 'N/A';

                // Thread pool
                document.getElementById('perf-tp-worker').textContent =
                    entry.threadPoolWorkerThreads >= 0 ? entry.threadPoolWorkerThreads : 'N/A';
                document.getElementById('perf-tp-io').textContent =
                    entry.threadPoolCompletionPortThreads >= 0 ? entry.threadPoolCompletionPortThreads : 'N/A';

                // Status
                document.getElementById('perf-status').textContent =
                    'Last update: ' + new Date(entry.timestamp).toLocaleTimeString();

                // Redraw charts only if performance tab is visible
                if (currentTab === 'performance') {
                    renderPerfCharts();
                }
            }

            // Filter
            filterInput.addEventListener('input', () => renderNetworkList());
            consoleFilterInput.addEventListener('input', () => renderConsoleList());

            // Format bytes
            function formatBytes(bytes) {
                if (bytes === 0 || bytes === -1) return '-';
                if (bytes < 1024) return bytes + ' B';
                if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
                return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
            }

            // Format time
            function formatTime(ms) {
                if (!ms) return '-';
                if (ms < 1000) return Math.round(ms) + ' ms';
                return (ms / 1000).toFixed(2) + ' s';
            }

            // Get status class
            function getStatusClass(status, isError) {
                if (isError) return 'status-error';
                if (status >= 200 && status < 300) return 'status-2xx';
                if (status >= 300 && status < 400) return 'status-3xx';
                if (status >= 400 && status < 500) return 'status-4xx';
                return 'status-5xx';
            }

            // Render network list
            function renderNetworkList() {
                const filter = filterInput.value.toLowerCase();
                const filtered = networkEntries.filter(e =>
                    !filter || e.url.toLowerCase().includes(filter) || e.method.toLowerCase().includes(filter)
                );

                networkList.innerHTML = filtered.map(entry => `
                    <tr data-id=""${entry.id}"" class=""${selectedEntry && selectedEntry.id === entry.id ? 'selected' : ''}"">
                        <td class=""${getStatusClass(entry.statusCode, entry.isError)}"">${entry.isError ? 'Error' : entry.statusCode}</td>
                        <td>${entry.method}</td>
                        <td title=""${entry.url}"">${entry.url}</td>
                        <td>${formatBytes(entry.responseContentLength)}</td>
                        <td>${formatTime(entry.totalMs)}</td>
                    </tr>
                `).join('');

                // Add click handlers
                networkList.querySelectorAll('tr').forEach(row => {
                    row.addEventListener('click', () => {
                        const id = row.dataset.id;
                        selectedEntry = networkEntries.find(e => e.id === id);
                        renderNetworkList();
                        detailPanel.style.display = 'block';
                        renderDetail();
                    });
                });
            }

            // Render detail panel
            function renderDetail() {
                if (!selectedEntry) return;

                let content = '';
                const e = selectedEntry;

                switch (currentDetailTab) {
                    case 'headers':
                        content = `General:
  Request URL: ${e.url}
  Request Method: ${e.method}
  Status Code: ${e.statusCode} ${e.statusText || ''}
  Protocol: ${e.protocol || 'HTTP/1.1'}

Request Headers:
${Object.entries(e.requestHeaders || {}).map(([k, v]) => `  ${k}: ${v}`).join('\n')}

Response Headers:
${Object.entries(e.responseHeaders || {}).map(([k, v]) => `  ${k}: ${v}`).join('\n')}`;
                        break;
                    case 'request':
                        content = e.requestBody || '(No request body)';
                        break;
                    case 'response':
                        content = e.responseBody || '(No response body)';
                        try {
                            const parsed = JSON.parse(content);
                            content = JSON.stringify(parsed, null, 2);
                        } catch {}
                        break;
                    case 'timing':
                        content = `Total: ${formatTime(e.totalMs)}
DNS Lookup: ${formatTime(e.dnsLookupMs)}
TCP Connect: ${formatTime(e.tcpConnectMs)}
TLS Handshake: ${formatTime(e.tlsHandshakeMs)}
Request Sent: ${formatTime(e.requestSentMs)}
Waiting (TTFB): ${formatTime(e.waitingMs)}
Content Download: ${formatTime(e.contentDownloadMs)}`;
                        break;
                }

                detailContent.textContent = content;
            }

            // 콘솔 로그 레벨별 CSS 클래스
            function getLevelClass(level) {
                const levelClasses = {
                    'Trace': 'console-trace',
                    'Debug': 'console-debug',
                    'Information': 'console-info',
                    'Warning': 'console-warning',
                    'Error': 'console-error',
                    'Critical': 'console-critical'
                };
                return levelClasses[level] || '';
            }

            // Render console list
            function renderConsoleList() {
                const filter = consoleFilterInput.value.toLowerCase();
                const filtered = consoleEntries.filter(e =>
                    !filter || e.message.toLowerCase().includes(filter)
                );

                consoleList.innerHTML = filtered.map(entry => `
                    <div style=""padding: 4px 8px; border-bottom: 1px solid var(--border-secondary); font-family: monospace; font-size: 12px;"">
                        <span class=""${getLevelClass(entry.level)}"">[${entry.level}]</span>
                        <span style=""color: var(--text-secondary); margin-left: 8px"">${new Date(entry.timestamp).toLocaleTimeString()}</span>
                        <span style=""color: var(--status-3xx); margin-left: 8px"">${entry.category || ''}</span>
                        <div style=""margin-top: 2px; white-space: pre-wrap; word-break: break-all;"">${escapeHtml(entry.message)}</div>
                        ${entry.exceptionDetails ? `<div style=""color: var(--status-5xx); margin-top: 4px; white-space: pre-wrap;"">${escapeHtml(entry.exceptionDetails)}</div>` : ''}
                    </div>
                `).join('');
            }

            function escapeHtml(text) {
                if (!text) return '';
                return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            }

            // Application tab
            let applicationInfo = null;

            function renderApplicationInfo() {
                if (!applicationInfo) return;
                const info = applicationInfo;

                // App Info section
                const appInfoHtml = [
                    ['Assembly', (info.assemblyName || 'N/A') + ' v' + (info.assemblyVersion || 'N/A')],
                    ['Location', info.assemblyLocation || 'N/A'],
                    ['Runtime', info.frameworkDescription || info.runtimeVersion || 'N/A'],
                    ['OS', info.osDescription || 'N/A'],
                    ['Architecture', info.processArchitecture || 'N/A'],
                    ['Process ID', info.processId],
                    ['Machine', info.machineName || 'N/A'],
                    ['User', info.userName || 'N/A'],
                    ['Start Time', info.processStartTime ? new Date(info.processStartTime).toLocaleString() : 'N/A'],
                    ['Working Dir', info.workingDirectory || 'N/A'],
                    ['Processors', info.processorCount],
                    ['Server GC', info.isServerGC ? 'Yes' : 'No'],
                    ['Command Line', (info.commandLineArgs || []).join(' ') || 'N/A']
                ].map(([k, v]) => '<div style=""display:flex;padding:2px 0""><span style=""color:#969696;min-width:120px"">' + escapeHtml(k) + ':</span><span>' + escapeHtml(String(v)) + '</span></div>').join('');
                document.getElementById('app-info-content').innerHTML = appInfoHtml;

                renderEnvironmentVariables();
                renderAssemblies();

                document.getElementById('app-status').textContent =
                    'Last update: ' + new Date(info.timestamp).toLocaleTimeString();
            }

            function renderEnvironmentVariables() {
                if (!applicationInfo) return;
                const filter = (document.getElementById('env-filter').value || '').toLowerCase();
                const envVars = applicationInfo.environmentVariables || {};
                const keys = Object.keys(envVars).sort();
                const filtered = keys.filter(k => !filter || k.toLowerCase().includes(filter) || envVars[k].toLowerCase().includes(filter));

                document.getElementById('env-count').textContent = keys.length;

                const html = '<table><thead><tr><th style=""width:30%"">Key</th><th>Value</th></tr></thead><tbody>' +
                    filtered.map(k => '<tr><td style=""color:#4ec9b0"">' + escapeHtml(k) + '</td><td style=""word-break:break-all;white-space:normal"">' + escapeHtml(envVars[k]) + '</td></tr>').join('') +
                    '</tbody></table>';
                document.getElementById('env-vars-content').innerHTML = html;
            }

            function renderAssemblies() {
                if (!applicationInfo) return;
                const filter = (document.getElementById('asm-filter').value || '').toLowerCase();
                const assemblies = applicationInfo.loadedAssemblies || [];
                const filtered = assemblies.filter(a => !filter || (a.name || '').toLowerCase().includes(filter) || (a.location || '').toLowerCase().includes(filter));

                document.getElementById('assembly-count').textContent = assemblies.length;

                document.getElementById('assembly-list').innerHTML = filtered.map(a =>
                    '<tr><td title=""' + escapeHtml(a.fullName || '') + '"">' + escapeHtml(a.name || '') + '</td>' +
                    '<td>' + escapeHtml(a.version || '') + '</td>' +
                    '<td style=""word-break:break-all;white-space:normal;max-width:400px"" title=""' + escapeHtml(a.location || '') + '"">' + escapeHtml(a.location || '') + '</td>' +
                    '<td>' + (a.isDynamic ? 'Yes' : '') + '</td></tr>'
                ).join('');
            }

            // Application tab event listeners
            document.getElementById('app-refresh-btn').addEventListener('click', function() {
                fetch(API_BASE + '/api/application/refresh', { method: 'POST' });
                fetch(API_BASE + '/api/application')
                    .then(function(r) { return r.json(); })
                    .then(function(data) { applicationInfo = data; renderApplicationInfo(); })
                    .catch(console.error);
            });

            document.getElementById('env-filter').addEventListener('input', renderEnvironmentVariables);
            document.getElementById('asm-filter').addEventListener('input', renderAssemblies);

            // Connect WebSocket
            function connectWebSocket() {
                const wsUrl = `ws://${window.location.host}/ws`;
                ws = new WebSocket(wsUrl);

                ws.onopen = () => {
                    wsStatus.classList.add('connected');
                    wsStatus.title = 'WebSocket connected';
                };

                ws.onclose = () => {
                    wsStatus.classList.remove('connected');
                    wsStatus.title = 'WebSocket disconnected';
                    setTimeout(connectWebSocket, 2000);
                };

                ws.onerror = () => {
                    ws.close();
                };

                ws.onmessage = (event) => {
                    try {
                        const msg = JSON.parse(event.data);
                        if (msg.type === 'network:entry') {
                            networkEntries.push(msg.data);
                            if (networkEntries.length > 1000) {
                                networkEntries.shift();
                            }
                            renderNetworkList();
                        } else if (msg.type === 'console:entry') {
                            consoleEntries.push(msg.data);
                            if (consoleEntries.length > 5000) {
                                consoleEntries.shift();
                            }
                            renderConsoleList();
                        } else if (msg.type === 'performance:entry') {
                            updatePerformanceUI(msg.data);
                        } else if (msg.type === 'application:info') {
                            applicationInfo = msg.data;
                            renderApplicationInfo();
                        }
                    } catch {}
                };
            }

            // Load initial data
            fetch(API_BASE + '/api/network?limit=1000')
                .then(r => r.json())
                .then(data => {
                    networkEntries = data.items || [];
                    renderNetworkList();
                })
                .catch(console.error);

            fetch(API_BASE + '/api/console?limit=5000')
                .then(r => r.json())
                .then(data => {
                    consoleEntries = data.items || [];
                    renderConsoleList();
                })
                .catch(console.error);

            fetch(API_BASE + '/api/performance?limit=' + PERF_MAX_POINTS)
                .then(r => r.json())
                .then(data => {
                    const items = data.items || [];
                    items.forEach(entry => updatePerformanceUI(entry));
                })
                .catch(console.error);

            fetch(API_BASE + '/api/application')
                .then(r => r.json())
                .then(data => {
                    applicationInfo = data;
                    renderApplicationInfo();
                })
                .catch(console.error);

            connectWebSocket();

            // === Copy as... 기능 ===
            var SQ = String.fromCharCode(39);  // 작은따옴표
            var BS = String.fromCharCode(92);  // 백슬래시
            var DQ = String.fromCharCode(34);  // 큰따옴표
            var NL = String.fromCharCode(10);  // 줄바꿈

            // 이스케이프 헬퍼 함수들
            function escapeCmdArg(str) {
                if (!str) return str;
                return str.split(BS).join(BS + BS).split(DQ).join(BS + DQ);
            }

            function escapeBashArg(str) {
                if (!str) return SQ + SQ;
                return SQ + str.split(SQ).join(SQ + BS + SQ + SQ) + SQ;
            }

            function escapePowerShellArg(str) {
                if (!str) return str;
                return str.split(DQ).join(BS + DQ).split(String.fromCharCode(96)).join(String.fromCharCode(96) + String.fromCharCode(96));
            }

            function escapeJsString(str) {
                if (!str) return str;
                return str.split(BS).join(BS + BS).split(DQ).join(BS + DQ).split(NL).join(BS + 'n');
            }

            function escapeCSharpString(str) {
                if (!str) return str;
                return str.split(BS).join(BS + BS).split(DQ).join(BS + DQ).split(NL).join(BS + 'n');
            }

            // cURL (Windows cmd) 변환
            function toCurlCmd(entry) {
                var lines = [];
                var method = entry.method || 'GET';
                lines.push('curl -X ' + method + ' ^');
                lines.push('  ' + DQ + entry.url + DQ + ' ^');

                if (entry.requestHeaders) {
                    var keys = Object.keys(entry.requestHeaders);
                    for (var i = 0; i < keys.length; i++) {
                        var key = keys[i];
                        var value = entry.requestHeaders[key];
                        lines.push('  -H ' + DQ + key + ': ' + escapeCmdArg(value) + DQ + ' ^');
                    }
                }

                if (entry.requestBody) {
                    lines.push('  -d ' + DQ + escapeCmdArg(entry.requestBody) + DQ);
                } else {
                    var lastLine = lines[lines.length - 1];
                    if (lastLine && lastLine.slice(-1) === '^') {
                        lines[lines.length - 1] = lastLine.slice(0, -2);
                    }
                }

                return lines.join(NL);
            }

            // cURL (bash) 변환
            function toCurlBash(entry) {
                var lines = [];
                var method = entry.method || 'GET';
                lines.push('curl -X ' + method + ' ' + BS);
                lines.push('  ' + escapeBashArg(entry.url) + ' ' + BS);

                if (entry.requestHeaders) {
                    var keys = Object.keys(entry.requestHeaders);
                    for (var i = 0; i < keys.length; i++) {
                        var key = keys[i];
                        var value = entry.requestHeaders[key];
                        lines.push('  -H ' + escapeBashArg(key + ': ' + value) + ' ' + BS);
                    }
                }

                if (entry.requestBody) {
                    lines.push('  -d ' + escapeBashArg(entry.requestBody));
                } else {
                    var lastLine = lines[lines.length - 1];
                    if (lastLine && lastLine.slice(-1) === BS) {
                        lines[lines.length - 1] = lastLine.slice(0, -2);
                    }
                }

                return lines.join(NL);
            }

            // PowerShell 변환
            function toPowerShell(entry) {
                var lines = [];
                var method = entry.method || 'GET';

                if (entry.requestHeaders && Object.keys(entry.requestHeaders).length > 0) {
                    lines.push('$headers = @{');
                    var keys = Object.keys(entry.requestHeaders);
                    for (var i = 0; i < keys.length; i++) {
                        var key = keys[i];
                        var value = entry.requestHeaders[key];
                        lines.push('    ' + DQ + key + DQ + ' = ' + DQ + escapePowerShellArg(value) + DQ);
                    }
                    lines.push('}');
                    lines.push('');
                }

                if (entry.requestBody) {
                    lines.push('$body = @' + DQ);
                    lines.push(entry.requestBody);
                    lines.push(DQ + '@');
                    lines.push('');
                }

                lines.push('Invoke-RestMethod -Uri ' + DQ + entry.url + DQ + ' -Method ' + method + ' `');

                if (entry.requestHeaders && Object.keys(entry.requestHeaders).length > 0) {
                    lines.push('    -Headers $headers `');
                }

                if (entry.requestBody) {
                    lines.push('    -Body $body');
                } else {
                    var lastLine = lines[lines.length - 1];
                    if (lastLine && lastLine.slice(-1) === '`') {
                        lines[lines.length - 1] = lastLine.slice(0, -2);
                    }
                }

                return lines.join(NL);
            }

            // JavaScript fetch 변환
            function toFetch(entry) {
                var lines = [];
                var method = entry.method || 'GET';

                lines.push('fetch(' + DQ + escapeJsString(entry.url) + DQ + ', {');
                lines.push('  method: ' + DQ + method + DQ + ',');

                if (entry.requestHeaders && Object.keys(entry.requestHeaders).length > 0) {
                    lines.push('  headers: {');
                    var keys = Object.keys(entry.requestHeaders);
                    for (var i = 0; i < keys.length; i++) {
                        var key = keys[i];
                        var value = entry.requestHeaders[key];
                        var comma = (i < keys.length - 1) ? ',' : '';
                        lines.push('    ' + DQ + escapeJsString(key) + DQ + ': ' + DQ + escapeJsString(value) + DQ + comma);
                    }
                    lines.push('  },');
                }

                if (entry.requestBody) {
                    lines.push('  body: ' + DQ + escapeJsString(entry.requestBody) + DQ);
                } else {
                    var lastLine = lines[lines.length - 1];
                    if (lastLine && lastLine.slice(-1) === ',') {
                        lines[lines.length - 1] = lastLine.slice(0, -1);
                    }
                }

                lines.push('})');
                lines.push('.then(response => response.json())');
                lines.push('.then(data => console.log(data))');
                lines.push('.catch(error => console.error(error));');

                return lines.join(NL);
            }

            // C# HttpClient 변환
            function toCSharpHttpClient(entry) {
                var lines = [];
                var method = entry.method || 'GET';

                lines.push('using var client = new HttpClient();');
                lines.push('');
                lines.push('var request = new HttpRequestMessage');
                lines.push('{');
                lines.push('    Method = HttpMethod.' + method.charAt(0).toUpperCase() + method.slice(1).toLowerCase() + ',');
                lines.push('    RequestUri = new Uri(' + DQ + escapeCSharpString(entry.url) + DQ + ')');
                lines.push('};');
                lines.push('');

                var headerKeys = entry.requestHeaders ? Object.keys(entry.requestHeaders).filter(function(k) {
                    var lower = k.toLowerCase();
                    return lower !== 'host' && lower !== 'content-length' && (lower !== 'content-type' || !entry.requestBody);
                }) : [];

                if (headerKeys.length > 0) {
                    for (var i = 0; i < headerKeys.length; i++) {
                        var key = headerKeys[i];
                        var value = entry.requestHeaders[key];
                        lines.push('request.Headers.TryAddWithoutValidation(' + DQ + key + DQ + ', ' + DQ + escapeCSharpString(value) + DQ + ');');
                    }
                    lines.push('');
                }

                if (entry.requestBody) {
                    var contentType = entry.requestContentType || 'application/json';
                    lines.push('request.Content = new StringContent(');
                    lines.push('    ' + DQ + escapeCSharpString(entry.requestBody) + DQ + ',');
                    lines.push('    Encoding.UTF8,');
                    lines.push('    ' + DQ + contentType + DQ + ');');
                    lines.push('');
                }

                lines.push('var response = await client.SendAsync(request);');
                lines.push('var content = await response.Content.ReadAsStringAsync();');
                lines.push('Console.WriteLine(content);');

                return lines.join(NL);
            }

            // 클립보드 복사
            function copyToClipboard(text) {
                if (navigator.clipboard && navigator.clipboard.writeText) {
                    return navigator.clipboard.writeText(text);
                }

                var textarea = document.createElement('textarea');
                textarea.value = text;
                textarea.style.position = 'fixed';
                textarea.style.opacity = '0';
                textarea.style.left = '-9999px';
                document.body.appendChild(textarea);
                textarea.select();

                try {
                    document.execCommand('copy');
                } finally {
                    document.body.removeChild(textarea);
                }

                return Promise.resolve();
            }

            // 토스트 알림
            function showToast(message) {
                var toast = document.getElementById('copy-toast');
                if (toast) {
                    toast.textContent = message;
                    toast.classList.add('show');
                    setTimeout(function() {
                        toast.classList.remove('show');
                    }, 2000);
                }
            }

            // Copy 버튼 이벤트 핸들러
            var copyBtn = document.getElementById('copy-btn');
            var copyMenu = document.getElementById('copy-menu');

            if (copyBtn && copyMenu) {
                copyBtn.addEventListener('click', function(e) {
                    e.stopPropagation();
                    copyMenu.classList.toggle('show');
                });

                document.addEventListener('click', function() {
                    copyMenu.classList.remove('show');
                });

                copyMenu.addEventListener('click', function(e) {
                    var item = e.target.closest('.copy-menu-item');
                    if (!item || !selectedEntry) return;

                    var format = item.getAttribute('data-format');
                    var text = '';

                    switch (format) {
                        case 'curl-cmd':
                            text = toCurlCmd(selectedEntry);
                            break;
                        case 'curl-bash':
                            text = toCurlBash(selectedEntry);
                            break;
                        case 'powershell':
                            text = toPowerShell(selectedEntry);
                            break;
                        case 'fetch':
                            text = toFetch(selectedEntry);
                            break;
                        case 'csharp':
                            text = toCSharpHttpClient(selectedEntry);
                            break;
                    }

                    if (text) {
                        copyToClipboard(text).then(function() {
                            showToast('Copied as ' + format);
                        });
                    }

                    copyMenu.classList.remove('show');
                });
            }

            // 테마 매니저 초기화 (DOM 로드 후)
            ThemeManager.init();
        })();
    </script>
    <div class=""copy-toast"" id=""copy-toast"">Copied!</div>
</body>
</html>";
    }

    private void LoadEmbeddedResources()
    {
        // 임베디드 리소스가 없으면 기본 index.html 로드
        var indexContent = GetDefaultIndexHtml();
        lock (_cacheLock)
        {
            _cache["index.html"] = Encoding.UTF8.GetBytes(indexContent);
        }
    }
}
