/**
 * Work item detail — open summary-list “Change” links in a modal with a single-field edit form.
 * Loads via fetch (same-origin); extracts only the target field from the embed edit page (?embed=1).
 */
(function () {
  'use strict';

  var ROOT_IDS = [
    'sc-work-edit',
    'sc-work-edittags',
    'sc-work-editstrategicalignment',
    'sc-work-editmultideptcooperation',
    'sc-work-setprimarycontact',
    'sc-work-setbudgetowner',
    'sc-work-addcontact'
  ];

  /** Field ids that include extra sibling controls in the same modal. */
  var FIELD_EXTRA_SELECTORS = {
    'edit-priority': '#PriorityChangeReason'
  };

  /** Pages where the whole form is already a single-purpose editor (no hash required). */
  var FULL_FORM_FIELD_IDS = {
    'sc-work-edittags': 'edit-tags',
    'sc-work-editmultideptcooperation': 'multi-dept-form',
    'sc-work-setprimarycontact': 'edit-primary-contact',
    'sc-work-setbudgetowner': 'edit-budget-owner',
    'sc-work-addcontact': 'edit-add-contact'
  };

  function initDfeModal(dialog) {
    if (window.DfeFrontend && typeof window.DfeFrontend.initModal === 'function') {
      window.DfeFrontend.initModal(dialog);
    }
  }

  function normalizeWorkEditPath(pathname) {
    var legacy = pathname.match(/^\/ModernWork\/Edit\/(\d+)\/?$/i);
    if (legacy) return '/modern/work/' + legacy[1] + '/edit';

    var wrongOrder = pathname.match(/^\/modern\/work\/edit\/(\d+)\/?$/i);
    if (wrongOrder) return '/modern/work/' + wrongOrder[1] + '/edit';

    var legacyTags = pathname.match(/^\/ModernWork\/EditWorkTags\/(\d+)\/?$/i);
    if (legacyTags) return '/modern/work/' + legacyTags[1] + '/tags/edit';

    var legacyStrategic = pathname.match(/^\/ModernWork\/EditStrategicAlignment\/(\d+)\/?$/i);
    if (legacyStrategic) return '/modern/work/' + legacyStrategic[1] + '/strategic-alignment/edit';

    var legacyMulti = pathname.match(/^\/ModernWork\/EditMultiDeptCooperation\/(\d+)\/?$/i);
    if (legacyMulti) return '/modern/work/' + legacyMulti[1] + '/multi-dept/edit';

    var legacyPeople = pathname.match(/^\/ModernWork\/EditPeople\/(\d+)\/?$/i);
    if (legacyPeople) return '/modern/work/' + legacyPeople[1] + '/people/edit';

    var wrongPeople = pathname.match(/^\/modern\/work\/people\/edit\/(\d+)\/?$/i);
    if (wrongPeople) return '/modern/work/' + wrongPeople[1] + '/people/edit';

    var legacyPrimary = pathname.match(/^\/ModernWork\/SetPrimaryContact\/(\d+)\/?$/i);
    if (legacyPrimary) return '/modern/work/' + legacyPrimary[1] + '/primary-contact/set';

    var legacyBudget = pathname.match(/^\/ModernWork\/SetBudgetOwner\/(\d+)\/?$/i);
    if (legacyBudget) return '/modern/work/' + legacyBudget[1] + '/budget-owner/set';

    var legacyAddContact = pathname.match(/^\/ModernWork\/AddContact\/(\d+)\/?$/i);
    if (legacyAddContact) return '/modern/work/' + legacyAddContact[1] + '/contact/add';

    return pathname;
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

    url.pathname = normalizeWorkEditPath(url.pathname);
    url.searchParams.set('embed', '1');
    if (hash) {
      url.hash = hash;
    }
    return url.toString();
  }

  function parseFieldId(hash) {
    if (!hash || hash.length < 2) return '';
    return hash.charAt(0) === '#' ? hash.slice(1) : hash;
  }

  function findSourceForm(doc) {
    var i;
    var root;
    var form;

    for (i = 0; i < ROOT_IDS.length; i++) {
      root = doc.getElementById(ROOT_IDS[i]);
      if (!root) continue;
      form = root.querySelector('form');
      if (form) return form;
    }

    return doc.querySelector('main form');
  }

  function findRootId(doc) {
    var i;
    for (i = 0; i < ROOT_IDS.length; i++) {
      if (doc.getElementById(ROOT_IDS[i])) return ROOT_IDS[i];
    }
    return '';
  }

  function getAntiForgeryInput(form, doc) {
    return (
      form.querySelector('input[name="__RequestVerificationToken"]') ||
      doc.querySelector('input[name="__RequestVerificationToken"]')
    );
  }

  function collectFieldHtml(doc, fieldId) {
    var rootId = findRootId(doc);
    var defaultFieldId = FULL_FORM_FIELD_IDS[rootId] || '';
    var targetId = fieldId || defaultFieldId;

    if (!targetId) return null;

    if (
      targetId === 'edit-add-contact' &&
      doc.getElementById('edit-contact-user') &&
      doc.querySelector('input[name="ContactRoleTypeId"][type="hidden"]')
    ) {
      targetId = 'edit-contact-user';
    }

    var target = doc.getElementById(targetId);
    if (!target) return null;

    var html =
      target.tagName && target.tagName.toLowerCase() === 'form'
        ? target.innerHTML
        : target.outerHTML;
    var extraSelector = FIELD_EXTRA_SELECTORS[targetId];
    if (extraSelector) {
      var extra = doc.querySelector(extraSelector);
      if (extra) {
        var extraGroup = extra.closest('.govuk-form-group, .dfe-c-form__group, fieldset');
        if (extraGroup && extraGroup.id !== targetId) {
          html += extraGroup.outerHTML;
        }
      }
    }

    return html;
  }

  function escapeAttr(value) {
    return String(value)
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/</g, '&lt;');
  }

  function buildModalFormHtml(sourceForm, doc, fieldHtml) {
    var token = getAntiForgeryInput(sourceForm, doc);
    var action = sourceForm.getAttribute('action') || '';
    var method = (sourceForm.getAttribute('method') || 'post').toLowerCase();

    var parts = [
      '<form action="',
      escapeAttr(action),
      '" method="',
      escapeAttr(method),
      '" novalidate class="wd-overview-edit-modal__form govuk-!-margin-bottom-0">'
    ];

    if (token) {
      parts.push(token.outerHTML);
    }

    parts.push(
      '<input type="hidden" name="embed" value="1" />',
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
    var form = findSourceForm(doc);
    if (!form) return null;

    var targetId = fieldId || FULL_FORM_FIELD_IDS[findRootId(doc)] || '';
    if (targetId) {
      var targetEl = doc.getElementById(targetId);
      if (targetEl && targetEl.tagName && targetEl.tagName.toLowerCase() === 'form') {
        form = targetEl;
      }
    }

    var fieldHtml = collectFieldHtml(doc, fieldId);
    if (!fieldHtml) return null;

    return { html: buildModalFormHtml(form, doc, fieldHtml) };
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
      .catch(function () {
        /* govuk optional */
      });
    if (window.DfeFrontend && typeof window.DfeFrontend.initAll === 'function') {
      window.DfeFrontend.initAll(host);
    }

    if (host.querySelector('input[name="SameAsSro"]')) {
      initBudgetOwnerToggle(host);
    }

    if (host.querySelector('.user-picker[data-field-name="AppUserId"]')) {
      /* user-picker.js is loaded globally on modern layout */
    }
  }

  function runEmbeddedPageScripts(sourceDoc) {
    sourceDoc.querySelectorAll('body script:not([src])').forEach(function (oldScript) {
      var code = (oldScript.textContent || '').trim();
      if (!code || code.indexOf('gov-dept') < 0) return;
      try {
        var run = new Function(code);
        run();
      } catch (e) {
        /* page scripts are best-effort in modal embed */
      }
    });
  }

  function initBudgetOwnerToggle(host) {
    var radios = host.querySelectorAll('input[name="SameAsSro"]');
    var picker = host.querySelector('#budget-owner-picker');
    if (!radios.length || !picker) return;

    function toggle() {
      var checked = host.querySelector('input[name="SameAsSro"]:checked');
      picker.style.display = checked && checked.value === 'false' ? 'block' : 'none';
    }

    radios.forEach(function (r) {
      r.addEventListener('change', toggle);
    });
    toggle();
  }

  function setHostBusy(host, busy) {
    host.setAttribute('aria-busy', busy ? 'true' : 'false');
  }

  function showLoadError(host, href, message) {
    var safeHref = href.replace(/"/g, '&quot;');
    host.innerHTML =
      '<div class="govuk-!-padding-4">' +
      '<p class="govuk-body">' +
      (message || 'Could not load the edit form.') +
      '</p>' +
      '<p class="govuk-body govuk-!-margin-bottom-0">' +
      '<a class="govuk-link" href="' +
      safeHref +
      '">Open full edit page</a>' +
      '</p>' +
      '</div>';
    setHostBusy(host, false);
  }

  function loadIntoHost(host, href, hashFromLink) {
    var embedUrl = resolveEmbedUrl(href);
    if (!embedUrl) {
      showLoadError(host, href || '#', 'Invalid edit link.');
      return;
    }

    var hash = hashFromLink || '';
    if (!hash && embedUrl.indexOf('#') >= 0) {
      hash = embedUrl.slice(embedUrl.indexOf('#'));
    }
    var fieldId = parseFieldId(hash);

    setHostBusy(host, true);
    host.innerHTML =
      '<div class="govuk-!-padding-4"><p class="govuk-body govuk-!-margin-bottom-0">Loading…</p></div>';

    fetch(embedUrl, {
      credentials: 'same-origin',
      headers: { Accept: 'text/html' }
    })
      .then(function (res) {
        if (!res.ok) {
          throw new Error('HTTP ' + res.status);
        }
        return res.text();
      })
      .then(function (html) {
        var doc = new DOMParser().parseFromString(html, 'text/html');
        var extracted = extractFormHtml(html, fieldId);
        if (!extracted || !extracted.html) {
          throw new Error('No field found');
        }
        host.innerHTML = extracted.html;
        armForms(host);
        initGovukIn(host);
        runEmbeddedPageScripts(doc);
        setHostBusy(host, false);
      })
      .catch(function () {
        showLoadError(host, href, 'Could not load this field.');
      });
  }

  function openModal(dialog, host, titleEl, href, titleText) {
    var t = titleText && String(titleText).trim() !== '' ? String(titleText).trim() : 'Edit';
    if (titleEl) titleEl.textContent = t;

    if (typeof dialog.showModal === 'function') {
      dialog.showModal();
    } else {
      dialog.setAttribute('open', '');
    }

    var hash = '';
    if (href && href.indexOf('#') >= 0) {
      hash = href.slice(href.indexOf('#'));
    }
    loadIntoHost(host, href, hash);
  }

  function init() {
    var dialog = document.getElementById('wd-overview-edit-modal');
    var host = document.getElementById('wd-overview-edit-modal-host');
    var titleEl = document.getElementById('wd-overview-edit-modal-title');
    if (!dialog || !host) return;

    wireClose(dialog);

    document.addEventListener(
      'click',
      function (e) {
        var a = e.target.closest('a.wd-detail-edit-modal-open');
        if (!a) return;
        var href = a.getAttribute('href');
        if (!href || href.startsWith('#') || href.startsWith('javascript:')) return;
        e.preventDefault();
        e.stopPropagation();
        openModal(dialog, host, titleEl, href, a.getAttribute('data-wd-modal-title'));
      },
      false
    );
  }

  function wireClose(dialog) {
    dialog.querySelectorAll('[data-dfe-modal-close]').forEach(function (btn) {
      if (btn.dataset.wdOverviewCloseWired === '1') return;
      btn.dataset.wdOverviewCloseWired = '1';
      btn.addEventListener('click', function () {
        if (typeof dialog.close === 'function') {
          dialog.close();
        }
      });
    });

    dialog.addEventListener('close', function () {
      var host = document.getElementById('wd-overview-edit-modal-host');
      if (host) {
        host.innerHTML = '';
        setHostBusy(host, false);
      }
    });

    initDfeModal(dialog);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
