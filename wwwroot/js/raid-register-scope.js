(function () {
  'use strict';

  function escHtml(s) {
    var d = document.createElement('div');
    d.textContent = s;
    return d.innerHTML;
  }

  function showPanel(message, registers) {
    var panel = document.getElementById('raid-scope-conflict-panel');
    var msgEl = document.getElementById('raid-scope-conflict-message');
    var listEl = document.getElementById('raid-scope-conflict-registers');
    if (!panel || !msgEl || !listEl) return;

    msgEl.textContent = message;
    listEl.innerHTML = '';
    (registers || []).forEach(function (r) {
      var li = document.createElement('li');
      li.innerHTML = '<strong>' + escHtml(r.registerName) + '</strong> (owned by ' + escHtml(r.ownerName) + ')';
      listEl.appendChild(li);
    });
    listEl.style.display = (registers && registers.length) ? '' : 'none';

    panel.style.display = 'block';
    panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    panel.setAttribute('tabindex', '-1');
    panel.focus();
  }

  function hidePanel() {
    var panel = document.getElementById('raid-scope-conflict-panel');
    if (panel) panel.style.display = 'none';
  }

  function bindPanelDismiss() {
    document.querySelectorAll('[data-raid-scope-conflict-dismiss]').forEach(function (btn) {
      btn.addEventListener('click', hidePanel);
    });
  }

  function checkScopeTracking(registerId, scopeType, itemId, itemName) {
    if (!registerId || !itemId) return Promise.resolve();

    var url = '/modern/raid/api/register/' + registerId + '/scope-tracking'
      + '?scopeType=' + encodeURIComponent(scopeType)
      + '&itemId=' + encodeURIComponent(itemId);

    return fetch(url, { credentials: 'same-origin' })
      .then(function (res) { return res.ok ? res.json() : null; })
      .then(function (data) {
        if (!data || !data.hasConflict) return;
        var label = data.itemName || itemName || 'This item';
        var others = data.otherRegisters || [];
        var intro;
        if (others.length === 1) {
          var r = others[0];
          if (data.scopeType === 'service') {
            intro = 'The service "' + label + '" is also being tracked in the RAID register "' + r.registerName + '" owned by ' + r.ownerName + '.';
          } else {
            intro = 'The work item "' + label + '" is also being tracked in the RAID register "' + r.registerName + '" owned by ' + r.ownerName + '.';
          }
          showPanel(intro, []);
        } else if (data.scopeType === 'service') {
          intro = 'The service "' + label + '" is also being tracked in other RAID registers:';
          showPanel(intro, others);
        } else {
          intro = 'The work item "' + label + '" is also being tracked in other RAID registers:';
          showPanel(intro, others);
        }
      })
      .catch(function () { /* non-blocking */ });
  }

  window.RaidRegisterScope = {
    init: bindPanelDismiss,
    checkWorkItem: function (registerId, projectId, itemName) {
      return checkScopeTracking(registerId, 'workitem', projectId, itemName);
    },
    checkService: function (registerId, serviceId, itemName) {
      return checkScopeTracking(registerId, 'service', serviceId, itemName);
    }
  };

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bindPanelDismiss);
  } else {
    bindPanelDismiss();
  }
})();
