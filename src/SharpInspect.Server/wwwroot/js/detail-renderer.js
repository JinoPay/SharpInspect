/**
 * SharpInspect DevTools - Detail Renderer
 * Headers, Request/Response, Timing 탭 렌더링 모듈
 */
var SharpInspectDetailRenderer = (function() {
    'use strict';

    var escapeHtml = SharpInspectUtils.escapeHtml;
    var formatTime = SharpInspectUtils.formatTime;

    // ===== Headers 렌더링 =====
    function renderHeaders(entry) {
        var html = '<div class="headers-section">';

        // General 섹션
        html += renderHeaderGroup('General', [
            ['Request URL', entry.url],
            ['Request Method', entry.method],
            ['Status Code', entry.statusCode + ' ' + (entry.statusText || '')],
            ['Protocol', entry.protocol || 'HTTP/1.1']
        ]);

        // Request Headers
        var reqHeaders = entry.requestHeaders || {};
        var reqHeaderItems = Object.keys(reqHeaders).map(function(k) {
            return [k, reqHeaders[k]];
        });
        html += renderHeaderGroup('Request Headers', reqHeaderItems, reqHeaderItems.length);

        // Response Headers
        var resHeaders = entry.responseHeaders || {};
        var resHeaderItems = Object.keys(resHeaders).map(function(k) {
            return [k, resHeaders[k]];
        });
        html += renderHeaderGroup('Response Headers', resHeaderItems, resHeaderItems.length);

        html += '</div>';
        return html;
    }

    function renderHeaderGroup(title, items, count) {
        var countStr = count !== undefined ? ' (' + count + ')' : '';
        var html = '<div class="headers-group">';
        html += '<div class="headers-group-title" data-collapsed="false">';
        html += '<span class="collapse-icon"></span>';
        html += '<span>' + escapeHtml(title) + countStr + '</span>';
        html += '</div>';
        html += '<div class="headers-group-content">';
        html += '<table class="headers-table">';
        items.forEach(function(item) {
            html += '<tr>';
            html += '<td class="header-name">' + escapeHtml(item[0]) + '</td>';
            html += '<td class="header-value">' + escapeHtml(String(item[1])) + '</td>';
            html += '</tr>';
        });
        html += '</table>';
        html += '</div>';
        html += '</div>';
        return html;
    }

    // ===== Form Data 파싱 =====
    function decodeURIComponentSafe(str) {
        try {
            return decodeURIComponent(str.replace(/\+/g, ' '));
        } catch (e) {
            return str;
        }
    }

    function parseFormUrlEncoded(content) {
        if (!content) return [];
        var pairs = [];
        var parts = content.split('&');
        for (var i = 0; i < parts.length; i++) {
            var part = parts[i];
            if (!part) continue;
            var eqIndex = part.indexOf('=');
            if (eqIndex === -1) {
                pairs.push([decodeURIComponentSafe(part), '']);
            } else {
                pairs.push([
                    decodeURIComponentSafe(part.substring(0, eqIndex)),
                    decodeURIComponentSafe(part.substring(eqIndex + 1))
                ]);
            }
        }
        return pairs;
    }

    function parseMultipartFormData(content, contentType) {
        if (!content || !contentType) return [];

        var boundaryMatch = contentType.match(/boundary="?([^"\s;]+)"?/i);
        if (!boundaryMatch) return [];
        var boundary = '--' + boundaryMatch[1];

        var pairs = [];
        var parts = content.split(boundary);

        for (var i = 1; i < parts.length; i++) {
            var part = parts[i];
            if (part.trim() === '--' || part.trim() === '') continue;

            var nameMatch = part.match(/Content-Disposition:[^\n]*name="([^"]*)"(?:;\s*filename="([^"]*)")?/i);
            if (!nameMatch) continue;

            var key = nameMatch[1];
            var filename = nameMatch[2];

            var headerBodySplit = part.indexOf('\r\n\r\n');
            if (headerBodySplit === -1) headerBodySplit = part.indexOf('\n\n');
            if (headerBodySplit === -1) continue;

            var separatorLen = part.indexOf('\r\n\r\n') !== -1 ? 4 : 2;
            var body = part.substring(headerBodySplit + separatorLen).trim();

            if (filename) {
                pairs.push([key, filename + ' (binary)']);
            } else {
                pairs.push([key, body]);
            }
        }
        return pairs;
    }

    function renderFormData(pairs, title) {
        return '<div class="headers-section">' +
            renderHeaderGroup(title || 'Form Data', pairs, pairs.length) +
            '</div>';
    }

    // ===== Body (Request/Response) 렌더링 =====
    function renderBody(content, contentType, isPretty) {
        if (!content || content === '(No request body)' || content === '(No response body)') {
            return '<div class="body-empty">' + escapeHtml(content || '(No body)') + '</div>';
        }

        var type = detectContentType(content, contentType);

        if (isPretty) {
            if (type === 'json') {
                return '<pre class="body-content body-highlighted">' + highlightJson(content) + '</pre>';
            } else if (type === 'xml' || type === 'html') {
                return '<pre class="body-content body-highlighted">' + highlightXml(content) + '</pre>';
            } else if (type === 'form-urlencoded') {
                var urlPairs = parseFormUrlEncoded(content);
                if (urlPairs.length > 0) {
                    return renderFormData(urlPairs, 'Form Data');
                }
            } else if (type === 'form-data') {
                var multiPairs = parseMultipartFormData(content, contentType);
                if (multiPairs.length > 0) {
                    return renderFormData(multiPairs, 'Form Data');
                }
            }
        }

        return '<pre class="body-content">' + escapeHtml(content) + '</pre>';
    }

    function detectContentType(content, contentTypeHeader) {
        if (contentTypeHeader) {
            var ct = contentTypeHeader.toLowerCase();
            if (ct.indexOf('application/json') !== -1 || ct.indexOf('+json') !== -1) return 'json';
            if (ct.indexOf('application/xml') !== -1 || ct.indexOf('text/xml') !== -1 || ct.indexOf('+xml') !== -1) return 'xml';
            if (ct.indexOf('text/html') !== -1) return 'html';
            if (ct.indexOf('application/x-www-form-urlencoded') !== -1) return 'form-urlencoded';
            if (ct.indexOf('multipart/form-data') !== -1) return 'form-data';
        }
        // 내용 기반 감지
        if (content) {
            var trimmed = content.trim();
            if ((trimmed.startsWith('{') && trimmed.endsWith('}')) ||
                (trimmed.startsWith('[') && trimmed.endsWith(']'))) {
                try {
                    JSON.parse(trimmed);
                    return 'json';
                } catch (e) {}
            }
            if (trimmed.startsWith('<?xml') || (trimmed.startsWith('<') && trimmed.indexOf('</') !== -1)) {
                return 'xml';
            }
        }
        return 'text';
    }

    // ===== JSON 구문 강조 =====
    function highlightJson(content) {
        try {
            var parsed = JSON.parse(content);
            return formatJsonValue(parsed, 0);
        } catch (e) {
            return escapeHtml(content);
        }
    }

    function formatJsonValue(value, indent) {
        var spaces = '  '.repeat(indent);
        var nextSpaces = '  '.repeat(indent + 1);

        if (value === null) {
            return '<span class="json-null">null</span>';
        }
        if (typeof value === 'boolean') {
            return '<span class="json-boolean">' + value + '</span>';
        }
        if (typeof value === 'number') {
            return '<span class="json-number">' + value + '</span>';
        }
        if (typeof value === 'string') {
            return '<span class="json-string">"' + escapeHtml(value) + '"</span>';
        }

        if (Array.isArray(value)) {
            if (value.length === 0) return '[]';
            var items = value.map(function(item) {
                return nextSpaces + formatJsonValue(item, indent + 1);
            });
            return '[\n' + items.join(',\n') + '\n' + spaces + ']';
        }

        if (typeof value === 'object') {
            var keys = Object.keys(value);
            if (keys.length === 0) return '{}';
            var props = keys.map(function(key) {
                return nextSpaces + '<span class="json-key">"' + escapeHtml(key) + '"</span>: ' +
                    formatJsonValue(value[key], indent + 1);
            });
            return '{\n' + props.join(',\n') + '\n' + spaces + '}';
        }

        return escapeHtml(String(value));
    }

    // ===== XML/HTML 구문 강조 =====
    function highlightXml(content) {
        // Pretty print XML
        var formatted = prettyPrintXml(content);

        // 이스케이프 후 강조
        return formatted
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            // 태그명
            .replace(/&lt;(\/?)([\w:-]+)/g, '&lt;$1<span class="xml-tag">$2</span>')
            // 속성명="값"
            .replace(/([\w:-]+)(=)(".*?")/g, '<span class="xml-attr">$1</span>$2<span class="xml-value">$3</span>');
    }

    function prettyPrintXml(xml) {
        var formatted = '';
        var indent = '';
        var pad = '  ';

        xml.split(/>\s*</).forEach(function(node, index) {
            if (index > 0) node = '<' + node;
            if (index < xml.split(/>\s*</).length - 1) node = node + '>';

            if (node.match(/^<\/\w/)) {
                indent = indent.substring(pad.length);
            }

            formatted += indent + node + '\n';

            if (node.match(/^<\w[^>]*[^\/]$/) && !node.match(/^<\?/)) {
                indent += pad;
            }
        });

        return formatted.trim();
    }

    // ===== Timing 워터폴 차트 =====
    function renderTiming(entry) {
        var phases = [
            { name: 'DNS Lookup', value: entry.dnsLookupMs || 0, color: '#4ec9b0' },
            { name: 'TCP Connect', value: entry.tcpConnectMs || 0, color: '#569cd6' },
            { name: 'TLS Handshake', value: entry.tlsHandshakeMs || 0, color: '#ce9178' },
            { name: 'Request Sent', value: entry.requestSentMs || 0, color: '#dcdcaa' },
            { name: 'Waiting (TTFB)', value: entry.waitingMs || 0, color: '#c586c0' },
            { name: 'Content Download', value: entry.contentDownloadMs || 0, color: '#9cdcfe' }
        ];

        var total = entry.totalMs || 0;
        var maxValue = total || 1;

        var html = '<div class="timing-container">';

        // Total 표시
        html += '<div class="timing-total">';
        html += '<span class="timing-label">Total</span>';
        html += '<span class="timing-value">' + formatTime(total) + '</span>';
        html += '</div>';

        // 워터폴 바
        html += '<div class="timing-waterfall">';
        phases.forEach(function(phase) {
            var percentage = (phase.value / maxValue) * 100;
            html += '<div class="timing-bar-row">';
            html += '<div class="timing-bar-label">' + phase.name + '</div>';
            html += '<div class="timing-bar-track">';
            html += '<div class="timing-bar" style="width: ' + Math.max(percentage, 0.5) + '%; background: ' + phase.color + ';"></div>';
            html += '</div>';
            html += '<div class="timing-bar-value">' + formatTime(phase.value) + '</div>';
            html += '</div>';
        });
        html += '</div>';

        // 범례
        html += '<div class="timing-legend">';
        phases.forEach(function(phase) {
            html += '<div class="timing-legend-item">';
            html += '<span class="timing-legend-color" style="background: ' + phase.color + ';"></span>';
            html += '<span>' + phase.name + '</span>';
            html += '</div>';
        });
        html += '</div>';

        html += '</div>';
        return html;
    }

    // ===== 이벤트 핸들러 초기화 =====
    function initHeaderCollapse() {
        document.querySelectorAll('.headers-group-title').forEach(function(title) {
            title.addEventListener('click', function() {
                var isCollapsed = this.dataset.collapsed === 'true';
                this.dataset.collapsed = String(!isCollapsed);
                var content = this.nextElementSibling;
                if (content) {
                    content.style.display = isCollapsed ? '' : 'none';
                }
            });
        });
    }

    return {
        renderHeaders: renderHeaders,
        renderBody: renderBody,
        renderTiming: renderTiming,
        initHeaderCollapse: initHeaderCollapse,
        detectContentType: detectContentType
    };
})();
