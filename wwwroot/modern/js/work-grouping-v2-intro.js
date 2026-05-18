/**
 * One-time “how to use” modal for Work V2 directorates / business areas (localStorage dismiss).
 */
(function () {
  'use strict';

  function initDfeModal() {
    if (typeof window.DfeFrontend !== 'undefined' && typeof window.DfeFrontend.initAll === 'function') {
      window.DfeFrontend.initAll();
    }
  }

  function persistDismissed(key) {
    try {
      localStorage.setItem(key, '1');
    } catch (_e) {
      /* private mode / blocked */
    }
  }

  function isDismissed(key) {
    try {
      return localStorage.getItem(key) === '1';
    } catch (_e) {
      return false;
    }
  }

  function wireDialog(dialog) {
    if (!(dialog instanceof HTMLDialogElement)) return;
    var key = dialog.getAttribute('data-storage-key');
    if (!key) return;

    function dismiss() {
      if (dialog.open) dialog.close();
    }

    dialog.querySelectorAll('[data-work-v2-intro-dismiss]').forEach(function (btn) {
      btn.addEventListener('click', dismiss);
    });

    dialog.addEventListener('close', function () {
      persistDismissed(key);
    });
  }

  function tryOpen(dialog) {
    if (!(dialog instanceof HTMLDialogElement)) return;
    var key = dialog.getAttribute('data-storage-key');
    if (!key || isDismissed(key)) return;
    if (typeof dialog.showModal !== 'function') return;
    if (dialog.open) return;
    dialog.showModal();
    initDfeModal();
    var primary = dialog.querySelector('[data-work-v2-intro-dismiss].govuk-button');
    if (primary && typeof primary.focus === 'function') primary.focus();
  }

  function init() {
    document.querySelectorAll('dialog[data-work-v2-intro]').forEach(function (el) {
      wireDialog(el);
      tryOpen(el);
    });
  }

  window.addEventListener('load', init);
})();
