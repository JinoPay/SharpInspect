/**
 * SharpInspect DevTools - Charts Module
 * 성능 모니터링 차트 관련 함수들
 */
var SharpInspectCharts = (function() {
    var PERF_MAX_POINTS = 60;

    /**
     * CSS 변수에서 차트 그리드 색상 가져오기
     */
    function getChartGridColor() {
        return getComputedStyle(document.documentElement).getPropertyValue('--chart-grid').trim() || '#333';
    }

    /**
     * 미니 차트 그리기
     */
    function drawChart(canvasId, data, color, formatLabel) {
        var canvas = document.getElementById(canvasId);
        if (!canvas) return;
        var ctx = canvas.getContext('2d');
        var w = canvas.width = canvas.offsetWidth || canvas.parentElement.offsetWidth || 300;
        var h = canvas.height = 120;
        if (w <= 0) return;
        ctx.clearRect(0, 0, w, h);

        if (data.length < 2) return;

        var max = Math.max.apply(null, data) * 1.1 || 1;
        var min = 0;
        var range = max - min || 1;

        // Grid lines
        ctx.strokeStyle = getChartGridColor();
        ctx.lineWidth = 0.5;
        for (var i = 0; i <= 4; i++) {
            var y = (h / 4) * i;
            ctx.beginPath();
            ctx.moveTo(0, y);
            ctx.lineTo(w, y);
            ctx.stroke();
        }

        // Data line
        ctx.strokeStyle = color;
        ctx.lineWidth = 1.5;
        ctx.beginPath();
        var step = w / (PERF_MAX_POINTS - 1);
        var offset = PERF_MAX_POINTS - data.length;
        for (var j = 0; j < data.length; j++) {
            var x = (offset + j) * step;
            var yPos = h - ((data[j] - min) / range) * (h - 4) - 2;
            if (j === 0) ctx.moveTo(x, yPos);
            else ctx.lineTo(x, yPos);
        }
        ctx.stroke();

        // Fill under line
        ctx.lineTo((offset + data.length - 1) * step, h);
        ctx.lineTo(offset * step, h);
        ctx.closePath();
        ctx.fillStyle = color.replace(')', ', 0.1)').replace('rgb', 'rgba');
        ctx.fill();
    }

    // Public API
    return {
        PERF_MAX_POINTS: PERF_MAX_POINTS,
        drawChart: drawChart
    };
})();
