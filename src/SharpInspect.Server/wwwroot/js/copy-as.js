/**
 * SharpInspect DevTools - Copy As Module
 * HTTP 요청을 다양한 형식으로 변환 (cURL, PowerShell, fetch, C# HttpClient)
 */
var SharpInspectCopyAs = (function() {
    var SQ = String.fromCharCode(39);  // 작은따옴표
    var BS = String.fromCharCode(92);  // 백슬래시
    var DQ = String.fromCharCode(34);  // 큰따옴표
    var NL = String.fromCharCode(10);  // 줄바꿈
    var BT = String.fromCharCode(96);  // 백틱

    /**
     * Windows CMD 인수 이스케이프
     */
    function escapeCmdArg(str) {
        if (!str) return str;
        return str.split(BS).join(BS + BS).split(DQ).join(BS + DQ);
    }

    /**
     * Bash 인수 이스케이프
     */
    function escapeBashArg(str) {
        if (!str) return SQ + SQ;
        return SQ + str.split(SQ).join(SQ + BS + SQ + SQ) + SQ;
    }

    /**
     * PowerShell 인수 이스케이프
     */
    function escapePowerShellArg(str) {
        if (!str) return str;
        return str.split(DQ).join(BS + DQ).split(BT).join(BT + BT);
    }

    /**
     * JavaScript 문자열 이스케이프
     */
    function escapeJsString(str) {
        if (!str) return str;
        return str.split(BS).join(BS + BS).split(DQ).join(BS + DQ).split(NL).join(BS + 'n');
    }

    /**
     * C# 문자열 이스케이프
     */
    function escapeCSharpString(str) {
        if (!str) return str;
        return str.split(BS).join(BS + BS).split(DQ).join(BS + DQ).split(NL).join(BS + 'n');
    }

    /**
     * cURL (Windows cmd) 형식으로 변환
     */
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

    /**
     * cURL (bash) 형식으로 변환
     */
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

    /**
     * PowerShell 형식으로 변환
     */
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

    /**
     * JavaScript fetch 형식으로 변환
     */
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

    /**
     * C# HttpClient 형식으로 변환
     */
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

    /**
     * 지정된 형식으로 변환
     */
    function convert(entry, format) {
        switch (format) {
            case 'curl-cmd':
                return toCurlCmd(entry);
            case 'curl-bash':
                return toCurlBash(entry);
            case 'powershell':
                return toPowerShell(entry);
            case 'fetch':
                return toFetch(entry);
            case 'csharp':
                return toCSharpHttpClient(entry);
            default:
                return '';
        }
    }

    // Public API
    return {
        toCurlCmd: toCurlCmd,
        toCurlBash: toCurlBash,
        toPowerShell: toPowerShell,
        toFetch: toFetch,
        toCSharpHttpClient: toCSharpHttpClient,
        convert: convert
    };
})();
