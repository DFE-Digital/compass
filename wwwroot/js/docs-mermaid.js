(function () {
  var root = document.getElementById('developers-doc-root');
  if (!root) return;

  var MERMAID_URL = 'https://cdn.jsdelivr.net/npm/mermaid@11/dist/mermaid.esm.min.mjs';
  var mermaidPromise = null;
  var initialised = false;

  function currentTheme() {
    return root.getAttribute('data-developers-doc-theme') === 'light' ? 'default' : 'dark';
  }

  function getMermaidNodes() {
    return Array.prototype.slice.call(document.querySelectorAll('pre.mermaid'));
  }

  function captureSources(nodes) {
    nodes.forEach(function (pre) {
      if (!pre.dataset.mermaidSource) {
        pre.dataset.mermaidSource = pre.textContent;
      }
    });
  }

  function resetForRerender(nodes) {
    nodes.forEach(function (pre) {
      if (pre.dataset.mermaidSource != null) {
        pre.textContent = pre.dataset.mermaidSource;
      }
      pre.removeAttribute('data-processed');
    });
  }

  function loadMermaid() {
    if (mermaidPromise) return mermaidPromise;
    mermaidPromise = import(MERMAID_URL).then(function (mod) {
      return mod && mod.default ? mod.default : mod;
    });
    return mermaidPromise;
  }

  function announce(msg) {
    var live = document.getElementById('developers-doc-live');
    if (!live) return;
    live.textContent = '';
    window.setTimeout(function () { live.textContent = msg; }, 50);
  }

  function configure(mermaid, theme) {
    mermaid.initialize({
      startOnLoad: false,
      theme: theme,
      // Diagram sources are authored statically by us in the Razor view, so
      // 'loose' is safe and lets <br/> render inside node labels.
      securityLevel: 'loose',
      fontFamily: 'Inter, system-ui, -apple-system, "Segoe UI", Roboto, Arial, sans-serif',
      flowchart: {
        htmlLabels: true,
        curve: 'basis',
        useMaxWidth: true,
        nodeSpacing: 35,
        rankSpacing: 45
      },
      sequence: { useMaxWidth: true }
    });
  }

  function renderAll() {
    var nodes = getMermaidNodes();
    if (!nodes.length) return Promise.resolve();
    captureSources(nodes);
    if (initialised) resetForRerender(nodes);
    return loadMermaid().then(function (mermaid) {
      configure(mermaid, currentTheme());
      return mermaid.run({ querySelector: 'pre.mermaid' }).then(function () {
        initialised = true;
      });
    }).catch(function (err) {
      console.error('Mermaid render failed', err);
      nodes.forEach(function (pre) {
        if (pre.dataset.mermaidErrorShown === 'true') return;
        pre.dataset.mermaidErrorShown = 'true';
        var msg = document.createElement('p');
        msg.className = 'developers-data-flow__diagram-error govuk-body-s';
        msg.textContent = 'Diagram failed to render. View the Mermaid source tab instead.';
        pre.parentNode.insertBefore(msg, pre);
      });
    });
  }

  function setupTabs() {
    document.querySelectorAll('[data-developers-tabs]').forEach(function (group) {
      var tabs = group.querySelectorAll('[role="tab"]');
      var panes = group.querySelectorAll('[role="tabpanel"]');
      if (!tabs.length || !panes.length) return;

      function selectTab(target) {
        tabs.forEach(function (t) {
          var selected = t === target;
          t.setAttribute('aria-selected', selected ? 'true' : 'false');
          t.setAttribute('tabindex', selected ? '0' : '-1');
        });
        var targetId = target.getAttribute('aria-controls');
        panes.forEach(function (p) {
          if (p.id === targetId) p.removeAttribute('hidden');
          else p.setAttribute('hidden', '');
        });
      }

      tabs.forEach(function (tab, index) {
        tab.setAttribute('tabindex', tab.getAttribute('aria-selected') === 'true' ? '0' : '-1');
        tab.addEventListener('click', function () { selectTab(tab); });
        tab.addEventListener('keydown', function (e) {
          if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft' && e.key !== 'Home' && e.key !== 'End') return;
          e.preventDefault();
          var next = index;
          if (e.key === 'ArrowRight') next = (index + 1) % tabs.length;
          else if (e.key === 'ArrowLeft') next = (index - 1 + tabs.length) % tabs.length;
          else if (e.key === 'Home') next = 0;
          else if (e.key === 'End') next = tabs.length - 1;
          tabs[next].focus();
          selectTab(tabs[next]);
        });
      });
    });
  }

  function setupCopy() {
    document.querySelectorAll('[data-developers-copy]').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var targetSel = btn.getAttribute('data-developers-copy');
        var node = targetSel ? document.querySelector(targetSel) : null;
        if (!node) return;
        var text = node.textContent || '';
        function done(ok) {
          announce(ok ? 'Mermaid source copied to clipboard' : 'Copy failed');
          btn.textContent = ok ? 'Copied' : 'Copy';
          window.setTimeout(function () { btn.textContent = 'Copy'; }, ok ? 2000 : 2500);
        }
        if (navigator.clipboard && navigator.clipboard.writeText) {
          navigator.clipboard.writeText(text).then(function () { done(true); }).catch(function () { done(false); });
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
          } catch (err) { done(false); }
        }
      });
    });
  }

  document.addEventListener('developers-doc:themechange', function () {
    if (initialised) renderAll();
  });

  function init() {
    setupTabs();
    setupCopy();
    renderAll();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
