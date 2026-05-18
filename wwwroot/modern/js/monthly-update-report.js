/**
 * Monthly report — section order (localStorage) and edit-view reorder UI.
 */
(function () {
  var STORAGE_KEY = 'compass.mr.sectionOrder.v1';
  var DEFAULT_ORDER = [
    'submission',
    'intelligence',
    'business-area',
    'changes',
    'trends',
    'milestones',
    'raid',
    'accessibility'
  ];

  function readStoredOrder() {
    try {
      var raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) return null;
      var parsed = JSON.parse(raw);
      return Array.isArray(parsed) && parsed.length ? parsed : null;
    } catch (e) {
      return null;
    }
  }

  function normalizeOrder(order, knownIds) {
    var seen = {};
    var result = [];
    order.forEach(function (id) {
      if (knownIds.indexOf(id) !== -1 && !seen[id]) {
        seen[id] = true;
        result.push(id);
      }
    });
    knownIds.forEach(function (id) {
      if (!seen[id]) result.push(id);
    });
    return result;
  }

  function getItemsContainer() {
    return document.querySelector('#mr-report-sections .dfe-f-toggle-group__items');
  }

  function getPanelsMap(container) {
    var map = {};
    container.querySelectorAll('[data-mr-section-id]').forEach(function (el) {
      map[el.getAttribute('data-mr-section-id')] = el;
    });
    return map;
  }

  function applyOrderToDom(order) {
    var container = getItemsContainer();
    if (!container) return;
    var panels = getPanelsMap(container);
    var knownIds = Object.keys(panels);
    var normalized = normalizeOrder(order, knownIds);
    normalized.forEach(function (id) {
      if (panels[id]) container.appendChild(panels[id]);
    });
    return normalized;
  }

  function panelTitle(el) {
    return el.getAttribute('data-mr-section-title')
      || (el.querySelector('.dfe-f-toggle__title') && el.querySelector('.dfe-f-toggle__title').textContent.trim())
      || el.getAttribute('data-mr-section-id')
      || 'Section';
  }

  function init() {
    var layout = document.getElementById('mr-report-layout');
    var reportGroup = document.getElementById('mr-report-sections');
    var orderPanel = document.getElementById('mr-report-order-panel');
    var orderList = document.getElementById('mr-report-order-list');
    var editBtn = document.getElementById('mr-report-edit-view');
    var saveBtn = document.getElementById('mr-report-order-save');
    var cancelBtn = document.getElementById('mr-report-order-cancel');
    if (!layout || !reportGroup || !orderPanel || !orderList) return;

    var container = getItemsContainer();
    if (!container) return;

    var panels = getPanelsMap(container);
    var knownIds = Object.keys(panels);
    var currentOrder = normalizeOrder(readStoredOrder() || DEFAULT_ORDER, knownIds);
    applyOrderToDom(currentOrder);

    var draftOrder = currentOrder.slice();
    var editing = false;

    function renderOrderList() {
      orderList.innerHTML = '';
      draftOrder.forEach(function (id, index) {
        var panel = panels[id];
        if (!panel) return;
        var li = document.createElement('li');
        li.className = 'mr-report-order-list__item';
        li.setAttribute('data-mr-order-id', id);
        li.draggable = true;

        var label = document.createElement('span');
        label.className = 'mr-report-order-list__label';
        label.textContent = panelTitle(panel);

        var actions = document.createElement('span');
        actions.className = 'mr-report-order-list__actions';

        function makeBtn(text, aria, disabled, handler) {
          var btn = document.createElement('button');
          btn.type = 'button';
          btn.className = 'govuk-button govuk-button--secondary govuk-!-margin-bottom-0 mr-report-order-list__btn';
          btn.textContent = text;
          btn.setAttribute('aria-label', aria);
          btn.disabled = disabled;
          btn.addEventListener('click', handler);
          return btn;
        }

        actions.appendChild(makeBtn('↑', 'Move ' + panelTitle(panel) + ' up', index === 0, function () {
          if (index === 0) return;
          var tmp = draftOrder[index - 1];
          draftOrder[index - 1] = draftOrder[index];
          draftOrder[index] = tmp;
          renderOrderList();
        }));
        actions.appendChild(makeBtn('↓', 'Move ' + panelTitle(panel) + ' down', index === draftOrder.length - 1, function () {
          if (index >= draftOrder.length - 1) return;
          var tmp = draftOrder[index + 1];
          draftOrder[index + 1] = draftOrder[index];
          draftOrder[index] = tmp;
          renderOrderList();
        }));

        li.appendChild(label);
        li.appendChild(actions);

        li.addEventListener('dragstart', function (e) {
          li.classList.add('mr-report-order-list__item--dragging');
          e.dataTransfer.setData('text/plain', id);
          e.dataTransfer.effectAllowed = 'move';
        });
        li.addEventListener('dragend', function () {
          li.classList.remove('mr-report-order-list__item--dragging');
        });
        li.addEventListener('dragover', function (e) {
          e.preventDefault();
          e.dataTransfer.dropEffect = 'move';
        });
        li.addEventListener('drop', function (e) {
          e.preventDefault();
          var fromId = e.dataTransfer.getData('text/plain');
          var toId = id;
          if (!fromId || fromId === toId) return;
          var fromIdx = draftOrder.indexOf(fromId);
          var toIdx = draftOrder.indexOf(toId);
          if (fromIdx < 0 || toIdx < 0) return;
          draftOrder.splice(fromIdx, 1);
          draftOrder.splice(toIdx, 0, fromId);
          renderOrderList();
        });

        orderList.appendChild(li);
      });
    }

    function setEditing(on) {
      editing = on;
      layout.classList.toggle('mr-report-layout--editing', on);
      orderPanel.hidden = !on;
      if (editBtn) {
        editBtn.setAttribute('aria-pressed', on ? 'true' : 'false');
        editBtn.textContent = on ? 'Done editing' : 'Edit view';
      }
      if (on) {
        draftOrder = currentOrder.slice();
        renderOrderList();
      }
    }

    if (editBtn) {
      editBtn.addEventListener('click', function () {
        setEditing(!editing);
      });
    }

    if (saveBtn) {
      saveBtn.addEventListener('click', function () {
        currentOrder = normalizeOrder(draftOrder, knownIds);
        applyOrderToDom(currentOrder);
        try {
          localStorage.setItem(STORAGE_KEY, JSON.stringify(currentOrder));
        } catch (e) { /* ignore quota errors */ }
        setEditing(false);
      });
    }

    if (cancelBtn) {
      cancelBtn.addEventListener('click', function () {
        setEditing(false);
      });
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
