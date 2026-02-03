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
            byte[] content;
            if (_cache.TryGetValue(path, out content)) return content;
        }

        // 캐시에 없으면 임베디드 리소스에서 로드 시도
        var resourceName = _resourcePrefix + "." + path;
        using (var stream = _assembly.GetManifestResourceStream(resourceName))
        {
            if (stream != null)
            {
                var content = ReadStream(stream);
                lock (_cacheLock)
                {
                    _cache[path] = content;
                }

                return content;
            }
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
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
            background: #1e1e1e;
            color: #d4d4d4;
            height: 100vh;
            display: flex;
            flex-direction: column;
        }
        .header {
            background: #252526;
            border-bottom: 1px solid #3c3c3c;
            padding: 8px 16px;
            display: flex;
            align-items: center;
            gap: 16px;
        }
        .header h1 {
            font-size: 14px;
            font-weight: 500;
            color: #cccccc;
        }
        .tabs {
            display: flex;
            gap: 4px;
        }
        .tab {
            padding: 6px 12px;
            background: transparent;
            border: none;
            color: #969696;
            cursor: pointer;
            font-size: 12px;
            border-radius: 4px;
        }
        .tab:hover {
            background: #2a2d2e;
            color: #d4d4d4;
        }
        .tab.active {
            background: #37373d;
            color: #ffffff;
        }
        .content {
            flex: 1;
            display: flex;
            flex-direction: column;
            overflow: hidden;
        }
        .toolbar {
            background: #252526;
            border-bottom: 1px solid #3c3c3c;
            padding: 4px 8px;
            display: flex;
            gap: 8px;
            align-items: center;
        }
        .toolbar input {
            background: #3c3c3c;
            border: none;
            padding: 4px 8px;
            color: #d4d4d4;
            border-radius: 4px;
            font-size: 12px;
            width: 200px;
        }
        .toolbar button {
            background: #0e639c;
            border: none;
            padding: 4px 12px;
            color: white;
            border-radius: 4px;
            font-size: 12px;
            cursor: pointer;
        }
        .toolbar button:hover {
            background: #1177bb;
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
            background: #252526;
            text-align: left;
            padding: 4px 8px;
            font-weight: 500;
            color: #969696;
            position: sticky;
            top: 0;
            border-bottom: 1px solid #3c3c3c;
        }
        td {
            padding: 4px 8px;
            border-bottom: 1px solid #2d2d2d;
            white-space: nowrap;
            overflow: hidden;
            text-overflow: ellipsis;
            max-width: 300px;
        }
        tr:hover {
            background: #2a2d2e;
        }
        tr.selected {
            background: #094771;
        }
        .status-2xx { color: #4ec9b0; }
        .status-3xx { color: #569cd6; }
        .status-4xx { color: #ce9178; }
        .status-5xx { color: #f14c4c; }
        .status-error { color: #f14c4c; }
        .detail-panel {
            height: 40%;
            border-top: 1px solid #3c3c3c;
            background: #1e1e1e;
            overflow: auto;
        }
        .detail-tabs {
            display: flex;
            background: #252526;
            border-bottom: 1px solid #3c3c3c;
        }
        .detail-tab {
            padding: 6px 12px;
            background: transparent;
            border: none;
            color: #969696;
            cursor: pointer;
            font-size: 12px;
        }
        .detail-tab.active {
            color: #ffffff;
            border-bottom: 2px solid #0e639c;
        }
        .detail-content {
            padding: 8px;
            font-family: 'Consolas', 'Monaco', monospace;
            font-size: 12px;
            white-space: pre-wrap;
            word-break: break-all;
        }
        .empty-state {
            display: flex;
            align-items: center;
            justify-content: center;
            height: 100%;
            color: #969696;
            font-size: 14px;
        }
        .ws-indicator {
            width: 8px;
            height: 8px;
            border-radius: 50%;
            background: #f14c4c;
        }
        .ws-indicator.connected {
            background: #4ec9b0;
        }
        #app {
            height: 100%;
            display: flex;
            flex-direction: column;
        }
        .perf-card {
            background: #252526;
            border: 1px solid #3c3c3c;
            border-radius: 6px;
            padding: 12px;
        }
        .perf-card-title {
            font-size: 11px;
            color: #969696;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            margin-bottom: 4px;
        }
        .perf-card-value {
            font-size: 24px;
            font-weight: 600;
            color: #d4d4d4;
            margin-bottom: 8px;
        }
        .perf-card canvas {
            width: 100%;
            display: block;
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
            </div>
            <div style=""flex: 1""></div>
            <div class=""ws-indicator"" id=""ws-status"" title=""WebSocket disconnected""></div>
        </div>
        <div class=""content"" id=""network-panel"">
            <div class=""toolbar"">
                <input type=""text"" placeholder=""Filter..."" id=""filter-input"">
                <button id=""clear-btn"">Clear</button>
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
                <span id=""perf-status"" style=""font-size: 12px; color: #969696; margin-left: 8px""></span>
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
                                <div style=""font-size: 20px; color: #4ec9b0"" id=""perf-gen0"">-</div>
                                <div style=""font-size: 11px; color: #969696"">Gen 0</div>
                            </div>
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: #dcdcaa"" id=""perf-gen1"">-</div>
                                <div style=""font-size: 11px; color: #969696"">Gen 1</div>
                            </div>
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: #ce9178"" id=""perf-gen2"">-</div>
                                <div style=""font-size: 11px; color: #969696"">Gen 2</div>
                            </div>
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: #569cd6"" id=""perf-gc-pause"">-</div>
                                <div style=""font-size: 11px; color: #969696"">GC Pause %</div>
                            </div>
                        </div>
                    </div>
                </div>
                <div style=""margin-top: 12px"">
                    <div class=""perf-card"">
                        <div class=""perf-card-title"">Thread Pool</div>
                        <div style=""display: flex; gap: 24px; margin-top: 8px"">
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: #4ec9b0"" id=""perf-tp-worker"">-</div>
                                <div style=""font-size: 11px; color: #969696"">Worker Threads</div>
                            </div>
                            <div style=""text-align: center"">
                                <div style=""font-size: 20px; color: #569cd6"" id=""perf-tp-io"">-</div>
                                <div style=""font-size: 11px; color: #969696"">IO Threads</div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script>
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

            // Tab switching
            const panels = { network: networkPanel, console: consolePanel, performance: performancePanel };
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
                ctx.strokeStyle = '#333';
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

            // Render console list
            function renderConsoleList() {
                const filter = consoleFilterInput.value.toLowerCase();
                const filtered = consoleEntries.filter(e =>
                    !filter || e.message.toLowerCase().includes(filter)
                );

                const levelColors = {
                    'Trace': '#969696',
                    'Debug': '#569cd6',
                    'Information': '#4ec9b0',
                    'Warning': '#dcdcaa',
                    'Error': '#f14c4c',
                    'Critical': '#ff6b6b'
                };

                consoleList.innerHTML = filtered.map(entry => `
                    <div style=""padding: 4px 8px; border-bottom: 1px solid #2d2d2d; font-family: monospace; font-size: 12px;"">
                        <span style=""color: ${levelColors[entry.level] || '#d4d4d4'}"">[${entry.level}]</span>
                        <span style=""color: #969696; margin-left: 8px"">${new Date(entry.timestamp).toLocaleTimeString()}</span>
                        <span style=""color: #569cd6; margin-left: 8px"">${entry.category || ''}</span>
                        <div style=""margin-top: 2px; white-space: pre-wrap; word-break: break-all;"">${escapeHtml(entry.message)}</div>
                        ${entry.exceptionDetails ? `<div style=""color: #f14c4c; margin-top: 4px; white-space: pre-wrap;"">${escapeHtml(entry.exceptionDetails)}</div>` : ''}
                    </div>
                `).join('');
            }

            function escapeHtml(text) {
                if (!text) return '';
                return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
            }

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

            connectWebSocket();
        })();
    </script>
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
