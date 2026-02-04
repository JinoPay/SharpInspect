/**
 * SharpInspect DevTools - Theme Manager
 * 테마 관리 모듈 (다크/라이트/시스템 테마 지원)
 */
var ThemeManager = {
    STORAGE_KEY: 'sharpinspect-theme',

    getSystemTheme: function() {
        return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
    },

    getSavedTheme: function() {
        return localStorage.getItem(this.STORAGE_KEY) || 'system';
    },

    getEffectiveTheme: function() {
        var saved = this.getSavedTheme();
        return saved === 'system' ? this.getSystemTheme() : saved;
    },

    apply: function(theme) {
        var effective = theme === 'system' ? this.getSystemTheme() : theme;
        document.documentElement.setAttribute('data-theme', effective);
        this.updateButton();
    },

    toggle: function() {
        var current = this.getSavedTheme();
        var next = current === 'dark' ? 'light' : current === 'light' ? 'system' : 'dark';
        localStorage.setItem(this.STORAGE_KEY, next);
        this.apply(next);
    },

    updateButton: function() {
        var btn = document.getElementById('theme-toggle');
        if (!btn) return;
        var icons = { dark: '\uD83C\uDF19', light: '\u2600\uFE0F', system: '\uD83D\uDCBB' };
        var saved = this.getSavedTheme();
        var effective = this.getEffectiveTheme();
        btn.textContent = icons[saved];
        btn.title = 'Theme: ' + saved + (saved === 'system' ? ' (' + effective + ')' : '');
    },

    init: function() {
        var self = this;
        this.apply(this.getSavedTheme());
        window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', function() {
            if (self.getSavedTheme() === 'system') self.apply('system');
        });
        var btn = document.getElementById('theme-toggle');
        if (btn) btn.addEventListener('click', function() { self.toggle(); });
    }
};

// 즉시 테마 적용 (깜빡임 방지)
ThemeManager.apply(ThemeManager.getSavedTheme());
