/**
 * Sub-navigation data access: download and API icon toolbar with toggle panels.
 */
(function () {
  function getPanel(wrap, panelType) {
    var bar = wrap.closest('.dfe-f-sub-navigation__bar');
    if (!bar) return null;
    var panelsRoot = bar.querySelector('.subnav-data-access-panels');
    if (!panelsRoot) return null;
    return panelsRoot.querySelector('[data-subnav-panel="' + panelType + '"]');
  }

  function closePanel(wrap, panelType) {
    var btn = wrap.querySelector('[data-subnav-panel="' + panelType + '"]');
    var panel = getPanel(wrap, panelType);
    if (btn) {
      btn.setAttribute('aria-expanded', 'false');
      btn.classList.remove('subnav-data-access__trigger--active');
    }
    if (panel) panel.hidden = true;
  }

  function closeAll(wrap) {
    wrap.querySelectorAll('.subnav-data-access__trigger').forEach(function (btn) {
      closePanel(wrap, btn.getAttribute('data-subnav-panel'));
    });
    wrap.classList.remove('subnav-data-access--open');
  }

  function openPanel(wrap, panelType) {
    document.querySelectorAll('[data-module="subnav-data-access"].subnav-data-access--open').forEach(function (other) {
      if (other !== wrap) closeAll(other);
    });

    wrap.querySelectorAll('.subnav-data-access__trigger').forEach(function (btn) {
      var type = btn.getAttribute('data-subnav-panel');
      if (type === panelType) {
        btn.setAttribute('aria-expanded', 'true');
        btn.classList.add('subnav-data-access__trigger--active');
      } else {
        btn.setAttribute('aria-expanded', 'false');
        btn.classList.remove('subnav-data-access__trigger--active');
        var otherPanel = getPanel(wrap, type);
        if (otherPanel) otherPanel.hidden = true;
      }
    });

    var panel = getPanel(wrap, panelType);
    if (panel) panel.hidden = false;
    wrap.classList.add('subnav-data-access--open');
  }

  function initWrap(wrap) {
    wrap.querySelectorAll('.subnav-data-access__trigger').forEach(function (btn) {
      btn.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var panelType = btn.getAttribute('data-subnav-panel');
        var isOpen = btn.getAttribute('aria-expanded') === 'true';
        if (isOpen) closeAll(wrap);
        else openPanel(wrap, panelType);
      });
    });

    wrap.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') {
        closeAll(wrap);
        var active = wrap.querySelector('.subnav-data-access__trigger--active');
        if (active) active.focus();
      }
    });
  }

  function init() {
    document.querySelectorAll('[data-module="subnav-data-access"]').forEach(initWrap);

    document.addEventListener('click', function (e) {
      var t = e.target;
      if (t && t.closest && t.closest('[data-module="subnav-data-access"]')) return;
      if (t && t.closest && t.closest('.subnav-data-access-panels')) return;
      document.querySelectorAll('[data-module="subnav-data-access"].subnav-data-access--open').forEach(closeAll);
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
