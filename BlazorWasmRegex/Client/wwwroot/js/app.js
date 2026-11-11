// Earlier versions registered a PWA service worker that served a cached index.html for every
// navigation. That page no longer exists, so remove any worker still installed in a browser.
if ('serviceWorker' in navigator) {
    navigator.serviceWorker.getRegistrations().then(function (registrations) {
        for (const registration of registrations) {
            registration.unregister();
        }
    });
}

// Used by RegexTester's export menu.
window.downloadFile = function (filename, content, contentType) {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    URL.revokeObjectURL(url);
};

(function () {
    'use strict';

    // ---- Theme (the initial theme is applied by an inline script in App.razor's <head>) ------------
    const THEME_KEY = 'rx-theme';

    function currentTheme() {
        return document.documentElement.getAttribute('data-theme') === 'dark' ? 'dark' : 'light';
    }

    function applyThemeButtons() {
        const dark = currentTheme() === 'dark';
        document.querySelectorAll('[data-theme-toggle]').forEach(function (btn) {
            btn.setAttribute('aria-pressed', dark ? 'true' : 'false');
            btn.setAttribute('aria-label', dark ? 'Switch to light theme' : 'Switch to dark theme');
        });
    }

    function toggleTheme() {
        const next = currentTheme() === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        try { localStorage.setItem(THEME_KEY, next); } catch (e) { /* storage blocked: theme lasts for this page */ }
        applyThemeButtons();
    }

    // ---- Toast ------------------------------------------------------------------------------------
    let toastTimer = null;
    function toast(message) {
        let el = document.getElementById('toast');
        if (!el) {
            el = document.createElement('div');
            el.id = 'toast';
            el.className = 'toast';
            el.setAttribute('role', 'status');
            el.setAttribute('aria-live', 'polite');
            document.body.appendChild(el);
        }
        el.textContent = message;
        el.classList.add('is-visible');
        clearTimeout(toastTimer);
        toastTimer = setTimeout(function () { el.classList.remove('is-visible'); }, 1600);
    }

    // ---- Copy buttons ([data-copy]) -------------------------------------------------------------------
    function copyText(text, button) {
        function done() {
            button.classList.add('is-copied');
            setTimeout(function () { button.classList.remove('is-copied'); }, 1200);
            toast('Copied to clipboard');
        }
        function fallback() {
            const area = document.createElement('textarea');
            area.value = text;
            area.setAttribute('readonly', '');
            area.style.position = 'fixed';
            area.style.opacity = '0';
            document.body.appendChild(area);
            area.select();
            try { document.execCommand('copy'); done(); } catch (e) { toast('Copy failed'); }
            document.body.removeChild(area);
        }
        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(text).then(done, fallback);
        } else {
            fallback();
        }
    }

    // ---- Hovering a match highlights it in every view ([data-m]) -----------------------------------
    let hot = null;
    function setHot(id) {
        if (hot === id) return;
        document.querySelectorAll('.is-hot').forEach(function (n) { n.classList.remove('is-hot'); });
        hot = id;
        if (id) {
            document.querySelectorAll('[data-m="' + CSS.escape(id) + '"]').forEach(function (n) { n.classList.add('is-hot'); });
        }
    }

    // ---- Quick reference: insert a token into the pattern or replacement field ----------------------
    const lastCaret = new Map(); // field id -> { start, end }

    function rememberCaret(el) {
        if (el && (el.id === 'patternInput' || el.id === 'replaceInput')) {
            lastCaret.set(el.id, { start: el.selectionStart, end: el.selectionEnd });
        }
    }

    function insertToken(text, targetId) {
        const field = document.getElementById(targetId);
        if (!field || field.disabled) {
            toast('The regex engine is still loading');
            return;
        }
        const caret = lastCaret.get(targetId) || { start: field.value.length, end: field.value.length };
        const start = Math.min(caret.start, field.value.length);
        const end = Math.min(caret.end, field.value.length);
        const selected = field.value.slice(start, end);

        // Group tokens like "(?<name>)" wrap the selected text; other tokens replace it.
        let insert = text;
        let cursor = start + text.length;
        const close = text.endsWith(')') && text.length > 1 && !/^\(\?[imnsx-]+\)$/.test(text);
        if (close) {
            insert = text.slice(0, -1) + selected + ')';
            cursor = start + insert.length - (selected ? 0 : 1);
        }

        field.value = field.value.slice(0, start) + insert + field.value.slice(end);
        // Blazor's @bind listens for 'input', so this updates the component like typing would.
        field.dispatchEvent(new Event('input', { bubbles: true }));
        field.focus();
        field.setSelectionRange(cursor, cursor);
        rememberCaret(field);
    }

    // ---- Document-level delegation (works for static pages and the interactive tester) --------------
    document.addEventListener('click', function (e) {
        const toggle = e.target.closest('[data-theme-toggle]');
        if (toggle) { toggleTheme(); return; }

        const copy = e.target.closest('[data-copy]');
        if (copy) { copyText(copy.getAttribute('data-copy'), copy); return; }

        const token = e.target.closest('[data-insert]');
        if (token) { insertToken(token.getAttribute('data-insert'), token.getAttribute('data-target') || 'patternInput'); }
    });

    document.addEventListener('mouseover', function (e) {
        const m = e.target.closest && e.target.closest('[data-m]');
        setHot(m ? m.getAttribute('data-m') : null);
    });

    ['keyup', 'mouseup', 'select', 'input', 'focusout'].forEach(function (type) {
        document.addEventListener(type, function (e) { rememberCaret(e.target); }, true);
    });

    document.addEventListener('DOMContentLoaded', applyThemeButtons);
    applyThemeButtons();

    // ---- Regex tester editor: scroll sync and gutter heights ------------------------------------------
    window.regexTester = {
        attach: function (root) {
            if (!root || root.__rxAttached) return;
            root.__rxAttached = true;

            const pattern = root.querySelector('#patternInput');
            const textarea = root.querySelector('#testInput');
            const backdrop = root.querySelector('.test-editor__backdrop');
            const gutter = root.querySelector('.test-editor__gutter-inner');

            function syncPattern() {
                const mirror = root.querySelector('.code-input__mirror');
                if (mirror && pattern) mirror.style.transform = 'translateX(' + (-pattern.scrollLeft) + 'px)';
            }

            function syncEditor() {
                if (!textarea) return;
                backdrop.scrollTop = textarea.scrollTop;
                gutter.style.transform = 'translateY(' + (-textarea.scrollTop) + 'px)';
            }

            // Each gutter row takes the height of its (possibly soft-wrapped) line in the backdrop.
            function layoutGutter() {
                const rows = gutter.children;
                const lines = backdrop.children;
                for (let i = 0; i < rows.length; i++) {
                    const h = lines[i] ? lines[i].offsetHeight : 0;
                    rows[i].style.height = h ? h + 'px' : '';
                }
                syncEditor();
            }

            let queued = false;
            function schedule() {
                if (queued) return;
                queued = true;
                requestAnimationFrame(function () {
                    queued = false;
                    layoutGutter();
                    syncPattern();
                });
            }

            if (pattern) {
                ['input', 'scroll', 'keyup', 'click', 'focus'].forEach(function (t) { pattern.addEventListener(t, syncPattern); });
            }
            if (textarea) {
                textarea.addEventListener('scroll', syncEditor);
                new ResizeObserver(schedule).observe(textarea);
            }
            // Blazor re-renders the backdrop and gutter after every run.
            new MutationObserver(schedule).observe(root, { childList: true, subtree: true, characterData: true });
            schedule();
        }
    };
})();
