/* /docs/specifications — aside TOC: collapse area children until that area is active. */
(function () {
    'use strict';

    var nav = document.querySelector('[data-spec-aside-nav]');
    if (!nav) return;

    var areaItems = Array.prototype.slice.call(nav.querySelectorAll('[data-spec-area-item]'));
    var links = Array.prototype.slice.call(nav.querySelectorAll('a[href^="#"]'));

    function areaIdForHash(hash) {
        if (!hash || hash.length < 2) return null;
        var id = hash.slice(1);
        var areaItem = nav.querySelector('[data-spec-area-item][data-spec-area="' + id + '"]');
        if (areaItem) return id;
        var childLink = nav.querySelector('a[href="' + hash + '"][data-spec-view]');
        if (childLink) return childLink.getAttribute('data-spec-area');
        return null;
    }

    function setExpanded(areaId) {
        areaItems.forEach(function (item) {
            var id = item.getAttribute('data-spec-area');
            var expanded = areaId && id === areaId;
            item.classList.toggle('docs-spec-aside-nav__area--expanded', expanded);
            var panel = item.querySelector('[data-spec-area-panel]');
            if (panel) {
                if (expanded) panel.removeAttribute('hidden');
                else panel.setAttribute('hidden', '');
            }
            var parentLink = item.querySelector('[data-spec-area-link]');
            if (parentLink) {
                parentLink.setAttribute('aria-expanded', expanded ? 'true' : 'false');
            }
        });
    }

    function setCurrent(hash) {
        links.forEach(function (link) {
            var match = hash && link.getAttribute('href') === hash;
            if (match) {
                link.setAttribute('aria-current', 'location');
                link.classList.add('active');
            } else {
                link.removeAttribute('aria-current');
                link.classList.remove('active');
            }
        });
    }

    function syncFromHash() {
        var hash = window.location.hash || '';
        setExpanded(areaIdForHash(hash));
        setCurrent(hash || null);
    }

    function initScrollSpy() {
        var main = document.querySelector('.developers-specifications');
        if (!main || !('IntersectionObserver' in window)) return;

        var targets = [];
        var overview = document.getElementById('overview');
        if (overview) targets.push(overview);
        Array.prototype.slice.call(main.querySelectorAll('h2[id], .docs-spec-card[id]')).forEach(function (el) {
            targets.push(el);
        });
        if (targets.length === 0) return;

        var visible = new Map();

        var observer = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                visible.set(entry.target.id, entry.isIntersecting ? entry.intersectionRatio : 0);
            });

            var bestId = null;
            var bestRatio = 0;
            var bestTop = Infinity;
            targets.forEach(function (el) {
                var ratio = visible.get(el.id) || 0;
                if (ratio <= 0) return;
                var top = el.getBoundingClientRect().top;
                if (ratio > bestRatio || (ratio === bestRatio && top < bestTop)) {
                    bestRatio = ratio;
                    bestTop = top;
                    bestId = el.id;
                }
            });

            if (!bestId) return;

            var hash = '#' + bestId;
            var areaId = areaIdForHash(hash);
            setExpanded(areaId);
            setCurrent(hash);
        }, {
            root: null,
            rootMargin: '-10% 0px -55% 0px',
            threshold: [0, 0.1, 0.25, 0.5, 0.75, 1]
        });

        targets.forEach(function (el) { observer.observe(el); });
    }

    areaItems.forEach(function (item) {
        var parentLink = item.querySelector('[data-spec-area-link]');
        if (!parentLink) return;
        parentLink.addEventListener('click', function () {
            var id = item.getAttribute('data-spec-area');
            setExpanded(id);
        });
    });

    syncFromHash();
    initScrollSpy();
    window.addEventListener('hashchange', syncFromHash);
})();
