/**
 * Confirm modal (DfE native dialog element). Forms: data-confirm-title + data-confirm-message on submit.
 * Programmatic: window.showConfirmModal(title, message, onConfirm).
 */
(function () {
  function init() {
    var dialog = document.getElementById('confirm-modal');
    var titleEl = document.getElementById('confirm-modal-title');
    var descEl = document.getElementById('confirm-modal-message');
    var cancelBtn = document.getElementById('confirm-modal-cancel');
    var confirmBtn = document.getElementById('confirm-modal-confirm');

    if (!dialog || !titleEl || !descEl || !cancelBtn || !confirmBtn) return;

    var pendingForm = null;
    /** @type {null | (() => void)} */
    var pendingCallback = null;

    function initModalComponents() {
      if (typeof window.DfeFrontend !== 'undefined' && typeof window.DfeFrontend.initAll === 'function') {
        window.DfeFrontend.initAll();
      }
    }

    function show(title, message) {
      titleEl.textContent = title || 'Confirm';
      descEl.textContent = message || 'Are you sure?';
      if (typeof dialog.showModal === 'function') {
        dialog.showModal();
      } else {
        dialog.setAttribute('open', '');
      }
      initModalComponents();
    }

    function hide() {
      if (typeof dialog.close === 'function') {
        dialog.close();
      } else {
        dialog.removeAttribute('open');
        pendingForm = null;
        pendingCallback = null;
      }
    }

    dialog.addEventListener('close', function () {
      pendingForm = null;
      pendingCallback = null;
    });

    function doConfirm() {
      if (pendingCallback) {
        var cb = pendingCallback;
        hide();
        try {
          cb();
        } catch (e) {}
        return;
      }
      if (pendingForm && typeof pendingForm.submit === 'function') {
        var form = pendingForm;
        hide();
        form.submit();
        return;
      }
      hide();
    }

    cancelBtn.addEventListener('click', function () {
      hide();
    });

    confirmBtn.addEventListener('click', doConfirm);

    /**
     * @param {string} title
     * @param {string} message
     * @param {() => void} onConfirm
     */
    window.showConfirmModal = function (title, message, onConfirm) {
      pendingCallback = typeof onConfirm === 'function' ? onConfirm : null;
      pendingForm = null;
      show(title || 'Confirm', message || 'Are you sure?');
    };

    document.addEventListener(
      'submit',
      function (e) {
        var form = e.target;
        if (form.getAttribute('data-confirm-title')) {
          e.preventDefault();
          var msg = form.getAttribute('data-confirm-message') || 'Are you sure?';
          pendingForm = form;
          pendingCallback = null;
          show(form.getAttribute('data-confirm-title'), msg);
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
