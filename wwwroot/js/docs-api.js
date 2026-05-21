/* ===========================================================================
 * /docs/api — endpoint card behaviour (expand/collapse, tabs, copy buttons,
 * deep-link hash navigation). The interactive explorer lives on its own page
 * now (see docs-api-explorer.js).
 * =========================================================================== */
(function () {
    'use strict';

    var root = document.getElementById('developers-doc-root') || document;

    function $$(sel, scope) {
        return Array.prototype.slice.call((scope || document).querySelectorAll(sel));
    }

    /* ---------- Endpoint card expand/collapse ---------- */
    function initEndpoints() {
        $$('.api-endpoint-header', root).forEach(function (header) {
            function resolveBody() {
                var id = header.getAttribute('aria-controls');
                var byId = id ? document.getElementById(id) : null;
                if (byId) return byId;
                var sib = header.nextElementSibling;
                while (sib && !sib.classList.contains('api-endpoint-body')) {
                    sib = sib.nextElementSibling;
                }
                return sib;
            }
            header.addEventListener('click', function () {
                var open = header.getAttribute('aria-expanded') === 'true';
                header.setAttribute('aria-expanded', open ? 'false' : 'true');
                var body = resolveBody();
                if (!body) return;
                if (open) body.setAttribute('hidden', '');
                else body.removeAttribute('hidden');
            });
        });
    }

    /* ---------- ARIA tab groups ---------- */
    function initTabGroups() {
        $$('[data-developers-tabs]', root).forEach(function (group) {
            var tabs = $$('button[role="tab"]', group).filter(function (tab) {
                var panel = document.getElementById(tab.getAttribute('aria-controls'));
                return panel && panel.parentElement === group;
            });
            if (tabs.length === 0) return;

            function activate(tab) {
                tabs.forEach(function (other) {
                    var isActive = other === tab;
                    other.setAttribute('aria-selected', isActive ? 'true' : 'false');
                    other.setAttribute('tabindex', isActive ? '0' : '-1');
                    var panel = document.getElementById(other.getAttribute('aria-controls'));
                    if (!panel) return;
                    if (isActive) panel.removeAttribute('hidden');
                    else panel.setAttribute('hidden', '');
                });
            }

            tabs.forEach(function (tab, index) {
                tab.addEventListener('click', function (event) {
                    event.preventDefault();
                    activate(tab);
                });
                tab.addEventListener('keydown', function (event) {
                    var next = null;
                    if (event.key === 'ArrowRight') next = tabs[(index + 1) % tabs.length];
                    else if (event.key === 'ArrowLeft') next = tabs[(index - 1 + tabs.length) % tabs.length];
                    else if (event.key === 'Home') next = tabs[0];
                    else if (event.key === 'End') next = tabs[tabs.length - 1];
                    if (next) {
                        event.preventDefault();
                        activate(next);
                        next.focus();
                    }
                });
            });
        });
    }

    /* ---------- Copy-to-clipboard ---------- */
    function initCopyButtons() {
        $$('[data-developers-copy-target]', root).forEach(function (btn) {
            btn.addEventListener('click', function () {
                var block = btn.closest('.api-code-block');
                if (!block) return;
                var pre = block.querySelector('pre');
                if (!pre) return;
                var text = pre.textContent || '';
                if (!navigator.clipboard) {
                    var range = document.createRange();
                    range.selectNodeContents(pre);
                    var sel = window.getSelection();
                    sel.removeAllRanges();
                    sel.addRange(range);
                    try { document.execCommand('copy'); } catch (_) { /* ignore */ }
                    sel.removeAllRanges();
                } else {
                    navigator.clipboard.writeText(text).catch(function () { /* ignore */ });
                }
                var original = btn.textContent;
                btn.textContent = 'Copied';
                btn.classList.add('api-code-copy--copied');
                setTimeout(function () {
                    btn.textContent = original;
                    btn.classList.remove('api-code-copy--copied');
                }, 1500);
            });
        });
    }

    /* ---------- Hash navigation (open card matching window.location.hash) ----------
       Remaps legacy hashes from the previous _ApiDocsBody.cshtml page so old
       bookmarks still land on a sensible section. */
    var LEGACY_HASH_ALIASES = {
        'risks-endpoints': 'risks',
        'issues-endpoints': 'issues',
        'milestones-endpoints': 'milestones',
        'health-endpoint': 'health',
        'lookups-endpoints': 'admin-lookups',
        'authentication': 'overview',
        'endpoints': 'overview',
        'explorer': 'overview',
        'actions-endpoints': 'overview',
        'accessibility-endpoints': 'overview',
        'survey-endpoints': 'overview',
        'statement-templates-endpoints': 'overview'
    };

    function openHashTarget() {
        var hash = window.location.hash;
        if (!hash || hash.length < 2) return;
        var key = hash.slice(1).toLowerCase();
        var alias = LEGACY_HASH_ALIASES[key];
        if (alias) {
            try { history.replaceState(null, '', '#' + alias); } catch (_) { /* ignore */ }
            hash = '#' + alias;
        }
        var target;
        try { target = document.querySelector(hash); } catch (_) { return; }
        if (!target) return;
        var card = target.closest('.api-endpoint');
        if (card) {
            var header = card.querySelector('.api-endpoint-header');
            var body = card.querySelector('.api-endpoint-body');
            if (header && body) {
                header.setAttribute('aria-expanded', 'true');
                body.removeAttribute('hidden');
            }
        }
        window.requestAnimationFrame(function () {
            target.scrollIntoView({ behavior: 'smooth', block: 'start' });
        });
    }

    function init() {
        initEndpoints();
        initTabGroups();
        initCopyButtons();
        openHashTarget();
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }

    window.addEventListener('hashchange', openHashTarget);
})();
