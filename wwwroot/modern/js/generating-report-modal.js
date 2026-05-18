/**
 * Shows "Generating report" (or per-link copy) while export/download requests run.
 * Intercepts same-origin GET downloads (Excel, CSV, etc.) and completes via fetch + blob so the overlay stays visible.
 */
(function () {
  var defaultModalTitle = 'Generating report';
  var defaultModalHint = 'Please wait — your download will start when the file is ready.';
  /** @type {HTMLElement | null} */
  var titleElCached = null;
  /** @type {HTMLElement | null} */
  var hintElCached = null;

  function getOverlay() {
    return document.getElementById('generating-report-overlay');
  }

  function getDialog() {
    return document.getElementById('generating-report-dialog');
  }

  /**
   * @param {string | null | undefined} title
   * @param {string | null | undefined} hint
   */
  function show(title, hint) {
    var overlay = getOverlay();
    var dialog = getDialog();
    if (!overlay || !dialog) return;
    if (titleElCached) {
      titleElCached.textContent = title != null && String(title).trim() !== '' ? String(title).trim() : defaultModalTitle;
    }
    if (hintElCached) {
      hintElCached.textContent = hint != null && String(hint).trim() !== '' ? String(hint).trim() : defaultModalHint;
    }
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
    if (titleElCached) titleElCached.textContent = defaultModalTitle;
    if (hintElCached) hintElCached.textContent = defaultModalHint;
  }

  /** @param {URL} url */
  function shouldInterceptExportUrl(url) {
    if (url.origin !== window.location.origin) return false;
    var pathLower = (url.pathname || '').toLowerCase();

    // Exports hub navigation only — not a file download
    if (pathLower === '/exports' || pathLower === '/exports/') return false;

    // /Exports/Download* (Excel) and /Exports/DownloadDemandRegisterCsv — works with path base and camel-cased actions
    if (pathLower.includes('/exports/') && pathLower.includes('download')) return true;
    if (pathLower.includes('export-register')) return true;
    if (pathLower.includes('/export/')) return true;

    var tail = pathLower.replace(/\/+$/, '');
    if (tail.endsWith('/export')) return true;

    return false;
  }

  /** @param {Element | null | undefined} el */
  function readModalCopy(el) {
    if (!el || !el.getAttribute) return { title: null, hint: null };
    return {
      title: el.getAttribute('data-generating-modal-title'),
      hint: el.getAttribute('data-generating-modal-hint')
    };
  }

  /** @param {HTMLAnchorElement} a */
  function anchorLooksLikeExport(a) {
    if (a.hasAttribute('data-skip-report-modal')) return false;
    if (a.hasAttribute('data-report-download')) return true;
    try {
      var u = new URL(a.href, window.location.href);
      return shouldInterceptExportUrl(u);
    } catch (e) {
      return false;
    }
  }

  function filenameFromDisposition(header) {
    if (!header) return 'download';
    var star = header.match(/filename\*=UTF-8''([^;\n]+)/i);
    if (star && star[1]) {
      try {
        return decodeURIComponent(star[1].trim());
      } catch (e) {
        return star[1].trim();
      }
    }
    var m = header.match(/filename[^;=\n]*=\s*((['"]).*?\2|[^;\n]*)/i);
    if (m && m[1]) {
      var raw = m[1].replace(/^["']|["']$/g, '').trim();
      return raw || 'download';
    }
    return 'download';
  }

  /**
   * @param {string} url
   * @param {Element | null | undefined} sourceEl anchor or form (optional modal copy)
   * @returns {Promise<void>}
   */
  function downloadViaFetch(url, sourceEl) {
    var copy = readModalCopy(sourceEl);
    show(copy.title, copy.hint);
    return fetch(url, {
      method: 'GET',
      credentials: 'same-origin',
      headers: { Accept: '*/*' }
    })
      .then(function (response) {
        if (!response.ok) {
          throw new Error('Export failed');
        }
        var disp = response.headers.get('content-disposition');
        var fname = filenameFromDisposition(disp);
        return response.blob().then(function (blob) {
          return { blob: blob, filename: fname };
        });
      })
      .then(function (_ref) {
        var blob = _ref.blob;
        var filename = _ref.filename;
        var urlObj = window.URL.createObjectURL(blob);
        var a = document.createElement('a');
        a.href = urlObj;
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(urlObj);
        hide();
      })
      .catch(function (err) {
        console.error(err);
        hide();
        window.alert('The report could not be generated. Please try again.');
      });
  }

  /**
   * Build full GET URL from a form (for submitter formaction).
   * @param {HTMLFormElement} form
   * @param {string} actionUrl absolute or relative
   */
  function buildFormGetUrl(form, actionUrl) {
    var url = new URL(actionUrl, window.location.origin);
    var fd = new FormData(form);
    fd.forEach(function (value, key) {
      if (!key) return;
      url.searchParams.append(key, value);
    });
    return url.toString();
  }

  function init() {
    var overlay = getOverlay();
    if (!overlay) return;

    titleElCached = document.getElementById('generating-report-title');
    hintElCached =
      document.getElementById('generating-report-hint') ||
      /** @type {HTMLElement | null} */ (document.querySelector('.generating-report-dialog__hint'));
    if (titleElCached && titleElCached.textContent.trim()) defaultModalTitle = titleElCached.textContent.trim();
    if (hintElCached && hintElCached.textContent.trim()) defaultModalHint = hintElCached.textContent.trim();

    document.addEventListener(
      'click',
      function (e) {
        var a = e.target.closest('a[href]');
        if (!a || e.defaultPrevented || e.button !== 0 || e.metaKey || e.ctrlKey || e.shiftKey || e.altKey)
          return;
        if (!anchorLooksLikeExport(a)) return;
        var href = a.getAttribute('href');
        if (!href || href.charAt(0) === '#') return;
        e.preventDefault();
        downloadViaFetch(a.href, a);
      },
      true
    );

    document.addEventListener(
      'submit',
      function (e) {
        var form = e.target;
        if (!(form instanceof HTMLFormElement)) return;
        if (form.hasAttribute('data-skip-report-modal')) return;
        var method = (form.getAttribute('method') || 'get').toLowerCase();
        if (method !== 'get') return;

        if (form.hasAttribute('data-report-download')) {
          var submitterForced = e.submitter;
          var actionForced = submitterForced && submitterForced.getAttribute('formaction');
          var actionUrlForced = actionForced || form.getAttribute('action') || window.location.pathname + window.location.search;
          e.preventDefault();
          downloadViaFetch(buildFormGetUrl(form, actionUrlForced), submitterForced || form);
          return;
        }

        var submitter = e.submitter;
        var actionAttr = submitter && submitter.getAttribute('formaction');
        var actionUrl = actionAttr || form.getAttribute('action') || window.location.pathname + window.location.search;

        try {
          var test = new URL(actionUrl, window.location.origin);
          if (!shouldInterceptExportUrl(test)) return;
        } catch (err) {
          return;
        }

        e.preventDefault();
        var full = buildFormGetUrl(form, actionUrl);
        downloadViaFetch(full, submitter || form);
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
