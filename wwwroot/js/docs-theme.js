(function () {
  var root = document.getElementById('developers-doc-root');
  if (!root) return;

  var STORAGE_KEY = 'compass-docs-theme';
  var layout = document.getElementById('developers-doc-layout');
  var scrim = document.querySelector('[data-developers-nav-close]');

  function getStoredTheme() {
    try {
      var s = localStorage.getItem(STORAGE_KEY);
      if (s === 'light' || s === 'dark') return s;
    } catch (e) { /* ignore */ }
    return null;
  }

  function applyTheme(theme) {
    var isLight = theme === 'light';
    root.classList.remove('developers-doc-app--theme-dark', 'developers-doc-app--theme-light');
    root.classList.add(isLight ? 'developers-doc-app--theme-light' : 'developers-doc-app--theme-dark');
    root.setAttribute('data-developers-doc-theme', isLight ? 'light' : 'dark');
    try {
      localStorage.setItem(STORAGE_KEY, isLight ? 'light' : 'dark');
    } catch (e) { /* ignore */ }

    var btn = document.querySelector('[data-developers-theme-toggle]');
    if (btn) {
      btn.textContent = isLight ? 'Dark' : 'Light';
      btn.setAttribute('aria-pressed', isLight ? 'false' : 'true');
      btn.setAttribute(
        'aria-label',
        isLight ? 'Switch to dark documentation theme' : 'Switch to light documentation theme'
      );
    }
  }

  applyTheme(getStoredTheme() || 'dark');

  document.querySelector('[data-developers-theme-toggle]')?.addEventListener('click', function () {
    var cur = root.getAttribute('data-developers-doc-theme') === 'light' ? 'light' : 'dark';
    applyTheme(cur === 'dark' ? 'light' : 'dark');
  });

  function setNavOpen(open) {
    if (!layout) return;
    layout.classList.toggle('developers-doc-layout--nav-open', open);
    var toggle = document.querySelector('[data-developers-nav-toggle]');
    if (toggle) toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
    if (scrim) scrim.setAttribute('aria-hidden', open ? 'false' : 'true');
    document.body.classList.toggle('developers-doc--nav-open', open);
  }

  document.querySelector('[data-developers-nav-toggle]')?.addEventListener('click', function () {
    var open = !layout.classList.contains('developers-doc-layout--nav-open');
    setNavOpen(open);
  });

  scrim?.addEventListener('click', function () {
    setNavOpen(false);
  });

  layout?.querySelectorAll('.developers-doc-nav__link').forEach(function (a) {
    a.addEventListener('click', function () {
      if (window.matchMedia('(max-width: 59.99em)').matches) setNavOpen(false);
    });
  });

  window.addEventListener('keydown', function (e) {
    if (e.key === 'Escape') setNavOpen(false);
  });

  function announce(msg) {
    var live = document.getElementById('developers-doc-live');
    if (!live) return;
    live.textContent = '';
    window.setTimeout(function () {
      live.textContent = msg;
    }, 50);
  }

  function enhanceCodeBlocks() {
    root.querySelectorAll('.developers-code-block').forEach(function (pre) {
      if (pre.closest('.developers-doc-code-wrap')) return;
      var wrap = document.createElement('div');
      wrap.className =
        'developers-doc-code-wrap' +
        (pre.classList.contains('developers-code-block--inverse')
          ? ' developers-doc-code-wrap--inverse'
          : '');
      pre.parentNode.insertBefore(wrap, pre);
      wrap.appendChild(pre);
      var btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'developers-doc-code-copy';
      btn.textContent = 'Copy';
      btn.setAttribute('aria-label', 'Copy code to clipboard');
      btn.addEventListener('click', function () {
        var code = pre.querySelector('code');
        var text = code ? code.textContent : pre.textContent;
        function done(ok) {
          announce(ok ? 'Code copied to clipboard' : 'Copy failed');
          btn.textContent = ok ? 'Copied' : 'Copy';
          window.setTimeout(function () {
            btn.textContent = 'Copy';
          }, ok ? 2000 : 2500);
        }
        if (navigator.clipboard && navigator.clipboard.writeText) {
          navigator.clipboard.writeText(text).then(function () {
            done(true);
          }).catch(function () {
            done(false);
          });
        } else {
          try {
            var ta = document.createElement('textarea');
            ta.value = text;
            ta.setAttribute('readonly', '');
            ta.style.position = 'fixed';
            ta.style.left = '-9999px';
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
            done(true);
          } catch (err) {
            done(false);
          }
        }
      });
      wrap.insertBefore(btn, pre);
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', enhanceCodeBlocks);
  } else {
    enhanceCodeBlocks();
  }
})();
