/**
 * Functional standard assessment conduct page: auto-save attainment/notes via fetch (CSP-safe; no inline script).
 */
(function () {
  'use strict';

  function init() {
    var root = document.getElementById('sc-standards-conduct');
    if (!root || root.getAttribute('data-conduct-editable') !== 'true') return;

    var saveUrl = root.getAttribute('data-save-url');
    if (!saveUrl) return;

    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    var token = tokenInput ? tokenInput.value : '';

    var noteTimers = {};
    var globalStatusTimer;

    document.querySelectorAll('.fsa-attainment-select').forEach(function (sel) {
      sel.addEventListener('change', function () {
        var rid = sel.dataset.responseId;
        var att = sel.value;
        var notesEl = document.querySelector('.fsa-notes-input[data-response-id="' + rid + '"]');
        var notesVal = notesEl ? notesEl.value : '';
        if (att !== '') {
          saveResponse(rid, att, notesVal, sel);
        }
      });
    });

    document.querySelectorAll('.fsa-notes-input').forEach(function (ta) {
      ta.addEventListener('input', function () {
        var rid = ta.dataset.responseId;
        clearTimeout(noteTimers[rid]);
        noteTimers[rid] = setTimeout(function () {
          var sel = document.querySelector('.fsa-attainment-select[data-response-id="' + rid + '"]');
          var att = sel && sel.value !== '' ? sel.value : null;
          saveResponse(rid, att, ta.value, ta);
        }, 800);
      });
    });

    function saveResponse(rid, att, notes, triggerEl) {
      var fd = new FormData();
      fd.append('responseId', rid);
      if (att !== null && att !== undefined && att !== '') {
        fd.append('attainment', att);
      }
      fd.append('notes', notes != null ? notes : '');
      fd.append('__RequestVerificationToken', token);

      fetch(saveUrl, { method: 'POST', body: fd, credentials: 'same-origin' })
        .then(function (r) { return r.json(); })
        .then(function (d) {
          if (d.success) {
            var criterion = triggerEl.closest('.fsa-criterion');
            var slot = criterion ? criterion.querySelector('.fsa-criterion__save-slot') : null;
            var target = slot || triggerEl.parentNode;
            var saved = document.createElement('span');
            saved.className = 'fsa-saved-indicator';
            saved.textContent = 'Saved';
            if (slot) {
              slot.innerHTML = '';
              slot.appendChild(saved);
            } else {
              target.appendChild(saved);
            }
            setTimeout(function () { saved.remove(); }, 2000);
            updateCounts();
            var statusEl = document.getElementById('fsa-save-status');
            if (statusEl) {
              statusEl.textContent = ' · Saved';
              clearTimeout(globalStatusTimer);
              globalStatusTimer = setTimeout(function () { statusEl.textContent = ''; }, 2200);
            }
          }
        })
        .catch(function () { /* silent */ });
    }

    function updateCounts() {
      document.querySelectorAll('.fsa-practice-area').forEach(function (pa) {
        var selects = pa.querySelectorAll('.fsa-attainment-select');
        var total = selects.length;
        var done = 0;
        selects.forEach(function (s) { if (s.value !== '') done++; });
        var badge = pa.querySelector('.fsa-pa-progress');
        if (badge) badge.textContent = done + ' / ' + total + ' assessed';
      });

      var themes = {};
      document.querySelectorAll('.fsa-criterion').forEach(function (c) {
        var tid = c.dataset.theme;
        if (!themes[tid]) themes[tid] = { total: 0, done: 0 };
        themes[tid].total++;
        var sel = c.querySelector('.fsa-attainment-select');
        if (sel && sel.value !== '') themes[tid].done++;
      });
      Object.keys(themes).forEach(function (tid) {
        var el = document.querySelector('.fsa-theme-progress[data-theme="' + tid + '"]');
        if (el) el.textContent = themes[tid].done + ' / ' + themes[tid].total;
      });

      var allSelects = document.querySelectorAll('.fsa-attainment-select');
      var globalTotal = allSelects.length;
      var globalDone = 0;
      var fmCount = 0;
      var pmCount = 0;
      var nmCount = 0;
      allSelects.forEach(function (s) {
        if (s.value !== '') globalDone++;
        if (s.value === '2') fmCount++;
        if (s.value === '1') pmCount++;
        if (s.value === '0') nmCount++;
      });
      var pct = globalTotal > 0 ? Math.round(100 * globalDone / globalTotal) : 0;
      var bar = document.querySelector('.fsa-progress-bar__fill');
      if (bar) bar.style.width = pct + '%';

      var stats = document.querySelectorAll('.fsa-stat');
      stats.forEach(function (st) {
        if (st.classList.contains('fsa-stat--green')) st.textContent = fmCount + ' fully met';
        if (st.classList.contains('fsa-stat--amber')) st.textContent = pmCount + ' partially met';
        if (st.classList.contains('fsa-stat--red')) st.textContent = nmCount + ' not met';
        if (st.classList.contains('fsa-stat--grey')) st.textContent = (globalTotal - globalDone) + ' unanswered';
      });
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
