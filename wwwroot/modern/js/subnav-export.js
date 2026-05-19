/**
 * Sub-navigation Export dropdown: button trigger + menu (works with generating-report-modal.js).
 */
(function () {
  function closeMenu(wrap) {
    var btn = wrap.querySelector('.subnav-export__trigger');
    var menu = wrap.querySelector('.subnav-export__menu');
    if (!btn || !menu) return;
    menu.hidden = true;
    btn.setAttribute('aria-expanded', 'false');
    wrap.classList.remove('subnav-export--open');
  }

  function openMenu(wrap) {
    var btn = wrap.querySelector('.subnav-export__trigger');
    var menu = wrap.querySelector('.subnav-export__menu');
    if (!btn || !menu) return;
    document.querySelectorAll('[data-module="subnav-export"].subnav-export--open').forEach(function (other) {
      if (other !== wrap) closeMenu(other);
    });
    menu.hidden = false;
    btn.setAttribute('aria-expanded', 'true');
    wrap.classList.add('subnav-export--open');
  }

  function initWrap(wrap) {
    var btn = wrap.querySelector('.subnav-export__trigger');
    var menu = wrap.querySelector('.subnav-export__menu');
    if (!btn || !menu) return;

    btn.addEventListener('click', function (e) {
      e.preventDefault();
      e.stopPropagation();
      var isOpen = wrap.classList.contains('subnav-export--open');
      if (isOpen) closeMenu(wrap);
      else openMenu(wrap);
    });

    menu.addEventListener('click', function (e) {
      if (e.target.closest('a[href]')) {
        closeMenu(wrap);
      }
    });

    menu.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') {
        closeMenu(wrap);
        btn.focus();
      }
    });
  }

  function init() {
    document.querySelectorAll('[data-module="subnav-export"]').forEach(initWrap);

    document.addEventListener('click', function (e) {
      var t = e.target;
      if (t && t.closest && t.closest('[data-module="subnav-export"]')) return;
      document.querySelectorAll('[data-module="subnav-export"].subnav-export--open').forEach(closeMenu);
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
