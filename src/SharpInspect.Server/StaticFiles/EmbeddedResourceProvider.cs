using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace SharpInspect.Server.StaticFiles;

/// <summary>
///     Provides static file content from embedded resources.
/// </summary>
public class EmbeddedResourceProvider
{
    private readonly Assembly _assembly;
    private readonly Dictionary<string, byte[]> _cache;
    private readonly object _cacheLock = new();
    private readonly string _resourcePrefix;

    /// <summary>
    ///     Creates a new EmbeddedResourceProvider.
    /// </summary>
    public EmbeddedResourceProvider()
    {
        _cache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        _assembly = typeof(EmbeddedResourceProvider).Assembly;
        _resourcePrefix = "SharpInspect.Server.wwwroot";

        // Pre-load embedded resources
        LoadEmbeddedResources();
    }

    /// <summary>
    ///     Gets the content for a path, or null if not found.
    /// </summary>
    public byte[] GetContent(string path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        // Normalize path
        path = path.TrimStart('/').Replace('/', '.').Replace('\\', '.');

        lock (_cacheLock)
        {
            byte[] content;
            if (_cache.TryGetValue(path, out content)) return content;
        }

        // If not in cache, try to load from embedded resources
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
    ///     Gets the content type for a file path.
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
        // Return a placeholder HTML that will be replaced by the actual UI build
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
    </style>
</head>
<body>
    <div id=""app"">
        <div class=""header"">
            <h1>SharpInspect</h1>
            <div class=""tabs"">
                <button class=""tab active"" data-tab=""network"">Network</button>
                <button class=""tab"" data-tab=""console"">Console</button>
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

            // Tab switching
            document.querySelectorAll('.tab').forEach(tab => {
                tab.addEventListener('click', () => {
                    document.querySelectorAll('.tab').forEach(t => t.classList.remove('active'));
                    tab.classList.add('active');
                    currentTab = tab.dataset.tab;

                    if (currentTab === 'network') {
                        networkPanel.style.display = 'flex';
                        consolePanel.style.display = 'none';
                    } else {
                        networkPanel.style.display = 'none';
                        consolePanel.style.display = 'flex';
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

            connectWebSocket();
        })();
    </script>
</body>
</html>";
    }

    private void LoadEmbeddedResources()
    {
        // Load default index.html if no embedded resources exist
        var indexContent = GetDefaultIndexHtml();
        lock (_cacheLock)
        {
            _cache["index.html"] = Encoding.UTF8.GetBytes(indexContent);
        }
    }
}