/**
 * Confirm modal for destructive actions (delete, suspend, etc.).
 * Add data-confirm-title and data-confirm-message to any form;
 * on submit the modal is shown and the form is only submitted after "Confirm".
 */
(function () {
  function init() {
    var overlay = document.getElementById('confirm-modal-overlay');
    var dialog = document.getElementById('confirm-modal');
    var titleEl = document.getElementById('confirm-modal-title');
    var descEl = document.getElementById('confirm-modal-description');
    var cancelBtn = document.getElementById('confirm-modal-cancel');
    var confirmBtn = document.getElementById('confirm-modal-confirm');

    if (!overlay || !dialog || !titleEl || !descEl || !cancelBtn || !confirmBtn) return;

    var pendingForm = null;

    function show(title, message) {
      titleEl.textContent = title || 'Confirm';
      descEl.textContent = message || 'Are you sure?';
      overlay.style.display = 'flex';
      overlay.setAttribute('aria-hidden', 'false');
      dialog.style.display = 'block';
      cancelBtn.focus();
    }

    function hide() {
      overlay.style.display = 'none';
      overlay.setAttribute('aria-hidden', 'true');
      dialog.style.display = 'none';
      pendingForm = null;
    }

    function doConfirm() {
      if (pendingForm && typeof pendingForm.submit === 'function') {
        var form = pendingForm;
        pendingForm = null;
        hide();
        form.submit();
      } else {
        hide();
      }
    }

    cancelBtn.addEventListener('click', function () {
      pendingForm = null;
      hide();
    });

    confirmBtn.addEventListener('click', doConfirm);

    overlay.addEventListener('click', function (e) {
      if (e.target === overlay) {
        pendingForm = null;
        hide();
      }
    });

    document.addEventListener('keydown', function (e) {
      if (dialog.style.display !== 'block') return;
      if (e.key === 'Escape') {
        pendingForm = null;
        hide();
      }
    });

    document.addEventListener('submit', function (e) {
      var form = e.target;
      if (form.getAttribute('data-confirm-title')) {
        e.preventDefault();
        var msg = form.getAttribute('data-confirm-message') || 'Are you sure?';
        pendingForm = form;
        show(form.getAttribute('data-confirm-title'), msg);
      }
    }, true);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
