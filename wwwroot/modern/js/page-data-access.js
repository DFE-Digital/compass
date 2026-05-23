/**
 * Page data access: Get data / Download popovers in sub-navigation.
 */
(function () {
  function closeMenu(menuWrap) {
    var btn = menuWrap.querySelector('.page-data-access__btn');
    var popover = menuWrap.querySelector('.page-data-access__popover');
    if (!btn || !popover) return;
    popover.hidden = true;
    btn.setAttribute('aria-expanded', 'false');
    menuWrap.classList.remove('page-data-access__menu--open');
  }

  function closeAllInRoot(root) {
    root.querySelectorAll('.page-data-access__menu--open').forEach(closeMenu);
  }

  function openMenu(menuWrap, root) {
    closeAllInRoot(root);
    var btn = menuWrap.querySelector('.page-data-access__btn');
    var popover = menuWrap.querySelector('.page-data-access__popover');
    if (!btn || !popover) return;
    popover.hidden = false;
    btn.setAttribute('aria-expanded', 'true');
    menuWrap.classList.add('page-data-access__menu--open');
  }

  function initRoot(root) {
    root.querySelectorAll('.page-data-access__menu').forEach(function (menuWrap) {
      var btn = menuWrap.querySelector('.page-data-access__btn');
      if (!btn) return;

      btn.addEventListener('click', function (e) {
        e.preventDefault();
        e.stopPropagation();
        var isOpen = menuWrap.classList.contains('page-data-access__menu--open');
        if (isOpen) closeMenu(menuWrap);
        else openMenu(menuWrap, root);
      });

      menuWrap.querySelectorAll('[data-copy-target]').forEach(function (copyBtn) {
        copyBtn.addEventListener('click', function (e) {
          e.preventDefault();
          var bar = copyBtn.closest('.page-data-access__endpoint-bar');
          var input = bar && bar.querySelector('.page-data-access__endpoint-url');
          if (!input) return;
          navigator.clipboard.writeText(input.value).then(function () {
            var label = copyBtn.textContent.trim();
            copyBtn.textContent = 'Copied';
            setTimeout(function () {
              copyBtn.innerHTML = '<span class="material-symbols-sharp" aria-hidden="true">content_copy</span> Copy';
            }, 1500);
          });
        });
      });

      menuWrap.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
          closeMenu(menuWrap);
          btn.focus();
        }
      });
    });

    root.addEventListener('click', function (e) {
      if (e.target.closest('.page-data-access__download-link[href]')) {
        closeAllInRoot(root);
      }
    });
  }

  function init() {
    document.querySelectorAll('[data-module="page-data-access"]').forEach(initRoot);

    document.addEventListener('click', function (e) {
      if (e.target && e.target.closest && e.target.closest('[data-module="page-data-access"]')) return;
      document.querySelectorAll('[data-module="page-data-access"]').forEach(closeAllInRoot);
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
