/**
 * One-time guide modal for RAID register spreadsheet (cookie dismiss).
 */
(function () {
  'use strict';

  var COOKIE_DAYS = 365;

  function initDfeModal() {
    if (typeof window.DfeFrontend !== 'undefined' && typeof window.DfeFrontend.initAll === 'function') {
      window.DfeFrontend.initAll();
    }
  }

  function setCookie(name, value, days) {
    var expires = '';
    if (days) {
      var d = new Date();
      d.setTime(d.getTime() + days * 24 * 60 * 60 * 1000);
      expires = '; expires=' + d.toUTCString();
    }
    document.cookie = name + '=' + encodeURIComponent(value) + expires + '; path=/; SameSite=Lax';
  }

  function getCookie(name) {
    var escaped = name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
    var match = document.cookie.match(new RegExp('(?:^|; )' + escaped + '=([^;]*)'));
    return match ? decodeURIComponent(match[1]) : null;
  }

  function persistDismissed(key) {
    setCookie(key, '1', COOKIE_DAYS);
    try {
      localStorage.removeItem(key);
    } catch (_e) { /* blocked */ }
  }

  function isDismissed(key) {
    if (getCookie(key) === '1') return true;
    try {
      if (localStorage.getItem(key) === '1') {
        persistDismissed(key);
        return true;
      }
    } catch (_e) { /* blocked */ }
    return false;
  }

  function wireDialog(dialog) {
    if (!(dialog instanceof HTMLDialogElement)) return;
    var key = dialog.getAttribute('data-storage-key');
    if (!key) return;

    function dismiss() {
      if (dialog.open) dialog.close();
    }

    dialog.querySelectorAll('[data-raid-ss-intro-dismiss]').forEach(function (btn) {
      btn.addEventListener('click', dismiss);
    });

    dialog.addEventListener('close', function () {
      persistDismissed(key);
    });
  }

  function openDialog(dialog) {
    if (!(dialog instanceof HTMLDialogElement)) return;
    if (typeof dialog.showModal !== 'function') return;
    if (dialog.open) return;
    dialog.showModal();
    initDfeModal();
    var primary = dialog.querySelector('[data-raid-ss-intro-dismiss].govuk-button');
    if (primary && typeof primary.focus === 'function') primary.focus();
  }

  function tryOpenOnManageView() {
    var dialog = document.getElementById('raid-spreadsheet-intro');
    if (!dialog) return;
    var key = dialog.getAttribute('data-storage-key');
    if (!key || isDismissed(key)) return;

    var manage = document.getElementById('view-manage');
    if (!manage || !manage.classList.contains('raid-ss-tab-active')) return;

    openDialog(dialog);
  }

  function init() {
    var dialog = document.getElementById('raid-spreadsheet-intro');
    if (!dialog) return;

    wireDialog(dialog);

    document.querySelectorAll('.raid-ss-show-guide-btn').forEach(function (btn) {
      btn.addEventListener('click', function () {
        openDialog(dialog);
      });
    });

    tryOpenOnManageView();

    document.querySelectorAll('#register-view-tabs a[data-view="manage"]').forEach(function (link) {
      link.addEventListener('click', function () {
        setTimeout(tryOpenOnManageView, 100);
      });
    });

    var hash = window.location.hash.replace('#', '');
    if (hash === 'view-manage' || hash.indexOf('ss-') === 0) {
      setTimeout(tryOpenOnManageView, 200);
    }
  }

  window.RaidSpreadsheetIntro = {
    open: function () {
      var dialog = document.getElementById('raid-spreadsheet-intro');
      if (dialog) openDialog(dialog);
    },
    tryAutoOpen: tryOpenOnManageView
  };

  window.addEventListener('load', init);
})();
