/**
 * RAID monthly review — business area localStorage and expandable work list.
 */
(function () {
  'use strict';

  var api = (window.compassRaidReview = window.compassRaidReview || {});

  function storageKey() {
    return api.storageKey || 'compass.raidReview.businessAreaIds';
  }

  function saveBusinessAreaIds(ids) {
    try {
      localStorage.setItem(storageKey(), JSON.stringify(ids));
    } catch (e) { /* ignore */ }
  }

  api.initSetupForm = function () {
    var form = document.getElementById('raid-review-setup-form');
    if (!form) return;
    form.addEventListener('submit', function (ev) {
      ev.preventDefault();
      var boxes = form.querySelectorAll('input[name="businessAreaIds"]:checked');
      var ids = [];
      boxes.forEach(function (b) {
        var n = parseInt(b.value, 10);
        if (n > 0) ids.push(n);
      });
      var err = document.getElementById('raid-review-setup-error');
      if (!ids.length) {
        if (err) err.hidden = false;
        return;
      }
      if (err) err.hidden = true;
      saveBusinessAreaIds(ids);
      window.location.href = '/modern/raid/review/overview?ba=' + encodeURIComponent(ids.join(','));
    });
  };

  api.initWorkList = function (config) {
    var page = document.querySelector('.raid-review-work');
    if (!page) return;

    page.addEventListener('click', function (ev) {
      var toggle = ev.target.closest('[data-raid-review-toggle]');
      if (!toggle) return;
      var row = toggle.closest('[data-raid-review-row]');
      if (!row) return;
      var expanded = row.getAttribute('aria-expanded') === 'true';
      var nextExpanded = !expanded;
      row.setAttribute('aria-expanded', nextExpanded ? 'true' : 'false');
      toggle.setAttribute('aria-expanded', nextExpanded ? 'true' : 'false');
      var panel = row.querySelector('[data-raid-review-panel]');
      if (panel) panel.hidden = !nextExpanded;
      if (nextExpanded) loadTimeline(row, config);
    });
  };

  api.initCloseModal = function () {
    var dialog = document.getElementById('raid-review-close-modal');
    var titleEl = document.getElementById('raid-review-close-modal-title');
    var ledeEl = document.getElementById('raid-review-close-modal-lede');
    var commentEl = document.getElementById('raid-review-close-comment');
    var errorEl = document.getElementById('raid-review-close-comment-error');
    var commentGroup = document.getElementById('raid-review-close-comment-group');
    var confirmBtn = document.getElementById('raid-review-close-confirm');
    if (!dialog || !titleEl || !commentEl || !confirmBtn) return;

    /** @type {HTMLFormElement | null} */
    var pendingForm = null;
    var pendingLabel = 'Close record';

    function initModalComponents() {
      if (typeof window.DfeFrontend !== 'undefined' && typeof window.DfeFrontend.initAll === 'function') {
        window.DfeFrontend.initAll();
      }
    }

    function showModal() {
      commentEl.value = '';
      if (errorEl) errorEl.hidden = true;
      if (commentGroup) commentGroup.classList.remove('govuk-form-group--error');
      commentEl.classList.remove('govuk-textarea--error');
      if (typeof dialog.showModal === 'function') {
        dialog.showModal();
      } else {
        dialog.setAttribute('open', '');
      }
      initModalComponents();
      commentEl.focus();
    }

    function hideModal() {
      if (typeof dialog.close === 'function') {
        dialog.close();
      } else {
        dialog.removeAttribute('open');
      }
      pendingForm = null;
    }

    dialog.addEventListener('close', function () {
      pendingForm = null;
    });

    document.addEventListener('click', function (ev) {
      var openBtn = ev.target.closest('[data-raid-review-close]');
      if (openBtn) {
        var formId = openBtn.getAttribute('data-close-form');
        if (!formId) return;
        pendingForm = document.getElementById(formId);
        if (!pendingForm) return;
        var ref = openBtn.getAttribute('data-close-ref') || 'this record';
        pendingLabel = openBtn.getAttribute('data-close-label') || 'Close record';
        titleEl.textContent = pendingLabel + '?';
        if (ledeEl) {
          ledeEl.textContent =
            'You are closing ' + ref + '. This cannot be undone from monthly review. Enter a closure comment to confirm.';
        }
        confirmBtn.textContent = 'Confirm ' + pendingLabel.toLowerCase();
        showModal();
        return;
      }

      if (ev.target.closest('[data-raid-review-close-cancel]')) {
        hideModal();
      }
    });

    confirmBtn.addEventListener('click', function () {
      var comment = (commentEl.value || '').trim();
      if (!comment) {
        if (errorEl) errorEl.hidden = false;
        if (commentGroup) commentGroup.classList.add('govuk-form-group--error');
        commentEl.classList.add('govuk-textarea--error');
        commentEl.focus();
        return;
      }
      if (!pendingForm) {
        hideModal();
        return;
      }
      var field = pendingForm.querySelector('[data-raid-review-close-comment-field]');
      if (field) field.value = comment;
      var form = pendingForm;
      hideModal();
      form.submit();
    });
  };

  function loadTimeline(row, config) {
    var panel = row.querySelector('[data-raid-review-panel]');
    if (!panel || panel.getAttribute('data-timeline-loaded') === '1') return;
    var kind = row.getAttribute('data-kind');
    var id = row.getAttribute('data-id');
    var host = panel.querySelector('[data-raid-review-timeline]');
    if (!host || !kind || !id) return;
    host.textContent = 'Loading last month\u2019s review\u2026';
    fetch('/modern/raid/review/work/item/' + encodeURIComponent(kind) + '/' + id + '/timeline', {
      headers: { Accept: 'text/html' }
    })
      .then(function (r) { return r.text(); })
      .then(function (html) {
        host.innerHTML = html;
        panel.setAttribute('data-timeline-loaded', '1');
      })
      .catch(function () {
        host.textContent = 'Could not load update history.';
      });
  }
})();
