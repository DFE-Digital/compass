/**
 * Shows a DfE information modal during full-page navigations to slow reporting GETs
 * and when their filter forms are submitted.
 * @see _PageNavLoadingOverlay.cshtml (dfe-f-modal + hidden opener for initModal wiring)
 */
(function () {
  'use strict';

  var DEFAULT_HINT = 'Please wait while the page is prepared.';

  function getDialog() {
    var el = document.getElementById('page-nav-loading-modal');
    return el instanceof HTMLDialogElement ? el : null;
  }

  function getOpenTrigger() {
    return document.getElementById('page-nav-loading-modal-open');
  }

  function getTitleEl() {
    return document.getElementById('page-nav-loading-modal-title');
  }

  function getHintEl() {
    return document.getElementById('page-nav-loading-modal-hint');
  }

  function show() {
    var dlg = getDialog();
    var opener = getOpenTrigger();
    if (!dlg) return;
    if (dlg.open) return;
    if (opener) {
      opener.click();
    }
    if (!dlg.open && typeof dlg.showModal === 'function') {
      dlg.showModal();
    }
  }

  function hide() {
    var dlg = getDialog();
    if (!dlg || !dlg.open) return;
    dlg.close();
  }

  /**
   * @param {string} pathname normalised (lowercase, no trailing slash except root)
   * @returns {{ title: string, hint?: string } | null}
   */
  function slowReportingTitleForPathname(pathname) {
    if (!pathname) return null;
    var p = pathname.toLowerCase().replace(/\/+$/, '') || '/';
    if (p.indexOf('/modern/reporting/') === -1) return null;

    var generatingMonthly = { title: 'Generating report', hint: 'Please wait.' };

    if (p.indexOf('/modern/reporting/monthly-update-overview') !== -1) {
      return generatingMonthly;
    }
    if (p.startsWith('/modern/reporting/monthly-update') && !p.startsWith('/modern/reporting/monthly-update-overview')) {
      return generatingMonthly;
    }
    if (p.indexOf('/modern/reporting/performance') !== -1) {
      return { title: 'Loading performance report' };
    }
    if (p.indexOf('/modern/reporting/raid') !== -1 || p.indexOf('/modern/reporting/risk') !== -1) {
      return { title: 'Loading RAID report' };
    }
    if (p.indexOf('/modern/reporting/assessments') !== -1) {
      return { title: 'Loading service assessments' };
    }
    if (p.indexOf('/modern/reporting/accessibility') !== -1) {
      return { title: 'Loading accessibility report' };
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
        h.textContent =
          copy.hint != null && String(copy.hint).trim() !== '' ? String(copy.hint).trim() : DEFAULT_HINT;
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
    var dlg = getDialog();
    if (!dlg) return;

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
                  hEl.textContent = DEFAULT_HINT;
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
