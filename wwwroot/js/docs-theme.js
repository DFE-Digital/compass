(function () {
  var root = document.getElementById('developers-doc-root');
  if (!root) return;

  var layout = document.getElementById('developers-doc-layout');
  var scrim = document.querySelector('[data-developers-nav-close]');

  root.classList.remove('developers-doc-app--theme-dark');
  root.classList.add('developers-doc-app--theme-light');
  root.setAttribute('data-developers-doc-theme', 'light');

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

  function setupScrollSpy() {
    if (!('IntersectionObserver' in window)) return;

    var asides = document.querySelectorAll('.docs-page-grid__aside');
    asides.forEach(function (aside) {
      var rawLinks = aside.querySelectorAll('a[href^="#"]');
      if (!rawLinks.length) return;

      var links = [];
      var sections = [];
      var linkBySection = new Map();

      rawLinks.forEach(function (link) {
        var hash = link.getAttribute('href') || '';
        var id;
        try {
          id = decodeURIComponent(hash.slice(1));
        } catch (err) {
          id = hash.slice(1);
        }
        if (!id) return;
        var target = document.getElementById(id);
        if (!target) return;
        links.push(link);
        sections.push(target);
        linkBySection.set(target, link);
      });

      if (!sections.length) return;

      var clickPinUntil = 0;

      function setActive(link) {
        links.forEach(function (l) {
          if (l === link) {
            l.setAttribute('aria-current', 'true');
            l.classList.add('active');
          } else {
            l.removeAttribute('aria-current');
            l.classList.remove('active');
          }
        });
        if (link) keepLinkInView(link);
      }

      function keepLinkInView(link) {
        var asideRect = aside.getBoundingClientRect();
        var linkRect = link.getBoundingClientRect();
        var overflowsTop = linkRect.top < asideRect.top + 4;
        var overflowsBottom = linkRect.bottom > asideRect.bottom - 4;
        if (overflowsTop || overflowsBottom) {
          link.scrollIntoView({ block: 'nearest' });
        }
      }

      function pickActiveFromVisibility(visibleSet) {
        if (!visibleSet.size) {
          var topmostAbove = null;
          var threshold = window.innerHeight * 0.2;
          for (var i = 0; i < sections.length; i++) {
            if (sections[i].getBoundingClientRect().top <= threshold) {
              topmostAbove = sections[i];
            }
          }
          return topmostAbove;
        }
        for (var j = 0; j < sections.length; j++) {
          if (visibleSet.has(sections[j])) return sections[j];
        }
        return null;
      }

      var visible = new Set();
      var observer = new IntersectionObserver(function (entries) {
        entries.forEach(function (e) {
          if (e.isIntersecting) visible.add(e.target);
          else visible.delete(e.target);
        });
        if (Date.now() < clickPinUntil) return;
        var current = pickActiveFromVisibility(visible);
        if (current) setActive(linkBySection.get(current));
      }, {
        rootMargin: '-15% 0px -70% 0px',
        threshold: 0
      });

      sections.forEach(function (s) { observer.observe(s); });

      links.forEach(function (link) {
        link.addEventListener('click', function () {
          setActive(link);
          // Hold the click selection briefly so smooth-scroll doesn't repaint it
          // to whatever the observer sees mid-scroll.
          clickPinUntil = Date.now() + 600;
        });
      });

      if (window.location.hash) {
        var initial = linkBySection.get(document.getElementById(window.location.hash.slice(1)));
        if (initial) setActive(initial);
      }
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
      enhanceCodeBlocks();
      setupScrollSpy();
    });
  } else {
    enhanceCodeBlocks();
    setupScrollSpy();
  }
})();
