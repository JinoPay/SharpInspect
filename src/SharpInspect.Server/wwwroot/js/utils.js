/**
 * SharpInspect DevTools - Utility Functions
 * 공통 유틸리티 함수들
 */
var SharpInspectUtils = (function() {
    /**
     * 바이트 수를 읽기 쉬운 형식으로 변환
     */
    function formatBytes(bytes) {
        if (bytes === 0 || bytes === -1) return '-';
        if (bytes < 1024) return bytes + ' B';
        if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
        return (bytes / (1024 * 1024)).toFixed(2) + ' MB';
    }

    /**
     * 밀리초를 읽기 쉬운 시간 형식으로 변환
     */
    function formatTime(ms) {
        if (!ms) return '-';
        if (ms < 1000) return Math.round(ms) + ' ms';
        return (ms / 1000).toFixed(2) + ' s';
    }

    /**
     * HTTP 상태 코드에 따른 CSS 클래스 반환
     */
    function getStatusClass(status, isError) {
        if (isError) return 'status-error';
        if (status >= 200 && status < 300) return 'status-2xx';
        if (status >= 300 && status < 400) return 'status-3xx';
        if (status >= 400 && status < 500) return 'status-4xx';
        return 'status-5xx';
    }

    /**
     * 콘솔 로그 레벨에 따른 CSS 클래스 반환
     */
    function getLevelClass(level) {
        var levelClasses = {
            'Trace': 'console-trace',
            'Debug': 'console-debug',
            'Information': 'console-info',
            'Warning': 'console-warning',
            'Error': 'console-error',
            'Critical': 'console-critical'
        };
        return levelClasses[level] || '';
    }

    /**
     * HTML 이스케이프
     */
    function escapeHtml(text) {
        if (!text) return '';
        return text.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
    }

    /**
     * 토스트 메시지 표시
     */
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

    /**
     * URL에서 name(경로 부분)을 추출
     * 크롬 개발자 도구의 Network 탭처럼 표시
     */
    function getUrlName(url) {
        if (!url) return '';
        try {
            var urlObj = new URL(url);
            var pathname = urlObj.pathname;
            var search = urlObj.search;

            // 경로의 마지막 부분 추출 (파일명 또는 마지막 세그먼트)
            var name = pathname;
            if (pathname !== '/') {
                var segments = pathname.split('/').filter(function(s) { return s; });
                if (segments.length > 0) {
                    name = segments[segments.length - 1];
                }
            }

            // 쿼리 스트링이 있으면 추가
            if (search) {
                name += search;
            }

            return name || '/';
        } catch (e) {
            // URL 파싱 실패 시 원본 반환
            return url;
        }
    }

    /**
     * 클립보드에 텍스트 복사
     */
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

    // Public API
    return {
        formatBytes: formatBytes,
        formatTime: formatTime,
        getStatusClass: getStatusClass,
        getLevelClass: getLevelClass,
        escapeHtml: escapeHtml,
        showToast: showToast,
        copyToClipboard: copyToClipboard,
        getUrlName: getUrlName
    };
})();
