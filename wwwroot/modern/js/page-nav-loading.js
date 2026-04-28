/**
 * Shows a blocking loading modal during full-page navigations to slow reporting GETs
 * (monthly, performance, RAID) and when their filter forms are submitted.
 * @see _PageNavLoadingOverlay.cshtml (reuses .generating-report-* styles)
 */
(function () {
  'use strict';

  var HINT = 'Please wait while the page is prepared.';

  function getOverlay() {
    return document.getElementById('page-nav-loading-overlay');
  }

  function getDialog() {
    return document.getElementById('page-nav-loading-dialog');
  }

  function getTitleEl() {
    return document.getElementById('page-nav-loading-title');
  }

  function getHintEl() {
    return document.getElementById('page-nav-loading-hint');
  }

  function show() {
    var overlay = getOverlay();
    var dialog = getDialog();
    if (!overlay || !dialog) return;
    overlay.style.display = 'flex';
    overlay.setAttribute('aria-hidden', 'false');
    dialog.style.display = 'block';
    document.documentElement.style.overflow = 'hidden';
  }

  function hide() {
    var overlay = getOverlay();
    var dialog = getDialog();
    if (!overlay || !dialog) return;
    overlay.style.display = 'none';
    overlay.setAttribute('aria-hidden', 'true');
    dialog.style.display = 'none';
    document.documentElement.style.overflow = '';
  }

  /**
   * @param {string} pathname normalised (lowercase, no trailing slash except root)
   * @returns {{ title: string } | null}
   */
  function slowReportingTitleForPathname(pathname) {
    if (!pathname) return null;
    var p = pathname.toLowerCase().replace(/\/+$/, '') || '/';
    if (p.endsWith('/modern/reporting/monthly-update')) {
      return { title: 'Loading monthly report' };
    }
    if (p.endsWith('/modern/reporting/performance')) {
      return { title: 'Loading performance report' };
    }
    if (p.endsWith('/modern/reporting/raid')) {
      return { title: 'Loading RAID report' };
    }
    return null;
  }

  /**
   * @param {string} href absolute or same-document
   */
  function showForDestinationHref(href) {
    try {
      var u = new URL(href, window.location.href);
      if (u.origin !== window.location.origin) return;
      var copy = slowReportingTitleForPathname(u.pathname);
      if (!copy) return;
      var t = getTitleEl();
      var h = getHintEl();
      if (t) {
        t.textContent = copy.title;
      }
      if (h) {
        h.textContent = HINT;
      }
      show();
    } catch (e) {
      // ignore
    }
  }

  function hrefTargetsSlowReporting(href) {
    if (!href || href.charAt(0) === '#') return false;
    if (href.toLowerCase().indexOf('javascript:') === 0) return false;
    try {
      var u = new URL(href, window.location.href);
      if (u.origin !== window.location.origin) return false;
      return slowReportingTitleForPathname(u.pathname) != null;
    } catch (e) {
      return false;
    }
  }

  function isSlowReportingPathname(pathname) {
    return slowReportingTitleForPathname(pathname) != null;
  }

  function init() {
    var overlay = getOverlay();
    if (!overlay) return;

    window.addEventListener('load', hide);
    window.addEventListener('pageshow', function () {
      hide();
    });

    document.addEventListener(
      'click',
      function (e) {
        var a = e.target && e.target.closest && e.target.closest('a[href]');
        if (!a) return;
        if (e.defaultPrevented) return;
        if (e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey) return;
        if (a.hasAttribute('data-skip-page-loading')) return;
        if (!a.getAttribute('href') || a.getAttribute('href').charAt(0) === '#') return;
        if (a.hasAttribute('target') && a.getAttribute('target') !== '' && a.getAttribute('target') !== '_self')
          return;
        if (hrefTargetsSlowReporting(a.href)) {
          showForDestinationHref(a.href);
        }
      },
      true
    );

    document.addEventListener(
      'submit',
      function (e) {
        var form = e.target;
        if (!form || form.nodeName !== 'FORM') return;
        if (form.hasAttribute('data-skip-page-loading')) return;
        var method = (form.getAttribute('method') || 'get').toLowerCase();
        if (form.hasAttribute('data-page-loading')) {
          if (method === 'get') {
            try {
              var actionUrl = form.getAttribute('action') || window.location.href;
              var dest = new URL(actionUrl, window.location.origin).toString();
              if (hrefTargetsSlowReporting(dest)) {
                showForDestinationHref(dest);
              } else {
                var tEl = getTitleEl();
                if (tEl) {
                  tEl.textContent = 'Loading report';
                }
                var hEl = getHintEl();
                if (hEl) {
                  hEl.textContent = HINT;
                }
                show();
              }
            } catch (err) {
              show();
            }
          }
          return;
        }
        if (method !== 'get') return;
        var act = form.getAttribute('action');
        var matchPath = null;
        try {
          if (act) {
            var u = new URL(act, window.location.origin);
            if (isSlowReportingPathname(u.pathname)) {
              matchPath = u.toString();
            }
          } else if (isSlowReportingPathname(window.location.pathname)) {
            matchPath = window.location.href;
          }
        } catch (err) {
          return;
        }
        if (matchPath) {
          showForDestinationHref(matchPath);
        }
      },
      true
    );
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
