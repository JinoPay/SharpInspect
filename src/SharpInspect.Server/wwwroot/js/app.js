/**
 * SharpInspect DevTools - Main Application
 * 메인 애플리케이션 로직
 */
(function() {
    'use strict';

    var API_BASE = window.location.origin;
    var ws = null;
    var networkEntries = [];
    var consoleEntries = [];
    var selectedEntry = null;
    var currentDetailTab = 'headers';
    var currentTab = 'network';
    var applicationInfo = null;

    // Performance chart data
    var PERF_MAX_POINTS = SharpInspectCharts.PERF_MAX_POINTS;
    var perfCpuData = [];
    var perfMemoryData = [];
    var perfGcHeapData = [];
    var perfThreadData = [];

    // DOM elements
    var wsStatus = document.getElementById('ws-status');
    var networkList = document.getElementById('network-list');
    var consoleList = document.getElementById('console-list');
    var filterInput = document.getElementById('filter-input');
    var consoleFilterInput = document.getElementById('console-filter-input');
    var detailPanel = document.getElementById('detail-panel');
    var detailContent = document.getElementById('detail-content');
    var networkPanel = document.getElementById('network-panel');
    var consolePanel = document.getElementById('console-panel');
    var performancePanel = document.getElementById('performance-panel');
    var applicationPanel = document.getElementById('application-panel');

    // Panels map
    var panels = {
        network: networkPanel,
        console: consolePanel,
        performance: performancePanel,
        application: applicationPanel
    };

    // ===== Tab Switching =====
    function initTabs() {
        document.querySelectorAll('.tab').forEach(function(tab) {
            tab.addEventListener('click', function() {
                document.querySelectorAll('.tab').forEach(function(t) {
                    t.classList.remove('active');
                });
                tab.classList.add('active');
                currentTab = tab.dataset.tab;
                Object.keys(panels).forEach(function(k) {
                    panels[k].style.display = k === currentTab ? 'flex' : 'none';
                });
                if (currentTab === 'performance') {
                    setTimeout(renderPerfCharts, 50);
                }
            });
        });
    }

    function initDetailTabs() {
        document.querySelectorAll('.detail-tab').forEach(function(tab) {
            tab.addEventListener('click', function() {
                document.querySelectorAll('.detail-tab').forEach(function(t) {
                    t.classList.remove('active');
                });
                tab.classList.add('active');
                currentDetailTab = tab.dataset.detail;
                renderDetail();
            });
        });
    }

    // ===== Network Panel =====
    function renderNetworkList() {
        var filter = filterInput.value.toLowerCase();
        var filtered = networkEntries.filter(function(e) {
            return !filter || e.url.toLowerCase().indexOf(filter) !== -1 || e.method.toLowerCase().indexOf(filter) !== -1;
        });

        networkList.innerHTML = filtered.map(function(entry) {
            var statusClass = SharpInspectUtils.getStatusClass(entry.statusCode, entry.isError);
            var selected = selectedEntry && selectedEntry.id === entry.id ? 'selected' : '';
            return '<tr data-id="' + entry.id + '" class="' + selected + '">' +
                '<td class="' + statusClass + '">' + (entry.isError ? 'Error' : entry.statusCode) + '</td>' +
                '<td>' + entry.method + '</td>' +
                '<td title="' + SharpInspectUtils.escapeHtml(entry.url) + '">' + SharpInspectUtils.escapeHtml(entry.url) + '</td>' +
                '<td>' + SharpInspectUtils.formatBytes(entry.responseContentLength) + '</td>' +
                '<td>' + SharpInspectUtils.formatTime(entry.totalMs) + '</td>' +
                '</tr>';
        }).join('');

        // Add click handlers
        networkList.querySelectorAll('tr').forEach(function(row) {
            row.addEventListener('click', function() {
                var id = row.dataset.id;
                selectedEntry = networkEntries.find(function(e) { return e.id === id; });
                renderNetworkList();
                detailPanel.style.display = 'block';
                renderDetail();
            });
        });
    }

    function renderDetail() {
        if (!selectedEntry) return;

        var content = '';
        var e = selectedEntry;

        switch (currentDetailTab) {
            case 'headers':
                content = 'General:\n' +
                    '  Request URL: ' + e.url + '\n' +
                    '  Request Method: ' + e.method + '\n' +
                    '  Status Code: ' + e.statusCode + ' ' + (e.statusText || '') + '\n' +
                    '  Protocol: ' + (e.protocol || 'HTTP/1.1') + '\n\n' +
                    'Request Headers:\n' +
                    Object.keys(e.requestHeaders || {}).map(function(k) {
                        return '  ' + k + ': ' + e.requestHeaders[k];
                    }).join('\n') + '\n\n' +
                    'Response Headers:\n' +
                    Object.keys(e.responseHeaders || {}).map(function(k) {
                        return '  ' + k + ': ' + e.responseHeaders[k];
                    }).join('\n');
                break;
            case 'request':
                content = e.requestBody || '(No request body)';
                break;
            case 'response':
                content = e.responseBody || '(No response body)';
                try {
                    var parsed = JSON.parse(content);
                    content = JSON.stringify(parsed, null, 2);
                } catch (ex) {}
                break;
            case 'timing':
                content = 'Total: ' + SharpInspectUtils.formatTime(e.totalMs) + '\n' +
                    'DNS Lookup: ' + SharpInspectUtils.formatTime(e.dnsLookupMs) + '\n' +
                    'TCP Connect: ' + SharpInspectUtils.formatTime(e.tcpConnectMs) + '\n' +
                    'TLS Handshake: ' + SharpInspectUtils.formatTime(e.tlsHandshakeMs) + '\n' +
                    'Request Sent: ' + SharpInspectUtils.formatTime(e.requestSentMs) + '\n' +
                    'Waiting (TTFB): ' + SharpInspectUtils.formatTime(e.waitingMs) + '\n' +
                    'Content Download: ' + SharpInspectUtils.formatTime(e.contentDownloadMs);
                break;
        }

        detailContent.textContent = content;
    }

    // ===== Console Panel =====
    function renderConsoleList() {
        var filter = consoleFilterInput.value.toLowerCase();
        var filtered = consoleEntries.filter(function(e) {
            return !filter || e.message.toLowerCase().indexOf(filter) !== -1;
        });

        consoleList.innerHTML = filtered.map(function(entry) {
            var levelClass = SharpInspectUtils.getLevelClass(entry.level);
            return '<div style="padding: 4px 8px; border-bottom: 1px solid var(--border-secondary); font-family: monospace; font-size: 12px;">' +
                '<span class="' + levelClass + '">[' + entry.level + ']</span>' +
                '<span style="color: var(--text-secondary); margin-left: 8px">' + new Date(entry.timestamp).toLocaleTimeString() + '</span>' +
                '<span style="color: var(--status-3xx); margin-left: 8px">' + (entry.category || '') + '</span>' +
                '<div style="margin-top: 2px; white-space: pre-wrap; word-break: break-all;">' + SharpInspectUtils.escapeHtml(entry.message) + '</div>' +
                (entry.exceptionDetails ? '<div style="color: var(--status-5xx); margin-top: 4px; white-space: pre-wrap;">' + SharpInspectUtils.escapeHtml(entry.exceptionDetails) + '</div>' : '') +
                '</div>';
        }).join('');
    }

    // ===== Performance Panel =====
    function renderPerfCharts() {
        SharpInspectCharts.drawChart('chart-cpu', perfCpuData, 'rgb(78, 201, 176)');
        SharpInspectCharts.drawChart('chart-memory', perfMemoryData, 'rgb(86, 156, 214)');
        SharpInspectCharts.drawChart('chart-gc-heap', perfGcHeapData, 'rgb(220, 220, 170)');
        SharpInspectCharts.drawChart('chart-threads', perfThreadData, 'rgb(206, 145, 120)');
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
        document.getElementById('perf-memory').textContent = SharpInspectUtils.formatBytes(entry.workingSetBytes);
        document.getElementById('perf-gc-heap').textContent = SharpInspectUtils.formatBytes(entry.totalMemoryBytes);
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

    // ===== Application Panel =====
    function renderApplicationInfo() {
        if (!applicationInfo) return;
        var info = applicationInfo;

        // App Info section
        var appInfoData = [
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
        ];

        var appInfoHtml = appInfoData.map(function(item) {
            return '<div style="display:flex;padding:2px 0">' +
                '<span style="color:#969696;min-width:120px">' + SharpInspectUtils.escapeHtml(item[0]) + ':</span>' +
                '<span>' + SharpInspectUtils.escapeHtml(String(item[1])) + '</span>' +
                '</div>';
        }).join('');
        document.getElementById('app-info-content').innerHTML = appInfoHtml;

        renderEnvironmentVariables();
        renderAssemblies();

        document.getElementById('app-status').textContent =
            'Last update: ' + new Date(info.timestamp).toLocaleTimeString();
    }

    function renderEnvironmentVariables() {
        if (!applicationInfo) return;
        var filter = (document.getElementById('env-filter').value || '').toLowerCase();
        var envVars = applicationInfo.environmentVariables || {};
        var keys = Object.keys(envVars).sort();
        var filtered = keys.filter(function(k) {
            return !filter || k.toLowerCase().indexOf(filter) !== -1 || envVars[k].toLowerCase().indexOf(filter) !== -1;
        });

        document.getElementById('env-count').textContent = keys.length;

        var html = '<table><thead><tr><th style="width:30%">Key</th><th>Value</th></tr></thead><tbody>' +
            filtered.map(function(k) {
                return '<tr><td style="color:#4ec9b0">' + SharpInspectUtils.escapeHtml(k) + '</td>' +
                    '<td style="word-break:break-all;white-space:normal">' + SharpInspectUtils.escapeHtml(envVars[k]) + '</td></tr>';
            }).join('') +
            '</tbody></table>';
        document.getElementById('env-vars-content').innerHTML = html;
    }

    function renderAssemblies() {
        if (!applicationInfo) return;
        var filter = (document.getElementById('asm-filter').value || '').toLowerCase();
        var assemblies = applicationInfo.loadedAssemblies || [];
        var filtered = assemblies.filter(function(a) {
            return !filter || (a.name || '').toLowerCase().indexOf(filter) !== -1 || (a.location || '').toLowerCase().indexOf(filter) !== -1;
        });

        document.getElementById('assembly-count').textContent = assemblies.length;

        document.getElementById('assembly-list').innerHTML = filtered.map(function(a) {
            return '<tr><td title="' + SharpInspectUtils.escapeHtml(a.fullName || '') + '">' + SharpInspectUtils.escapeHtml(a.name || '') + '</td>' +
                '<td>' + SharpInspectUtils.escapeHtml(a.version || '') + '</td>' +
                '<td style="word-break:break-all;white-space:normal;max-width:400px" title="' + SharpInspectUtils.escapeHtml(a.location || '') + '">' + SharpInspectUtils.escapeHtml(a.location || '') + '</td>' +
                '<td>' + (a.isDynamic ? 'Yes' : '') + '</td></tr>';
        }).join('');
    }

    // ===== WebSocket =====
    function connectWebSocket() {
        var wsUrl = 'ws://' + window.location.host + '/ws';
        ws = new WebSocket(wsUrl);

        ws.onopen = function() {
            wsStatus.classList.add('connected');
            wsStatus.title = 'WebSocket connected';
        };

        ws.onclose = function() {
            wsStatus.classList.remove('connected');
            wsStatus.title = 'WebSocket disconnected';
            setTimeout(connectWebSocket, 2000);
        };

        ws.onerror = function() {
            ws.close();
        };

        ws.onmessage = function(event) {
            try {
                var msg = JSON.parse(event.data);
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
            } catch (ex) {}
        };
    }

    // ===== Event Handlers =====
    function initEventHandlers() {
        // Filter inputs
        filterInput.addEventListener('input', function() { renderNetworkList(); });
        consoleFilterInput.addEventListener('input', function() { renderConsoleList(); });

        // Clear buttons
        document.getElementById('clear-btn').addEventListener('click', function() {
            fetch(API_BASE + '/api/network/clear', { method: 'POST' })
                .then(function() {
                    networkEntries = [];
                    selectedEntry = null;
                    renderNetworkList();
                    detailPanel.style.display = 'none';
                });
        });

        document.getElementById('export-har-btn').addEventListener('click', function() {
            window.location.href = API_BASE + '/api/network/export/har';
        });

        document.getElementById('console-clear-btn').addEventListener('click', function() {
            fetch(API_BASE + '/api/console/clear', { method: 'POST' })
                .then(function() {
                    consoleEntries = [];
                    renderConsoleList();
                });
        });

        document.getElementById('perf-clear-btn').addEventListener('click', function() {
            fetch(API_BASE + '/api/performance/clear', { method: 'POST', headers: {'Content-Length': '0'} })
                .then(function() {
                    perfCpuData = [];
                    perfMemoryData = [];
                    perfGcHeapData = [];
                    perfThreadData = [];
                    renderPerfCharts();
                    document.getElementById('perf-status').textContent = 'Cleared';
                });
        });

        document.getElementById('app-refresh-btn').addEventListener('click', function() {
            fetch(API_BASE + '/api/application/refresh', { method: 'POST' });
            fetch(API_BASE + '/api/application')
                .then(function(r) { return r.json(); })
                .then(function(data) {
                    applicationInfo = data;
                    renderApplicationInfo();
                })
                .catch(console.error);
        });

        document.getElementById('env-filter').addEventListener('input', renderEnvironmentVariables);
        document.getElementById('asm-filter').addEventListener('input', renderAssemblies);

        // Copy dropdown
        initCopyDropdown();
    }

    function initCopyDropdown() {
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
                var text = SharpInspectCopyAs.convert(selectedEntry, format);

                if (text) {
                    SharpInspectUtils.copyToClipboard(text).then(function() {
                        SharpInspectUtils.showToast('Copied as ' + format);
                    });
                }

                copyMenu.classList.remove('show');
            });
        }
    }

    // ===== Load Initial Data =====
    function loadInitialData() {
        fetch(API_BASE + '/api/network?limit=1000')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                networkEntries = data.items || [];
                renderNetworkList();
            })
            .catch(console.error);

        fetch(API_BASE + '/api/console?limit=5000')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                consoleEntries = data.items || [];
                renderConsoleList();
            })
            .catch(console.error);

        fetch(API_BASE + '/api/performance?limit=' + PERF_MAX_POINTS)
            .then(function(r) { return r.json(); })
            .then(function(data) {
                var items = data.items || [];
                items.forEach(function(entry) {
                    updatePerformanceUI(entry);
                });
            })
            .catch(console.error);

        fetch(API_BASE + '/api/application')
            .then(function(r) { return r.json(); })
            .then(function(data) {
                applicationInfo = data;
                renderApplicationInfo();
            })
            .catch(console.error);
    }

    // ===== Initialize =====
    function init() {
        initTabs();
        initDetailTabs();
        initEventHandlers();
        loadInitialData();
        connectWebSocket();
        ThemeManager.init();
    }

    // Start the application
    init();
})();
