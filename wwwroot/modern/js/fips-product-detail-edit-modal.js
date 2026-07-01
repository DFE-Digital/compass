/**
 * FIPS product detail — open summary-list “Change” links in a modal with a single-field edit form.
 */
(function () {
  'use strict';

  var ROOT_ID = 'sc-fips-edit-information';
  var FORM_ID = 'fips-information-edit-form';

  function initDfeModal(dialog) {
    if (window.DfeFrontend && typeof window.DfeFrontend.initModal === 'function') {
      window.DfeFrontend.initModal(dialog);
    }
  }

  function resolveEmbedUrl(href) {
    if (!href) return null;

    var hash = '';
    var pathAndQuery = href;
    var hashIdx = href.indexOf('#');
    if (hashIdx >= 0) {
      hash = href.slice(hashIdx);
      pathAndQuery = href.slice(0, hashIdx);
    }

    var url;
    if (/^https?:\/\//i.test(pathAndQuery)) {
      url = new URL(pathAndQuery);
    } else {
      var path = pathAndQuery;
      if (path.charAt(0) !== '/') {
        path = '/' + path.replace(/^\.\//, '');
      }
      url = new URL(path, window.location.origin);
    }

    url.searchParams.set('embed', '1');
    var fieldId = parseFieldId(hash || (href.indexOf('#') >= 0 ? href.slice(href.indexOf('#')) : ''));
    var fieldKey = fieldKeyFromElementId(fieldId);
    if (fieldKey) {
      url.searchParams.set('field', fieldKey);
    }
    if (hash) {
      url.hash = hash;
    }
    return url.toString();
  }

  function parseFieldId(hash) {
    if (!hash || hash.length < 2) return '';
    return hash.charAt(0) === '#' ? hash.slice(1) : hash;
  }

  function fieldKeyFromElementId(elementId) {
    if (!elementId || elementId.indexOf('edit-') !== 0) return '';
    return elementId.slice('edit-'.length);
  }

  function getAntiForgeryInput(form, doc) {
    return (
      form.querySelector('input[name="__RequestVerificationToken"]') ||
      doc.querySelector('input[name="__RequestVerificationToken"]')
    );
  }

  function escapeAttr(value) {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/</g, '&lt;');
  }

  function buildModalFormHtml(sourceForm, doc, fieldHtml, fieldKey) {
    var token = getAntiForgeryInput(sourceForm, doc);
    var action = sourceForm.getAttribute('action') || '';

    var parts = [
      '<form action="',
      escapeAttr(action),
      '" method="post" novalidate class="wd-overview-edit-modal__form govuk-!-margin-bottom-0">'
    ];

    if (token) {
      parts.push(token.outerHTML);
    }

    parts.push(
      '<input type="hidden" name="embed" value="1" />',
      fieldKey ? '<input type="hidden" name="field" value="' + escapeAttr(fieldKey) + '" />' : '',
      '<div class="wd-overview-edit-modal__field govuk-!-margin-bottom-4">',
      fieldHtml,
      '</div>',
      '<div class="govuk-button-group govuk-!-margin-bottom-0">',
      '<button type="submit" class="govuk-button" data-module="govuk-button">Save</button>',
      '<button type="button" class="govuk-link govuk-link--no-visited-state wd-overview-edit-modal__cancel" data-dfe-modal-close>Cancel</button>',
      '</div>',
      '</form>'
    );

    return parts.join('');
  }

  function extractFormHtml(html, fieldId) {
    var doc = new DOMParser().parseFromString(html, 'text/html');
    var root = doc.getElementById(ROOT_ID);
    var form = root ? root.querySelector('#' + FORM_ID) : doc.querySelector('#' + FORM_ID);
    if (!form) return null;

    var target = fieldId ? doc.getElementById(fieldId) : null;
    if (!target) return null;

    var fieldKey = fieldKeyFromElementId(fieldId);
    return { html: buildModalFormHtml(form, doc, target.outerHTML, fieldKey) };
  }

  function armForms(host) {
    host.querySelectorAll('form').forEach(function (f) {
      if (!f.querySelector('input[name="embed"][value="1"]')) {
        var h = document.createElement('input');
        h.type = 'hidden';
        h.name = 'embed';
        h.value = '1';
        f.appendChild(h);
      }
      f.setAttribute('target', '_top');
    });
  }

  function initGovukIn(host) {
    import('/modern/js/govuk-frontend-6.1.0.min.js')
      .then(function (govuk) {
        if (govuk && typeof govuk.initAll === 'function') {
          govuk.initAll(host);
        }
      })
      .catch(function () { /* optional */ });
    if (window.DfeFrontend && typeof window.DfeFrontend.initAll === 'function') {
      window.DfeFrontend.initAll(host);
    }
  }

  function setHostBusy(host, busy) {
    host.setAttribute('aria-busy', busy ? 'true' : 'false');
  }

  function showLoadError(host, href, message) {
    var safeHref = (href || '#').replace(/"/g, '&quot;');
    host.innerHTML =
      '<div class="govuk-!-padding-4">' +
      '<p class="govuk-body">' + (message || 'Could not load the edit form.') + '</p>' +
      '<p class="govuk-body govuk-!-margin-bottom-0">' +
      '<a class="govuk-link" href="' + safeHref + '">Open full edit page</a>' +
      '</p></div>';
    setHostBusy(host, false);
  }

  function loadIntoHost(host, href, hashFromLink) {
    var embedUrl = resolveEmbedUrl(href);
    if (!embedUrl) {
      showLoadError(host, href, 'Invalid edit link.');
      return;
    }

    var hash = hashFromLink || '';
    if (!hash && embedUrl.indexOf('#') >= 0) {
      hash = embedUrl.slice(embedUrl.indexOf('#'));
    }
    var fieldId = parseFieldId(hash);

    setHostBusy(host, true);
    host.innerHTML = '<div class="govuk-!-padding-4"><p class="govuk-body govuk-!-margin-bottom-0">Loading…</p></div>';

    fetch(embedUrl, { credentials: 'same-origin', headers: { Accept: 'text/html' } })
      .then(function (res) {
        if (!res.ok) throw new Error('HTTP ' + res.status);
        return res.text();
      })
      .then(function (html) {
        var extracted = extractFormHtml(html, fieldId);
        if (!extracted || !extracted.html) throw new Error('No field found');
        host.innerHTML = extracted.html;
        armForms(host);
        initGovukIn(host);
        setHostBusy(host, false);
      })
      .catch(function () {
        showLoadError(host, href.replace(/\?embed=1(&|$)/, '$1').replace(/#.*$/, '') + hash, 'Could not load this field.');
      });
  }

  function openModal(dialog, host, titleEl, href, titleText) {
    if (titleEl) {
      titleEl.textContent = titleText && String(titleText).trim() !== '' ? String(titleText).trim() : 'Edit';
    }

    if (typeof dialog.showModal === 'function') {
      dialog.showModal();
    } else {
      dialog.setAttribute('open', '');
    }

    var hash = href && href.indexOf('#') >= 0 ? href.slice(href.indexOf('#')) : '';
    loadIntoHost(host, href, hash);
  }

  function wireClose(dialog) {
    dialog.querySelectorAll('[data-dfe-modal-close]').forEach(function (btn) {
      if (btn.dataset.fipsOverviewCloseWired === '1') return;
      btn.dataset.fipsOverviewCloseWired = '1';
      btn.addEventListener('click', function () {
        if (typeof dialog.close === 'function') dialog.close();
      });
    });

    dialog.addEventListener('close', function () {
      var host = document.getElementById('fips-information-edit-modal-host');
      if (host) {
        host.innerHTML = '';
        setHostBusy(host, false);
      }
    });

    initDfeModal(dialog);
  }

  function init() {
    var dialog = document.getElementById('fips-information-edit-modal');
    var host = document.getElementById('fips-information-edit-modal-host');
    var titleEl = document.getElementById('fips-information-edit-modal-title');
    if (!dialog || !host) return;

    wireClose(dialog);

    document.addEventListener('click', function (e) {
      var a = e.target.closest('a.fips-detail-edit-modal-open');
      if (!a) return;
      var href = a.getAttribute('href');
      if (!href || href.startsWith('#') || href.startsWith('javascript:')) return;
      e.preventDefault();
      e.stopPropagation();
      openModal(dialog, host, titleEl, href, a.getAttribute('data-fips-modal-title'));
    }, false);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
