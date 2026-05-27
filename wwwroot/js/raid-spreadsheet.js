(function () {
  'use strict';

  var lookups = {};
  var csrfToken = '';
  var baseUrl = '';
  var registerId = 0;
  var activeFilters = {};
  var textFilters = {};
  var sortState = {};
  var STORAGE_PREFIX = 'raid-ss-';
  var a11yReorderTable = null;

  function readCsrfToken() {
    var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenEl ? tokenEl.value : '';
  }

  function parseJsonResponse(res) {
    return res.text().then(function (text) {
      var trimmed = (text || '').trim();
      if (!trimmed) {
        if (!res.ok) {
          throw new Error('Request failed (' + res.status + ')');
        }
        return {};
      }
      try {
        return JSON.parse(trimmed);
      } catch (e) {
        if (!res.ok) {
          throw new Error('Request failed (' + res.status + ')');
        }
        throw e;
      }
    });
  }

  function init(config) {
    lookups = config.lookups || {};
    csrfToken = config.csrfToken || readCsrfToken();
    baseUrl = config.baseUrl || '/modern/raid';
    registerId = config.registerId || 0;

    bindCellClicks();
    bindDetailButtons();
    bindModalClose();
    bindKeyboard();
    bindRefLinks();
    bindRelationLinks();
    bindRelationEditModal();
    bindFullscreen();
    bindSortHeaders();
    buildFilterRows();
    bindFilterToggleButtons();
    bindInlineAdd();
    captureDefaultColumnOrders();
    setupColumnResize();
    restoreColumnWidths();
    bindWrapToggle();
    restoreWrapState();
    bindReorderToggle();
    restoreColumnOrder();
    bindCommentButtons();
    bindAccessibleReorder();
    setupHeaderKeyboardResize();
    bindZoomControls();
    bindResetTableModal();
    bindClearFiltersButtons();
    restoreTableZoom();
    syncAllHeaderScrollMetrics();
    window.addEventListener('resize', syncAllHeaderScrollMetrics);
  }

  // ── Sorting ──

  function bindSortHeaders() {
    document.querySelectorAll('.raid-ss-sortable').forEach(function (th) {
      th.style.cursor = 'pointer';
      th.style.userSelect = 'none';
      th.addEventListener('click', function (e) {
        var table = th.closest('table');
        var col = th.getAttribute('data-col');
        var tableId = table.id;
        var key = tableId + ':' + col;

        var dir = 'asc';
        if (sortState[key] === 'asc') dir = 'desc';
        else if (sortState[key] === 'desc') dir = null;

        table.querySelectorAll('.raid-ss-sortable').forEach(function (h) {
          h.classList.remove('raid-ss-sort-asc', 'raid-ss-sort-desc');
        });

        sortState = {};
        if (dir) {
          sortState[key] = dir;
          th.classList.add(dir === 'asc' ? 'raid-ss-sort-asc' : 'raid-ss-sort-desc');
        }

        sortTable(table, th, dir);
      });
    });
  }

  function sortTable(table, th, dir) {
    var tbody = table.querySelector('tbody');
    var rows = Array.from(tbody.querySelectorAll('.raid-ss-row'));
    if (!dir) {
      rows.sort(function (a, b) {
        return parseInt(a.getAttribute('data-id')) - parseInt(b.getAttribute('data-id'));
      });
    } else {
      var colIdx = Array.from(th.parentNode.children).indexOf(th);
      rows.sort(function (a, b) {
        var aCell = a.children[colIdx];
        var bCell = b.children[colIdx];
        var aVal = getCellSortValue(aCell);
        var bVal = getCellSortValue(bCell);
        var aNum = parseFloat(aVal);
        var bNum = parseFloat(bVal);
        var result;
        if (!isNaN(aNum) && !isNaN(bNum)) {
          result = aNum - bNum;
        } else {
          result = aVal.localeCompare(bVal, undefined, { sensitivity: 'base' });
        }
        return dir === 'desc' ? -result : result;
      });
    }
    rows.forEach(function (row) { tbody.appendChild(row); });
  }

  function getCellSortValue(cell) {
    var val = cell.querySelector('.raid-ss-val');
    var text = val ? val.textContent.trim() : cell.textContent.trim();
    return text === '—' ? '' : text;
  }

  // ── Filtering ──

  var TEXT_FILTER_COLUMNS = ['ref', 'title', 'description', 'cause', 'impact', 'mitigations'];

  function getCellFilterText(cell) {
    if (!cell) return '';
    var relLink = cell.querySelector('.raid-ss-relation-item-link');
    if (relLink) return relLink.textContent.trim();
    var refLink = cell.querySelector('.raid-ss-ref-link');
    if (refLink) return refLink.textContent.trim();
    var scoreBadge = cell.querySelector('.raid-ss-score-badge');
    if (scoreBadge) return scoreBadge.textContent.trim();
    var val = cell.querySelector('.raid-ss-val');
    if (val) return val.textContent.trim();
    return cell.textContent.trim();
  }

  function columnShouldHaveFilter(th) {
    return !th.classList.contains('raid-ss-no-filter') && !!th.getAttribute('data-col');
  }

  function columnUsesTextFilter(th, colName) {
    if (th.classList.contains('raid-ss-filterable')) return false;
    if (th.classList.contains('raid-ss-filter-text')) return true;
    return TEXT_FILTER_COLUMNS.indexOf(colName) !== -1;
  }

  function getColumnHeaderLabel(th) {
    return (th.textContent || '').trim().replace(/\s+/g, ' ');
  }

  function createMultiFilterDropdown(table, colIdx, colName) {
    var dropdown = document.createElement('div');
    dropdown.className = 'raid-ss-filter-dropdown';

    var toggle = document.createElement('button');
    toggle.type = 'button';
    toggle.className = 'raid-ss-filter-toggle';
    toggle.textContent = 'All';
    toggle.setAttribute('data-col', colName);
    toggle.setAttribute('data-col-idx', colIdx);

    var panel = document.createElement('div');
    panel.className = 'raid-ss-filter-panel';

    var uniqueVals = getUniqueColumnValues(table, colIdx);
    uniqueVals.forEach(function (v) {
      var label = document.createElement('label');
      var cb = document.createElement('input');
      cb.type = 'checkbox';
      cb.value = v;
      cb.checked = false;
      label.appendChild(cb);
      label.appendChild(document.createTextNode(v));
      panel.appendChild(label);
    });

    var actions = document.createElement('div');
    actions.className = 'raid-ss-filter-actions';
    var selectAllBtn = document.createElement('button');
    selectAllBtn.type = 'button';
    selectAllBtn.textContent = 'Select all';
    var clearBtn = document.createElement('button');
    clearBtn.type = 'button';
    clearBtn.textContent = 'Clear';

    selectAllBtn.addEventListener('click', function () {
      panel.querySelectorAll('input[type="checkbox"]').forEach(function (cb) { cb.checked = true; });
      applyMultiFilter(table, colIdx, colName, panel, toggle);
    });
    clearBtn.addEventListener('click', function () {
      panel.querySelectorAll('input[type="checkbox"]').forEach(function (cb) { cb.checked = false; });
      applyMultiFilter(table, colIdx, colName, panel, toggle);
    });

    actions.appendChild(selectAllBtn);
    actions.appendChild(clearBtn);
    panel.appendChild(actions);

    panel.addEventListener('change', function () {
      applyMultiFilter(table, colIdx, colName, panel, toggle);
    });

    toggle.addEventListener('click', function (e) {
      e.stopPropagation();
      closeAllFilterPanels(panel);
      panel.classList.toggle('raid-ss-filter-open');
    });

    dropdown.appendChild(toggle);
    dropdown.appendChild(panel);
    return dropdown;
  }

  function createTextFilterInput(table, colIdx, colName, th) {
    var filterKey = table.id + ':' + colName;
    var label = getColumnHeaderLabel(th);
    var textInput = document.createElement('input');
    textInput.type = 'search';
    textInput.className = 'raid-ss-title-filter';
    textInput.placeholder = 'Filter ' + label + '\u2026';
    textInput.setAttribute('aria-label', 'Filter by ' + label);
    if (textFilters[filterKey]) textInput.value = textFilters[filterKey].query;

    var debounceTimer = null;
    textInput.addEventListener('input', function () {
      clearTimeout(debounceTimer);
      debounceTimer = setTimeout(function () {
        var q = textInput.value.trim().toLowerCase();
        if (!q) {
          delete textFilters[filterKey];
        } else {
          textFilters[filterKey] = { tableId: table.id, colIdx: colIdx, query: q };
        }
        applyFilters(table);
        updateClearFilterBtn(table);
      }, 200);
    });

    return textInput;
  }

  function populateFilterCell(td, th, table, headerRow) {
    var colIdx = Array.from(headerRow.children).indexOf(th);
    var colName = th.getAttribute('data-col');
    if (!columnShouldHaveFilter(th)) return;

    if (columnUsesTextFilter(th, colName)) {
      td.appendChild(createTextFilterInput(table, colIdx, colName, th));
    } else {
      td.appendChild(createMultiFilterDropdown(table, colIdx, colName));
    }
  }

  function buildFilterRows() {
    document.querySelectorAll('.raid-ss-filter-row').forEach(function (filterRow) {
      var headerRow = filterRow.previousElementSibling;
      if (!headerRow) return;
      var table = filterRow.closest('table');
      var headers = headerRow.querySelectorAll('th');
      filterRow.innerHTML = '';

      headers.forEach(function (th) {
        var td = document.createElement('td');
        td.className = 'govuk-table__cell raid-ss-filter-cell';
        td.style.padding = '4px 6px';
        populateFilterCell(td, th, table, headerRow);

        if (th.classList.contains('raid-ss-sticky-col')) {
          td.classList.add('raid-ss-sticky-col');
        }

        filterRow.appendChild(td);
      });
    });

    document.addEventListener('click', function (e) {
      if (!e.target.closest('.raid-ss-filter-dropdown')) {
        closeAllFilterPanels();
      }
    });
  }

  function closeAllFilterPanels(except) {
    document.querySelectorAll('.raid-ss-filter-panel.raid-ss-filter-open').forEach(function (p) {
      if (p !== except) p.classList.remove('raid-ss-filter-open');
    });
  }

  function applyMultiFilter(table, colIdx, colName, panel, toggle) {
    var checked = [];
    panel.querySelectorAll('input[type="checkbox"]:checked').forEach(function (cb) {
      checked.push(cb.value);
    });

    var filterKey = table.id + ':' + colName;
    if (checked.length === 0) {
      delete activeFilters[filterKey];
      toggle.textContent = 'All';
      toggle.classList.remove('raid-ss-filter-active');
    } else {
      activeFilters[filterKey] = { tableId: table.id, colIdx: colIdx, values: checked };
      toggle.textContent = checked.length === 1 ? checked[0] : checked.length + ' selected';
      toggle.classList.add('raid-ss-filter-active');
    }

    applyFilters(table);
    updateClearFilterBtn(table);
  }

  function getUniqueColumnValues(table, colIdx) {
    var vals = {};
    table.querySelectorAll('tbody .raid-ss-row').forEach(function (row) {
      var cell = row.children[colIdx];
      if (!cell) return;
      var text = getCellFilterText(cell);
      if (text && text !== '—') vals[text] = true;
    });
    return Object.keys(vals).sort(function (a, b) {
      return a.localeCompare(b, undefined, { sensitivity: 'base' });
    });
  }

  function applyFilters(table) {
    var tableId = table.id;
    var filtersForTable = {};
    var textFiltersForTable = {};
    Object.keys(activeFilters).forEach(function (key) {
      if (activeFilters[key].tableId === tableId) {
        filtersForTable[activeFilters[key].colIdx] = activeFilters[key].values;
      }
    });
    Object.keys(textFilters).forEach(function (key) {
      if (textFilters[key].tableId === tableId) {
        textFiltersForTable[textFilters[key].colIdx] = textFilters[key].query;
      }
    });

    table.querySelectorAll('tbody .raid-ss-row').forEach(function (row) {
      var show = true;
      Object.keys(filtersForTable).forEach(function (colIdx) {
        var cell = row.children[parseInt(colIdx)];
        if (!cell) return;
        var text = getCellFilterText(cell);
        if (filtersForTable[colIdx].indexOf(text) === -1) show = false;
      });
      Object.keys(textFiltersForTable).forEach(function (colIdx) {
        var cell = row.children[parseInt(colIdx)];
        if (!cell) return;
        var text = getCellFilterText(cell).toLowerCase();
        if (text.indexOf(textFiltersForTable[colIdx]) === -1) show = false;
      });
      row.style.display = show ? '' : 'none';
    });
  }

  function updateClearFilterBtn(table) {
    var hasFilters = Object.keys(activeFilters).some(function (k) {
      return activeFilters[k].tableId === table.id;
    }) || Object.keys(textFilters).some(function (k) {
      return textFilters[k].tableId === table.id;
    });
    var section = table.closest('.raid-ss-tab-panel') || table.closest('[id^="ss-panel-"]');
    if (section) {
      var btn = section.querySelector('.raid-ss-clear-filters-btn');
      if (btn) btn.style.display = hasFilters ? '' : 'none';
    }
  }

  function clearFilters() {
    activeFilters = {};
    textFilters = {};
    document.querySelectorAll('.raid-ss-title-filter').forEach(function (inp) {
      inp.value = '';
    });
    document.querySelectorAll('.raid-ss-filter-toggle').forEach(function (btn) {
      btn.textContent = 'All';
      btn.classList.remove('raid-ss-filter-active');
    });
    document.querySelectorAll('.raid-ss-filter-panel input[type="checkbox"]').forEach(function (cb) {
      cb.checked = false;
    });
    document.querySelectorAll('.raid-spreadsheet-table').forEach(function (table) {
      table.querySelectorAll('tbody .raid-ss-row').forEach(function (row) {
        row.style.display = '';
      });
    });
    document.querySelectorAll('.raid-ss-clear-filters-btn').forEach(function (btn) {
      btn.style.display = 'none';
    });
    document.querySelectorAll('.raid-ss-filter-row').forEach(function (row) {
      row.style.display = 'none';
    });
    document.querySelectorAll('.raid-ss-toggle-filters-btn').forEach(function (btn) {
      btn.classList.remove('raid-ss-btn-active');
    });
  }

  function bindFilterToggleButtons() {
    document.querySelectorAll('.raid-ss-toggle-filters-btn').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var section = btn.closest('.raid-ss-tab-panel') || btn.closest('[id^="ss-panel-"]') || btn.closest('.raid-ss-tab-active') || document.body;
        var filterRows = section.querySelectorAll('.raid-ss-filter-row');
        if (filterRows.length === 0) filterRows = document.querySelectorAll('.raid-ss-filter-row');
        var willShow = false;
        filterRows.forEach(function (row) {
          var isVisible = row.style.display !== 'none';
          if (!isVisible) willShow = true;
          row.style.display = isVisible ? 'none' : '';
        });
        btn.classList.toggle('raid-ss-btn-active', willShow);
      });
    });
  }

  // ── Inline editing (select dropdowns and user lookup) ──

  function bindCellClicks() {
    document.addEventListener('click', function (e) {
      var cell = e.target.closest('.raid-ss-editable');
      if (!cell || cell.classList.contains('raid-ss-editing')) return;

      var type = cell.getAttribute('data-type');
      if (type === 'modal') {
        openTextModal(cell);
        return;
      }
      if (type === 'userlookup') {
        closeAllEditors();
        openUserLookup(cell);
        return;
      }

      closeAllEditors();
      openEditor(cell);
    });
  }

  function openEditor(cell) {
    var type = cell.getAttribute('data-type');
    var field = cell.getAttribute('data-field');
    var row = cell.closest('.raid-ss-row');
    var valSpan = cell.querySelector('.raid-ss-val');
    if (!valSpan) return;

    cell.classList.add('raid-ss-editing');
    row.classList.add('raid-ss-row-editing');

    if (type === 'select') {
      var lookupName = cell.getAttribute('data-lookup');
      var currentId = cell.getAttribute('data-current');
      var options = lookups[lookupName] || [];

      var sel = document.createElement('select');
      sel.className = 'raid-ss-inline-select';
      sel.innerHTML = '<option value="">— Select —</option>';
      options.forEach(function (opt) {
        var o = document.createElement('option');
        o.value = opt.id;
        o.textContent = opt.name;
        if (String(opt.id) === String(currentId)) o.selected = true;
        sel.appendChild(o);
      });

      valSpan.style.display = 'none';
      cell.appendChild(sel);
      sel.focus();

      sel.addEventListener('change', function () {
        var newVal = sel.value;
        var newText = sel.options[sel.selectedIndex].text;
        var badgeKind = cell.getAttribute('data-badge-kind');
        saveField(row, field, newVal, function () {
          cell.setAttribute('data-current', newVal);
          if (badgeKind) {
            setLookupBadgeValSpan(valSpan, newVal ? newText : '—', badgeKind);
          } else {
            valSpan.textContent = newVal ? newText : '—';
          }
          closeEditor(cell);
          updateScoresFromResponse(row);
        }, function () {
          closeEditor(cell);
        });
      });

      sel.addEventListener('blur', function () {
        setTimeout(function () { closeEditor(cell); }, 150);
      });

      sel.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') closeEditor(cell);
      });
    }
  }

  function closeEditor(cell) {
    var row = cell.closest('.raid-ss-row');
    cell.classList.remove('raid-ss-editing');
    if (row) row.classList.remove('raid-ss-row-editing');
    var valSpan = cell.querySelector('.raid-ss-val');
    if (valSpan) valSpan.style.display = '';
    var inp = cell.querySelector('.raid-ss-inline-select, .raid-ss-inline-input, .raid-ss-user-lookup-wrap');
    if (inp) inp.remove();
  }

  function closeAllEditors() {
    document.querySelectorAll('.raid-ss-editing').forEach(closeEditor);
  }

  // ── User lookup (Owner field) ──

  function openUserLookup(cell) {
    var field = cell.getAttribute('data-field');
    var row = cell.closest('.raid-ss-row');
    var valSpan = cell.querySelector('.raid-ss-val');
    if (!valSpan) return;

    row.classList.add('raid-ss-row-editing');

    var modal = document.getElementById('raid-user-modal');
    if (!modal) return;

    var searchInput = document.getElementById('raid-user-modal-search');
    var listEl = document.getElementById('raid-user-modal-list');
    var hintEl = document.getElementById('raid-user-modal-hint');
    var clearBtn = document.getElementById('raid-user-modal-clear');
    var cancelBtn = document.getElementById('raid-user-modal-cancel');
    var closeBtn = modal.querySelector('.raid-modal-close');

    searchInput.value = '';
    listEl.innerHTML = '';
    listEl.style.display = 'none';
    hintEl.style.display = '';
    hintEl.textContent = 'Type at least 2 characters to search.';

    var hasCurrent = cell.getAttribute('data-current') && valSpan.textContent.trim() !== '—';
    clearBtn.style.display = hasCurrent ? '' : 'none';

    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';
    searchInput.focus();

    var debounceTimer = null;

    function cleanup() {
      row.classList.remove('raid-ss-row-editing');
      modal.style.display = 'none';
      document.body.style.overflow = '';
      searchInput.removeEventListener('input', onInput);
      searchInput.removeEventListener('keydown', onKeydown);
      clearBtn.replaceWith(clearBtn.cloneNode(true));
      cancelBtn.replaceWith(cancelBtn.cloneNode(true));
      closeBtn.replaceWith(closeBtn.cloneNode(true));
      clearTimeout(debounceTimer);
    }

    function selectUser(userId, userName) {
      saveField(row, field, userId, function () {
        cell.setAttribute('data-current', userId);
        valSpan.textContent = userName;
        cleanup();
      }, function () {
        cleanup();
      });
    }

    function onInput() {
      clearTimeout(debounceTimer);
      var q = searchInput.value.trim();
      if (q.length < 2) {
        listEl.style.display = 'none';
        hintEl.style.display = '';
        hintEl.textContent = 'Type at least 2 characters to search.';
        return;
      }
      hintEl.textContent = 'Searching…';
      hintEl.style.display = '';
      debounceTimer = setTimeout(function () {
        fetch('/api/users/search?q=' + encodeURIComponent(q) + '&top=10')
          .then(function (r) { return r.json(); })
          .then(function (users) {
            if (!Array.isArray(users)) users = users.users || users.value || [];
            listEl.innerHTML = '';
            if (users.length === 0) {
              listEl.style.display = 'none';
              hintEl.textContent = 'No users found.';
              hintEl.style.display = '';
              return;
            }
            hintEl.style.display = 'none';
            users.forEach(function (u) {
              var li = document.createElement('li');
              li.style.cssText = 'padding:8px 4px; border-bottom:1px solid #b1b4b6; cursor:pointer;';
              li.setAttribute('data-user-id', u.id);
              var displayName = u.displayName || u.name || u.email;
              li.innerHTML = '<strong>' + escapeHtml(displayName) + '</strong>'
                + (u.jobTitle ? '<br><span style="color:#505a5f; font-size:14px;">' + escapeHtml(u.jobTitle) + '</span>' : '')
                + (u.email ? '<br><span style="color:#505a5f; font-size:14px;">' + escapeHtml(u.email) + '</span>' : '');
              li.addEventListener('click', function () {
                selectUser(u.id, displayName);
              });
              li.addEventListener('mouseenter', function () { li.style.background = '#f3f2f1'; });
              li.addEventListener('mouseleave', function () { li.style.background = ''; });
              listEl.appendChild(li);
            });
            listEl.style.display = '';
          })
          .catch(function () {
            listEl.style.display = 'none';
            hintEl.textContent = 'Search failed. Please try again.';
            hintEl.style.display = '';
          });
      }, 300);
    }

    function onKeydown(e) {
      if (e.key === 'Escape') cleanup();
    }

    searchInput.addEventListener('input', onInput);
    searchInput.addEventListener('keydown', onKeydown);

    clearBtn.addEventListener('click', function () {
      saveField(row, field, null, function () {
        cell.setAttribute('data-current', '');
        valSpan.textContent = '—';
        cleanup();
      }, function () {
        cleanup();
      });
    }, { once: true });

    cancelBtn.addEventListener('click', function () { cleanup(); }, { once: true });
    closeBtn.addEventListener('click', function () { cleanup(); }, { once: true });
  }

  function escapeHtml(str) {
    var div = document.createElement('div');
    div.textContent = str || '';
    return div.innerHTML;
  }

  // ── Text editing modal (Title, Description, Cause, Impact, Mitigations) ──

  function openTextModal(cell) {
    var field = cell.getAttribute('data-field');
    var label = cell.getAttribute('data-label') || field;
    var row = cell.closest('.raid-ss-row');
    var valSpan = cell.querySelector('.raid-ss-val');
    var currentText = valSpan ? valSpan.textContent.trim() : '';
    if (currentText === '—') currentText = '';

    row.classList.add('raid-ss-row-editing');

    var modal = document.getElementById('raid-text-modal');
    if (!modal) return;

    document.getElementById('raid-text-modal-title').textContent = 'Edit ' + label;
    var textarea = document.getElementById('raid-text-modal-input');
    textarea.value = currentText;

    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';
    textarea.focus();

    var saveBtn = document.getElementById('raid-text-modal-save');
    var cancelBtn = document.getElementById('raid-text-modal-cancel');
    var closeBtn = modal.querySelector('.raid-modal-close');

    function cleanup() {
      row.classList.remove('raid-ss-row-editing');
      saveBtn.replaceWith(saveBtn.cloneNode(true));
      cancelBtn.replaceWith(cancelBtn.cloneNode(true));
      closeBtn.replaceWith(closeBtn.cloneNode(true));
      modal.style.display = 'none';
      document.body.style.overflow = '';
    }

    saveBtn.addEventListener('click', function handler() {
      var newVal = textarea.value.trim();
      saveField(row, field, newVal, function () {
        if (valSpan) valSpan.textContent = newVal || '—';
        cleanup();
      }, function () {
        cleanup();
      });
    }, { once: true });

    cancelBtn.addEventListener('click', function () { cleanup(); }, { once: true });
    closeBtn.addEventListener('click', function () { cleanup(); }, { once: true });
  }

  // ── Keyboard navigation ──

  function bindKeyboard() {
    document.addEventListener('keydown', function (e) {
      if (e.key === 'Escape') {
        closeAllEditors();
        closeModal();
        closeTextModal();
        closeUserModal();
        closeRelationEditModal();
      }
    });
  }

  function closeUserModal() {
    var modal = document.getElementById('raid-user-modal');
    if (modal) {
      modal.style.display = 'none';
      document.body.style.overflow = '';
    }
    document.querySelectorAll('.raid-ss-row-editing').forEach(function (r) {
      r.classList.remove('raid-ss-row-editing');
    });
  }

  // ── Save field via API ──

  var _lastResponse = null;

  function saveField(row, field, value, onSuccess, onError) {
    var entity = row.getAttribute('data-entity');
    var id = row.getAttribute('data-id');
    var url = baseUrl + '/api/' + entity + '/' + id + '/update';

    row.classList.add('raid-ss-saving');

    fetch(url, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': csrfToken
      },
      body: JSON.stringify({ field: field, value: value })
    })
    .then(function (res) {
      if (!res.ok) throw new Error('Save failed: ' + res.status);
      return res.json();
    })
    .then(function (data) {
      row.classList.remove('raid-ss-saving');
      row.classList.add('raid-ss-saved');
      setTimeout(function () { row.classList.remove('raid-ss-saved'); }, 600);
      _lastResponse = data;
      if (data.updatedAt) {
        var editedCell = row.querySelector('.raid-ss-last-edited .raid-ss-val');
        if (editedCell) editedCell.textContent = data.updatedAt;
      }
      if (onSuccess) onSuccess(data);
    })
    .catch(function (err) {
      row.classList.remove('raid-ss-saving');
      row.classList.add('raid-ss-error');
      setTimeout(function () { row.classList.remove('raid-ss-error'); }, 600);
      console.error('RAID save error:', err);
      if (onError) onError(err);
    });
  }

  function updateScoresFromResponse(row) {
    if (!_lastResponse) return;
    var scoreMap = {
      inherentScore: _lastResponse.inherentScore,
      currentScore: _lastResponse.currentScore,
      residualScore: _lastResponse.residualScore,
      toleranceScore: _lastResponse.toleranceScore
    };
    Object.keys(scoreMap).forEach(function (key) {
      var cell = row.querySelector('[data-score-field="' + key + '"]');
      if (cell && scoreMap[key] !== undefined) {
        renderScoreBadgeCell(cell, scoreMap[key]);
      }
    });
    if (scoreMap.currentScore !== undefined) {
      applyRefScoreIndicator(row, scoreMap.currentScore);
    }
  }

  // ── Register page counts (dashboard stat cards + manage tabs) ──

  function isOpenRaidStatus(status) {
    var s = (status || '').trim().toLowerCase();
    return s !== 'closed';
  }

  function adjustRegisterCount(key, delta) {
    if (!delta) return;
    document.querySelectorAll('[data-register-count="' + key + '"]').forEach(function (el) {
      var text = (el.textContent || '').trim();
      var n;
      if (text.charAt(0) === '(') {
        n = (parseInt(text.replace(/[()]/g, ''), 10) || 0) + delta;
        el.textContent = '(' + n + ')';
      } else {
        n = (parseInt(text, 10) || 0) + delta;
        el.textContent = String(n);
      }
    });
  }

  function onRegisterItemCreated(entityType, data) {
    var totalKey = entityType === 'risk' ? 'total-risks'
      : entityType === 'issue' ? 'total-issues'
        : entityType === 'nearmiss' ? 'total-nearmisses'
          : entityType === 'assumption' ? 'total-assumptions'
            : null;
    if (!totalKey) return;

    adjustRegisterCount(totalKey, 1);

    if (isOpenRaidStatus(data.status)) {
      var openKey = entityType === 'risk' ? 'open-risks'
        : entityType === 'issue' ? 'open-issues'
          : entityType === 'nearmiss' ? 'open-nearmisses'
            : entityType === 'assumption' ? 'open-assumptions'
              : null;
      if (openKey) adjustRegisterCount(openKey, 1);
    }
  }

  // ── Inline add new row ──

  function bindInlineAdd() {
    document.addEventListener('click', function (e) {
      var btn = e.target.closest('.raid-ss-add-btn') || e.target.closest('#btn-add-risk-top, #btn-add-issue-top, #btn-add-nearmiss-top, #btn-add-assumption-top');
      if (!btn) return;

      var entityType = btn.getAttribute('data-entity-type');
      var table = btn.closest('table') || document.querySelector('table[data-entity-type="' + entityType + '"]');
      var tbody = table ? table.querySelector('tbody') : null;
      if (!tbody) return;

      var existingInput = tbody.querySelector('.raid-ss-new-row');
      if (existingInput) {
        existingInput.querySelector('input')?.focus();
        return;
      }

      var headerCells = table.querySelectorAll('thead .raid-ss-header-row th');
      var colCount = headerCells.length;

      var newRow = document.createElement('tr');
      newRow.className = 'govuk-table__row raid-ss-new-row';

      var refTd = document.createElement('td');
      refTd.className = 'govuk-table__cell raid-ss-sticky-col';
      refTd.textContent = '—';
      newRow.appendChild(refTd);

      var titleTd = document.createElement('td');
      titleTd.className = 'govuk-table__cell';
      titleTd.setAttribute('colspan', colCount - 2);
      titleTd.style.padding = '4px 6px';

      var inputWrap = document.createElement('div');
      inputWrap.style.display = 'flex';
      inputWrap.style.gap = '8px';
      inputWrap.style.alignItems = 'center';

      var titleInput = document.createElement('input');
      titleInput.type = 'text';
      titleInput.className = 'govuk-input govuk-input--width-10';
      if (entityType === 'assumption') {
        titleInput.placeholder = 'Enter assumption description…';
      } else if (entityType === 'nearmiss') {
        titleInput.placeholder = 'Enter impact description…';
      } else {
        titleInput.placeholder = 'Enter ' + entityType + ' title…';
      }
      titleInput.style.cssText = 'font-size:14px; padding:4px 8px; height:32px; width:50%; max-width:400px;';

      var saveBtn = document.createElement('button');
      saveBtn.type = 'button';
      saveBtn.className = 'govuk-button govuk-button--small govuk-!-margin-bottom-0';
      saveBtn.textContent = 'Save';
      saveBtn.style.fontSize = '13px';
      saveBtn.style.padding = '4px 12px';
      saveBtn.style.minHeight = '28px';

      var cancelBtn = document.createElement('button');
      cancelBtn.type = 'button';
      cancelBtn.className = 'govuk-button govuk-button--secondary govuk-button--small govuk-!-margin-bottom-0';
      cancelBtn.textContent = 'Cancel';
      cancelBtn.style.fontSize = '13px';
      cancelBtn.style.padding = '4px 12px';
      cancelBtn.style.minHeight = '28px';

      inputWrap.appendChild(titleInput);
      inputWrap.appendChild(saveBtn);
      inputWrap.appendChild(cancelBtn);
      titleTd.appendChild(inputWrap);
      newRow.appendChild(titleTd);

      var actionTd = document.createElement('td');
      actionTd.className = 'govuk-table__cell';
      newRow.appendChild(actionTd);

      tbody.insertBefore(newRow, tbody.firstChild);
      titleInput.focus();

      function doSave() {
        var title = titleInput.value.trim();
        if (!title) { titleInput.focus(); return; }
        saveBtn.disabled = true;
        saveBtn.textContent = 'Saving…';

        var url = baseUrl + '/api/register/' + registerId + '/' + entityType + '/create';

        fetch(url, {
          method: 'POST',
          credentials: 'same-origin',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': csrfToken || readCsrfToken()
          },
          body: JSON.stringify(entityType === 'assumption' ? { description: title } : { title: title })
        })
        .then(function (r) {
          return parseJsonResponse(r).then(function (d) {
            if (!r.ok) throw new Error(d.error || 'Create failed (' + r.status + ')');
            return d;
          });
        })
        .then(function (data) {
          newRow.remove();
          data.title = data.title || title;
          var dataRow = buildNewDataRow(table, data, entityType);
          if (dataRow) {
            tbody.insertBefore(dataRow, tbody.firstChild);
            dataRow.classList.add('raid-ss-row-highlight');
            dataRow.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
            setTimeout(function () { dataRow.classList.remove('raid-ss-row-highlight'); }, 3000);
            onRegisterItemCreated(entityType, data);
            rebuildFilterRow(table);
          } else {
            window.location.reload();
          }
        })
        .catch(function (err) {
          saveBtn.disabled = false;
          saveBtn.textContent = 'Save';
          console.error('Inline create error:', err);
          alert('Could not create ' + entityType + ': ' + (err.message || 'Unknown error'));
        });
      }

      saveBtn.addEventListener('click', doSave);
      titleInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter') { e.preventDefault(); doSave(); }
        if (e.key === 'Escape') newRow.remove();
      });
      cancelBtn.addEventListener('click', function () { newRow.remove(); });
    });
  }

  // ── Build a data row from create API response ──

  function formatEntityRef(entityType, id) {
    var prefix = entityType === 'risk' ? 'R'
      : entityType === 'issue' ? 'I'
        : entityType === 'nearmiss' ? 'NM'
          : entityType === 'assumption' ? 'A'
            : entityType.charAt(0).toUpperCase();
    var num = String(id).padStart(4, '0');
    return prefix + '-' + num;
  }

  function commentEntityType(entity) {
    if (entity === 'nearmiss') return 'NearMiss';
    if (entity === 'assumption') return 'Assumption';
    return entity.charAt(0).toUpperCase() + entity.slice(1);
  }

  function appendValSpan(td, text) {
    var span = document.createElement('span');
    span.className = 'raid-ss-val';
    span.textContent = text || '—';
    td.appendChild(span);
    return span;
  }

  function isSpreadsheetTierEditable(tierId) {
    var opts = lookups.riskTiers || [];
    if (!tierId) return true;
    return opts.some(function (opt) { return String(opt.id) === String(tierId); });
  }

  function raidLookupBadgeClass(kind, text) {
    var t = (text || '').trim();
    var s = t.toLowerCase();
    var base = 'dfe-f-badge dfe-f-badge--small';
    if (!t || t === '—') return base + ' dfe-f-badge--grey';

    if (kind === 'riskStatus') {
      if (s.indexOf('escalat') !== -1) return base + ' dfe-f-badge--red';
      if (s.indexOf('closed') !== -1) return base + ' dfe-f-badge--green';
      if (s.indexOf('proposed') !== -1) return base + ' dfe-f-badge--grey';
      return base + ' dfe-f-badge--blue';
    }
    if (kind === 'issueStatus') {
      if (s.indexOf('closed') !== -1) return base + ' dfe-f-badge--green';
      return base + ' dfe-f-badge--blue';
    }
    if (kind === 'priority') {
      if (s.indexOf('critical') !== -1) return 'dfe-f-badge dfe-f-badge--red dfe-f-badge--small';
      if (s.indexOf('high') !== -1) return 'dfe-f-badge dfe-f-badge--orange dfe-f-badge--small';
      if (s.indexOf('medium') !== -1) return 'dfe-f-badge dfe-f-badge--blue dfe-f-badge--small';
      if (s.indexOf('low') !== -1) return base + ' dfe-f-badge--grey';
      return base + ' dfe-f-badge--grey';
    }
    if (kind === 'severity') {
      if (s.indexOf('critical') !== -1) return 'dfe-f-badge dfe-f-badge--red dfe-f-badge--small';
      if (s.indexOf('major') !== -1) return 'dfe-f-badge dfe-f-badge--orange dfe-f-badge--small';
      return 'dfe-f-badge dfe-f-badge--green dfe-f-badge--small';
    }
    if (kind === 'tier') {
      if (s.indexOf('proposed') !== -1) {
        if (s.indexOf('1') !== -1 || s.indexOf('tier one') !== -1 || s === 't1') return base + ' dfe-f-badge--blue';
        if (s.indexOf('2') !== -1 || s.indexOf('tier two') !== -1 || s === 't2') return base + ' dfe-f-badge--orange';
        return base + ' dfe-f-badge--grey';
      }
      if (s.indexOf('3') !== -1 || s.indexOf('tier three') !== -1 || s === 't3') return base + ' dfe-f-badge--red';
      if (s.indexOf('2') !== -1 || s.indexOf('tier two') !== -1 || s === 't2') return base + ' dfe-f-badge--orange';
      if (s.indexOf('1') !== -1 || s.indexOf('tier one') !== -1 || s === 't1') return base + ' dfe-f-badge--blue';
      return base + ' dfe-f-badge--grey';
    }
    return base + ' dfe-f-badge--grey';
  }

  function formatLookupBadgeLabel(text, kind) {
    if (!text || text === '—') return '—';
    if (kind === 'tier') return text;
    return text.toUpperCase();
  }

  function setLookupBadgeValSpan(span, text, kind) {
    var label = formatLookupBadgeLabel(text, kind);
    span.className = 'raid-ss-val ' + raidLookupBadgeClass(kind, label);
    span.textContent = label;
  }

  function appendBadgeValSpan(td, text, kind) {
    var span = document.createElement('span');
    setLookupBadgeValSpan(span, text || '—', kind);
    td.appendChild(span);
    return span;
  }

  function appendSelectCell(td, field, lookupName, currentId, displayText, badgeKind) {
    td.classList.add('raid-ss-editable');
    td.setAttribute('data-type', 'select');
    td.setAttribute('data-field', field);
    td.setAttribute('data-lookup', lookupName);
    if (currentId != null) td.setAttribute('data-current', currentId);
    if (badgeKind) {
      td.classList.add('raid-ss-badge-cell');
      td.setAttribute('data-badge-kind', badgeKind);
      appendBadgeValSpan(td, displayText || '—', badgeKind);
    } else {
      appendValSpan(td, displayText || '—');
    }
  }

  function appendModalCell(td, field, label, text) {
    td.classList.add('raid-ss-editable');
    td.setAttribute('data-type', 'modal');
    td.setAttribute('data-field', field);
    td.setAttribute('data-label', label);
    appendValSpan(td, text || '—');
  }

  function appendUserLookupCell(td, field, label, currentUserId, displayText) {
    td.classList.add('raid-ss-editable');
    td.setAttribute('data-type', 'userlookup');
    td.setAttribute('data-field', field);
    td.setAttribute('data-label', label);
    if (currentUserId != null) td.setAttribute('data-current', currentUserId);
    appendValSpan(td, displayText || '—');
  }

  function appendScoreCell(td, scoreField, score) {
    td.className = 'govuk-table__cell govuk-table__cell--numeric raid-ss-score';
    td.setAttribute('data-score-field', scoreField);
    renderScoreBadgeCell(td, score);
  }

  function relationDisplayText(rel) {
    if (!rel || rel.relationKind === 'Unknown') return '—';
    var target = rel.relationRelatedTitle || rel.relationTarget;
    if (rel.relationKind === 'Organisation') return 'Organisation';
    if (rel.relationKind === 'Work') {
      return target ? 'Work item · ' + target : 'Work item';
    }
    if (rel.relationKind === 'FIPS') {
      return target ? 'Service · ' + target : 'Service';
    }
    return '—';
  }

  function relationTargetName(rel) {
    if (!rel || rel.relationKind === 'Organisation' || rel.relationKind === 'Unknown') return null;
    return (rel.relationRelatedTitle || rel.relationTarget || '').trim() || null;
  }

  function defaultOrganisationRelation() {
    return {
      relationKind: 'Organisation',
      relationTarget: null,
      associationUiKind: 'organisation',
      projectId: null,
      primaryProductId: null,
      relationSourceLabel: 'Organisation',
      relationRelatedTitle: 'Organisation',
      relationRelatedDescription:
        'This is an organisational risk or issue. It is not linked to a work item or service register entry.',
      relationLinkHref: ''
    };
  }

  function applyRelationDataAttributes(td, rel) {
    td.setAttribute('data-association-kind', rel.associationUiKind || '');
    td.setAttribute('data-project-id', rel.projectId != null ? String(rel.projectId) : '');
    td.setAttribute('data-primary-product-id', rel.primaryProductId != null ? String(rel.primaryProductId) : '');
    td.setAttribute('data-relation-kind', rel.relationKind || 'Unknown');
    td.setAttribute('data-relation-source', rel.relationSourceLabel || '');
    td.setAttribute('data-relation-title', rel.relationRelatedTitle || rel.relationTarget || '');
    td.setAttribute('data-relation-description', rel.relationRelatedDescription || '');
    td.setAttribute('data-relation-href', rel.relationLinkHref || '');
  }

  function renderRelationCell(td, rel) {
    var data = rel || defaultOrganisationRelation();
    td.className = 'govuk-table__cell govuk-body-s raid-ss-relation-cell';
    td.setAttribute('data-type', 'relation');
    applyRelationDataAttributes(td, data);
    td.innerHTML = '';

    var wrap = document.createElement('div');
    wrap.className = 'raid-ss-relation-cell__content';

    var target = relationTargetName(data);
    var href = (data.relationLinkHref || '').trim();
    var canView = href && data.relationKind !== 'Organisation' && data.relationKind !== 'Unknown';

    if (canView && target) {
      var link = document.createElement('button');
      link.type = 'button';
      link.className = 'govuk-link govuk-link--no-visited-state raid-ss-relation-item-link raid-ss-relation-view-btn';
      link.textContent = target;
      link.setAttribute('aria-label', 'View details for ' + target);
      wrap.appendChild(link);
    } else if (data.relationKind === 'Organisation') {
      var orgEl = document.createElement('button');
      orgEl.type = 'button';
      orgEl.className = 'govuk-link govuk-link--no-visited-state raid-ss-relation-org-label raid-ss-relation-view-btn';
      orgEl.textContent = 'Not linked to a work item or service';
      orgEl.setAttribute('aria-label', 'View organisational relation details');
      wrap.appendChild(orgEl);
    } else if (target) {
      var nameEl = document.createElement('button');
      nameEl.type = 'button';
      nameEl.className = 'govuk-link govuk-link--no-visited-state raid-ss-relation-name raid-ss-relation-view-btn';
      nameEl.textContent = target;
      nameEl.setAttribute('aria-label', 'View relation details for ' + target);
      wrap.appendChild(nameEl);
    } else {
      var emptyEl = document.createElement('span');
      emptyEl.className = 'raid-ss-relation-empty';
      emptyEl.textContent = '—';
      wrap.appendChild(emptyEl);
    }

    var actions = document.createElement('div');
    actions.className = 'raid-ss-relation-actions';

    var changeBtn = document.createElement('button');
    changeBtn.type = 'button';
    changeBtn.className = 'govuk-link govuk-body-s raid-ss-relation-change-btn';
    changeBtn.textContent = 'Change';
    changeBtn.setAttribute('aria-label', 'Change related item');
    actions.appendChild(changeBtn);

    wrap.appendChild(actions);
    td.appendChild(wrap);
  }

  function appendRelationCell(td, rel) {
    renderRelationCell(td, rel);
  }

  function createCommentToggle(entityType, id, count) {
    var btn = document.createElement('button');
    btn.type = 'button';
    btn.className = 'raid-ss-comment-toggle';
    btn.setAttribute('data-entity', entityType);
    btn.setAttribute('data-id', id);
    btn.setAttribute('aria-expanded', 'false');

    var icon = document.createElement('span');
    icon.className = 'material-symbols-outlined raid-ss-comment-toggle__icon';
    icon.setAttribute('aria-hidden', 'true');
    icon.textContent = 'chat_bubble_outline';

    var countSpan = document.createElement('span');
    countSpan.className = 'raid-ss-comment-count';
    countSpan.setAttribute('aria-hidden', 'true');

    btn.appendChild(icon);
    btn.appendChild(countSpan);
    setCommentCount(btn, count || 0);
    return btn;
  }

  function appendRefCell(td, entityType, id, refText, commentCount) {
    td.classList.add('raid-ss-sticky-col');
    var wrap = document.createElement('div');
    wrap.className = 'raid-ss-ref-cell';

    var refLink = document.createElement('a');
    refLink.className = 'govuk-link govuk-link--no-visited-state raid-ss-ref-link';
    refLink.href = '#';
    var detailPath = entityType === 'nearmiss'
      ? 'near-misses'
      : entityType === 'assumption'
        ? 'assumptions'
        : entityType + 's';
    refLink.setAttribute('data-href', baseUrl + '/' + detailPath + '/' + id);
    refLink.textContent = refText;

    wrap.appendChild(refLink);
    wrap.appendChild(createCommentToggle(entityType, id, commentCount));
    td.appendChild(wrap);
  }

  var RISK_SCORE_COLS = {
    origScore: 'inherentScore',
    currScore: 'currentScore',
    residScore: 'residualScore',
    tolScore: 'toleranceScore'
  };

  function buildRiskCell(td, col, th, data) {
    if (col === 'title') {
      appendModalCell(td, 'title', 'Title', data.title);
    } else     if (col === 'status') {
      appendSelectCell(td, 'statusId', 'riskStatuses', data.statusId, data.status || 'Open', 'riskStatus');
    } else if (col === 'tier') {
      if (isSpreadsheetTierEditable(data.tierId)) {
        appendSelectCell(td, 'tierId', 'riskTiers', data.tierId, data.tier || 'Tier 3', 'tier');
      } else {
        appendBadgeValSpan(td, data.tier || '—', 'tier');
      }
    } else if (col === 'priority') {
      appendSelectCell(td, 'priorityId', 'riskPriorities', data.priorityId, data.priority, 'priority');
    } else if (col === 'relation') {
      appendRelationCell(td, data.relation);
    } else if (col === 'category') {
      appendSelectCell(td, 'categoryId', 'riskCategories', data.categoryId, data.category);
    } else if (col === 'owner') {
      appendUserLookupCell(td, 'ownerUserId', 'Owner', data.ownerUserId, data.owner);
    } else if (col === 'description') {
      appendModalCell(td, 'description', 'Description', data.description);
    } else if (col === 'cause') {
      appendModalCell(td, 'cause', 'Cause', data.cause);
    } else if (col === 'impact') {
      appendModalCell(td, 'impactIfRealised', 'Impact if realised', data.impactIfRealised);
    } else if (col === 'mitigations') {
      appendModalCell(td, 'response', 'Response / Mitigations', data.response);
    } else if (col === 'origImpact') {
      if (th.classList.contains('raid-ss-group-start')) td.classList.add('raid-ss-group-start');
      appendSelectCell(td, 'originalImpactId', 'riskImpactLevels', data.originalImpactId, data.originalImpact);
    } else if (col === 'origLikelihood') {
      appendSelectCell(td, 'originalLikelihoodId', 'riskLikelihoods', data.originalLikelihoodId, data.originalLikelihood);
    } else if (col === 'origScore') {
      appendScoreCell(td, RISK_SCORE_COLS.origScore, data.inherentScore);
    } else if (col === 'currImpact') {
      if (th.classList.contains('raid-ss-group-start')) td.classList.add('raid-ss-group-start');
      appendSelectCell(td, 'currentImpactId', 'riskImpactLevels', data.currentImpactId, data.currentImpact);
    } else if (col === 'currLikelihood') {
      appendSelectCell(td, 'currentLikelihoodId', 'riskLikelihoods', data.currentLikelihoodId, data.currentLikelihood);
    } else if (col === 'currScore') {
      appendScoreCell(td, RISK_SCORE_COLS.currScore, data.currentScore);
    } else if (col === 'residImpact') {
      if (th.classList.contains('raid-ss-group-start')) td.classList.add('raid-ss-group-start');
      appendSelectCell(td, 'residualImpactId', 'riskImpactLevels', data.residualImpactId, data.residualImpact);
    } else if (col === 'residLikelihood') {
      appendSelectCell(td, 'residualLikelihoodId', 'riskLikelihoods', data.residualLikelihoodId, data.residualLikelihood);
    } else if (col === 'residScore') {
      appendScoreCell(td, RISK_SCORE_COLS.residScore, data.residualScore);
    } else if (col === 'tolImpact') {
      if (th.classList.contains('raid-ss-group-start')) td.classList.add('raid-ss-group-start');
      appendSelectCell(td, 'toleranceImpactId', 'riskImpactLevels', data.toleranceImpactId, data.toleranceImpact);
    } else if (col === 'tolLikelihood') {
      appendSelectCell(td, 'toleranceLikelihoodId', 'riskLikelihoods', data.toleranceLikelihoodId, data.toleranceLikelihood);
    } else if (col === 'tolScore') {
      appendScoreCell(td, RISK_SCORE_COLS.tolScore, data.toleranceScore);
    } else if (col === 'proximity') {
      appendSelectCell(td, 'proximityId', 'riskProximities', data.proximityId, data.proximity);
    } else if (col === 'createdDate') {
      td.textContent = data.createdDate || '—';
    } else if (col === 'lastEdited') {
      td.classList.add('raid-ss-last-edited');
      if (data.updatedAtIso) td.setAttribute('data-updated', data.updatedAtIso);
      appendValSpan(td, data.updatedAt || '—');
    } else {
      appendValSpan(td, '—');
    }
  }

  function scopeDisplayText(rel) {
    if (!rel || rel.relationKind !== 'Organisation') return '—';
    return rel.relationTarget ? 'Organisation · ' + rel.relationTarget : 'Organisation';
  }

  function buildNearMissCell(td, col, data) {
    if (col === 'impact') {
      appendModalCell(td, 'impact', 'Impact', data.impact);
    } else if (col === 'status') {
      appendSelectCell(td, 'statusId', 'nearMissStatuses', data.statusId, data.status || 'Open');
    } else if (col === 'seriousness') {
      appendSelectCell(td, 'seriousnessId', 'nearMissSeriousnesses', data.seriousnessId, data.seriousness);
    } else if (col === 'type') {
      appendSelectCell(td, 'typeId', 'nearMissTypes', data.typeId, data.type);
    } else if (col === 'scope') {
      appendValSpan(td, scopeDisplayText(data.relation));
    } else if (col === 'dateLogged') {
      td.textContent = data.dateLogged || '—';
    } else if (col === 'lastEdited') {
      td.classList.add('raid-ss-last-edited');
      if (data.updatedAtIso) td.setAttribute('data-updated', data.updatedAtIso);
      appendValSpan(td, data.updatedAt || '—');
    } else {
      appendValSpan(td, '—');
    }
  }

  function buildAssumptionCell(td, col, data) {
    if (col === 'description') {
      appendModalCell(td, 'description', 'Description', data.description);
    } else if (col === 'status') {
      appendSelectCell(td, 'assumptionStatusId', 'assumptionStatuses', data.statusId, data.status || 'Open');
    } else if (col === 'criticality') {
      appendSelectCell(td, 'assumptionCriticalityId', 'assumptionCriticalities', data.criticalityId, data.criticality);
    } else if (col === 'relation') {
      appendRelationCell(td, data.relation);
    } else if (col === 'owner') {
      appendUserLookupCell(td, 'ownerUserId', 'Owner', data.ownerUserId, data.owner);
    } else if (col === 'createdDate') {
      td.textContent = data.createdDate || '—';
    } else if (col === 'lastEdited') {
      td.classList.add('raid-ss-last-edited');
      if (data.updatedAtIso) td.setAttribute('data-updated', data.updatedAtIso);
      appendValSpan(td, data.updatedAt || '—');
    } else {
      appendValSpan(td, '—');
    }
  }

  function buildIssueCell(td, col, data) {
    if (col === 'title') {
      appendModalCell(td, 'title', 'Title', data.title);
    } else     if (col === 'status') {
      appendSelectCell(td, 'statusId', 'issueStatuses', data.statusId, data.status || 'Open', 'issueStatus');
    } else if (col === 'severity') {
      appendSelectCell(td, 'severityId', 'issueSeverities', data.severityId, data.severity, 'severity');
    } else if (col === 'priority') {
      appendSelectCell(td, 'priorityId', 'issuePriorities', data.priorityId, data.priority, 'priority');
    } else if (col === 'relation') {
      appendRelationCell(td, data.relation);
    } else if (col === 'category') {
      appendSelectCell(td, 'categoryId', 'issueCategories', data.categoryId, data.category);
    } else if (col === 'owner') {
      appendUserLookupCell(td, 'ownerUserId', 'Owner', data.ownerUserId, data.owner);
    } else if (col === 'description') {
      appendModalCell(td, 'description', 'Description', data.description);
    } else if (col === 'identified') {
      td.textContent = data.identifiedDate || '—';
    } else if (col === 'targetDate') {
      td.textContent = data.targetDate || '—';
    } else if (col === 'lastEdited') {
      td.classList.add('raid-ss-last-edited');
      if (data.updatedAtIso) td.setAttribute('data-updated', data.updatedAtIso);
      appendValSpan(td, data.updatedAt || '—');
    } else {
      appendValSpan(td, '—');
    }
  }

  function buildNewDataRow(table, data, entityType) {
    var headerRow = table.querySelector('thead .raid-ss-header-row');
    if (!headerRow) return null;
    var headers = headerRow.querySelectorAll('th');
    var tr = document.createElement('tr');
    tr.className = 'govuk-table__row raid-ss-row';
    tr.setAttribute('data-entity', entityType);
    tr.setAttribute('data-id', data.id);

    var refText = data.reference || data.ref || formatEntityRef(entityType, data.id);

    headers.forEach(function (th) {
      var col = th.getAttribute('data-col');
      var td = document.createElement('td');
      td.className = 'govuk-table__cell';

      if (th.classList.contains('raid-ss-sticky-col')) {
        appendRefCell(td, entityType, data.id, refText, data.commentCount || 0);
      } else if (entityType === 'risk') {
        buildRiskCell(td, col, th, data);
      } else if (entityType === 'issue') {
        buildIssueCell(td, col, data);
      } else if (entityType === 'nearmiss') {
        buildNearMissCell(td, col, data);
      } else if (entityType === 'assumption') {
        buildAssumptionCell(td, col, data);
      } else {
        appendValSpan(td, '—');
      }
      tr.appendChild(td);
    });

    if (wrapEnabled) {
      applyWrapState();
    }

    return tr;
  }

  // ── Relation editor and view modal ──

  var relationSelectsPopulated = false;
  var relationEditContext = null;

  function ensureRelationSelectsPopulated() {
    if (relationSelectsPopulated) return;
    var projSel = document.getElementById('raid-relation-edit-project');
    var fipsSel = document.getElementById('raid-relation-edit-fips');
    if (!projSel || !fipsSel) return;
    (lookups.workItems || []).forEach(function (opt) {
      var o = document.createElement('option');
      o.value = opt.id;
      o.textContent = opt.name;
      projSel.appendChild(o);
    });
    (lookups.fipsProducts || []).forEach(function (opt) {
      var o = document.createElement('option');
      o.value = opt.id;
      o.textContent = opt.name;
      fipsSel.appendChild(o);
    });
    relationSelectsPopulated = true;
  }

  function updateRelationEditPanels() {
    var kind = document.querySelector('input[name="raid-relation-edit-kind"]:checked');
    var k = kind ? kind.value : '';
    var workWrap = document.getElementById('raid-relation-edit-work-wrap');
    var productWrap = document.getElementById('raid-relation-edit-product-wrap');
    if (workWrap) workWrap.style.display = k === 'work' ? '' : 'none';
    if (productWrap) productWrap.style.display = k === 'product' ? '' : 'none';
  }

  function closeRelationEditModal() {
    var modal = document.getElementById('raid-relation-edit-modal');
    if (!modal) return;
    modal.style.display = 'none';
    document.body.style.overflow = '';
    relationEditContext = null;
    var row = document.querySelector('.raid-ss-row-editing');
    if (row) row.classList.remove('raid-ss-row-editing');
  }

  function openRelationEditor(cell) {
    var row = cell.closest('.raid-ss-row');
    if (!row) return;
    var modal = document.getElementById('raid-relation-edit-modal');
    if (!modal) return;

    ensureRelationSelectsPopulated();

    var entity = row.getAttribute('data-entity');
    var id = row.getAttribute('data-id');
    var assocKind = cell.getAttribute('data-association-kind') || 'organisation';
    var projectId = cell.getAttribute('data-project-id') || '';
    var productId = cell.getAttribute('data-primary-product-id') || '';

    relationEditContext = { cell: cell, row: row, entity: entity, id: id };

    document.querySelectorAll('input[name="raid-relation-edit-kind"]').forEach(function (inp) {
      inp.checked = inp.value === assocKind;
    });
    var projSel = document.getElementById('raid-relation-edit-project');
    var fipsSel = document.getElementById('raid-relation-edit-fips');
    if (projSel) projSel.value = projectId;
    if (fipsSel) fipsSel.value = productId;
    updateRelationEditPanels();

    var errEl = document.getElementById('raid-relation-edit-error');
    if (errEl) errEl.style.display = 'none';

    row.classList.add('raid-ss-row-editing');
    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';
  }

  function bindRelationEditModal() {
    document.querySelectorAll('input[name="raid-relation-edit-kind"]').forEach(function (inp) {
      inp.addEventListener('change', updateRelationEditPanels);
    });

    var saveBtn = document.getElementById('raid-relation-edit-save');
    if (saveBtn) {
      saveBtn.addEventListener('click', function () {
        if (!relationEditContext) return;
        var kindInp = document.querySelector('input[name="raid-relation-edit-kind"]:checked');
        var associationKind = kindInp ? kindInp.value : '';
        var projectId = document.getElementById('raid-relation-edit-project')?.value || '';
        var primaryProductId = document.getElementById('raid-relation-edit-fips')?.value || '';
        var ctx = relationEditContext;
        var url = baseUrl + '/api/' + ctx.entity + '/' + ctx.id + '/association';

        saveBtn.disabled = true;
        saveBtn.textContent = 'Saving…';

        fetch(url, {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'RequestVerificationToken': csrfToken
          },
          body: JSON.stringify({
            associationKind: associationKind,
            projectId: projectId ? parseInt(projectId, 10) : null,
            primaryProductId: primaryProductId ? parseInt(primaryProductId, 10) : null
          })
        })
        .then(function (res) {
          if (!res.ok) {
            return res.json().then(function (d) { throw new Error(d.error || 'Save failed'); });
          }
          return res.json();
        })
        .then(function (rel) {
          renderRelationCell(ctx.cell, rel);
          ctx.row.classList.remove('raid-ss-row-editing');
          ctx.row.classList.add('raid-ss-saved');
          setTimeout(function () { ctx.row.classList.remove('raid-ss-saved'); }, 600);
          closeRelationEditModal();
        })
        .catch(function (err) {
          var errEl = document.getElementById('raid-relation-edit-error');
          if (errEl) {
            errEl.textContent = err.message || 'Could not save relation.';
            errEl.style.display = '';
          }
        })
        .finally(function () {
          saveBtn.disabled = false;
          saveBtn.textContent = 'Save';
        });
      });
    }

    document.querySelectorAll('[data-raid-relation-edit-close]').forEach(function (btn) {
      btn.addEventListener('click', closeRelationEditModal);
    });
  }

  function relationInfoFromCell(cell) {
    return {
      kind: cell.getAttribute('data-relation-kind') || '',
      source: cell.getAttribute('data-relation-source') || '',
      title: cell.getAttribute('data-relation-title') || '',
      description: cell.getAttribute('data-relation-description') || '',
      href: cell.getAttribute('data-relation-href') || ''
    };
  }

  function bindRelationLinks() {
    document.addEventListener('click', function (e) {
      var changeBtn = e.target.closest('.raid-ss-relation-change-btn');
      if (changeBtn) {
        e.preventDefault();
        e.stopPropagation();

        var editCell = changeBtn.closest('.raid-ss-relation-cell');
        if (!editCell) return;

        closeAllEditors();
        openRelationEditor(editCell);
        return;
      }

      var viewBtn = e.target.closest('.raid-ss-relation-view-btn');
      if (!viewBtn) return;

      var cell = viewBtn.closest('.raid-ss-relation-cell');
      if (!cell) return;

      e.preventDefault();
      openRelationModal(relationInfoFromCell(cell));
    });
  }

  function openRelationModal(info) {
    var modal = document.getElementById('raid-relation-modal');
    if (!modal) return;

    var isOrganisation = info.kind === 'Organisation';
    var href = (info.href || '').trim();
    var canOpen = !isOrganisation && href.length > 0;

    var titleEl = document.getElementById('raid-relation-modal-title');
    var sourceEl = document.getElementById('raid-relation-modal-source');
    var itemTitleEl = document.getElementById('raid-relation-modal-item-title');
    var descriptionEl = document.getElementById('raid-relation-modal-description');
    var titleRow = document.getElementById('raid-relation-modal-title-row');
    var descriptionRow = document.getElementById('raid-relation-modal-description-row');
    var promptEl = document.getElementById('raid-relation-modal-prompt');
    var yesBtn = document.getElementById('raid-relation-modal-yes');
    var closeBtn = document.getElementById('raid-relation-modal-close');
    var headerCloseBtn = modal.querySelector('.raid-modal-close');

    if (titleEl) {
      titleEl.textContent = isOrganisation ? 'Organisational item' : 'Related item';
    }
    if (sourceEl) {
      sourceEl.textContent = info.source || (isOrganisation ? 'Organisation' : '—');
    }

    if (isOrganisation) {
      if (titleRow) titleRow.style.display = 'none';
      if (descriptionRow) descriptionRow.style.display = '';
      if (itemTitleEl) itemTitleEl.textContent = '';
      if (descriptionEl) {
        descriptionEl.textContent = info.description ||
          'This is an organisational risk or issue. It is not linked to a work item or service register entry.';
      }
    } else {
      if (titleRow) titleRow.style.display = '';
      if (descriptionRow) descriptionRow.style.display = '';
      if (itemTitleEl) {
        itemTitleEl.textContent = info.title || '—';
      }
      if (descriptionEl) {
        var desc = (info.description || '').trim();
        descriptionEl.textContent = desc || 'No description available.';
        if (!desc) {
          descriptionEl.classList.add('dfe-c-text-muted');
        } else {
          descriptionEl.classList.remove('dfe-c-text-muted');
        }
      }
    }

    if (promptEl) {
      promptEl.hidden = !canOpen;
      promptEl.textContent = canOpen
        ? 'Open this related item in a new tab?'
        : '';
    }
    if (yesBtn) {
      yesBtn.hidden = !canOpen;
    }

    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';

    function cleanup() {
      yesBtn.replaceWith(yesBtn.cloneNode(true));
      closeBtn.replaceWith(closeBtn.cloneNode(true));
      headerCloseBtn.replaceWith(headerCloseBtn.cloneNode(true));
      modal.style.display = 'none';
      document.body.style.overflow = '';
    }

    var freshYes = document.getElementById('raid-relation-modal-yes');
    var freshClose = document.getElementById('raid-relation-modal-close');
    var freshHeaderClose = modal.querySelector('.raid-modal-close');

    if (canOpen && freshYes) {
      freshYes.addEventListener('click', function () {
        window.open(href, '_blank');
        cleanup();
      }, { once: true });
    }

    if (freshClose) {
      freshClose.addEventListener('click', function () { cleanup(); }, { once: true });
    }
    if (freshHeaderClose) {
      freshHeaderClose.addEventListener('click', function () { cleanup(); }, { once: true });
    }
  }

  // ── Reference link modal ──

  function bindRefLinks() {
    document.addEventListener('click', function (e) {
      var link = e.target.closest('.raid-ss-ref-link');
      if (!link) return;
      e.preventDefault();

      var href = link.getAttribute('data-href');
      var ref = link.textContent.trim();

      var modal = document.getElementById('raid-ref-modal');
      if (!modal) return;

      document.getElementById('raid-ref-modal-text').textContent =
        'Open ' + ref + ' in a new tab to view the full details?';
      modal.style.display = 'flex';
      document.body.style.overflow = 'hidden';

      var yesBtn = document.getElementById('raid-ref-modal-yes');
      var noBtn = document.getElementById('raid-ref-modal-no');
      var closeBtn = modal.querySelector('.raid-modal-close');

      function cleanup() {
        yesBtn.replaceWith(yesBtn.cloneNode(true));
        noBtn.replaceWith(noBtn.cloneNode(true));
        closeBtn.replaceWith(closeBtn.cloneNode(true));
        modal.style.display = 'none';
        document.body.style.overflow = '';
      }

      yesBtn.addEventListener('click', function () {
        window.open(href, '_blank');
        cleanup();
      }, { once: true });

      noBtn.addEventListener('click', function () { cleanup(); }, { once: true });
      closeBtn.addEventListener('click', function () { cleanup(); }, { once: true });
    });
  }

  // ── Detail modal ──

  function bindDetailButtons() {
    document.addEventListener('click', function (e) {
      var btn = e.target.closest('.raid-ss-detail-btn');
      if (!btn) return;

      var entity = btn.getAttribute('data-entity');
      var id = btn.getAttribute('data-id');
      openDetailModal(entity, id);
    });
  }

  function openDetailModal(entity, id) {
    var modal = document.getElementById('raid-detail-modal');
    var body = document.getElementById('raid-modal-body');
    var title = document.getElementById('raid-modal-title');
    var fullLink = document.getElementById('raid-modal-full-link');

    if (!modal) return;

    var row = document.querySelector('.raid-ss-row[data-entity="' + entity + '"][data-id="' + id + '"]');
    var cells = row ? row.querySelectorAll('.govuk-table__cell') : [];
    var html = '<dl class="govuk-summary-list govuk-summary-list--no-border">';
    var headers = row ? row.closest('table').querySelectorAll('thead .raid-ss-header-row th') : [];

    for (var i = 0; i < cells.length && i < headers.length; i++) {
      var headerText = headers[i].textContent.trim();
      if (!headerText) continue;
      var cellVal = cells[i].querySelector('.raid-ss-val');
      var text = cellVal ? cellVal.textContent.trim() : cells[i].textContent.trim();
      html += '<div class="govuk-summary-list__row">';
      html += '<dt class="govuk-summary-list__key">' + escapeHtml(headerText) + '</dt>';
      html += '<dd class="govuk-summary-list__value">' + escapeHtml(text) + '</dd>';
      html += '</div>';
    }
    html += '</dl>';

    title.textContent = (entity.charAt(0).toUpperCase() + entity.slice(1)) + ' #' + id;
    body.innerHTML = html;
    fullLink.href = baseUrl + '/' + entity + 's/' + id;
    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';
  }

  function closeModal() {
    var modal = document.getElementById('raid-detail-modal');
    if (modal) {
      modal.style.display = 'none';
      document.body.style.overflow = '';
    }
  }

  function closeTextModal() {
    var modal = document.getElementById('raid-text-modal');
    if (modal) {
      modal.style.display = 'none';
      document.body.style.overflow = '';
    }
    document.querySelectorAll('.raid-ss-row-editing').forEach(function (r) {
      r.classList.remove('raid-ss-row-editing');
    });
  }

  function bindModalClose() {
    document.addEventListener('click', function (e) {
      if (e.target.classList.contains('raid-modal-close') ||
          e.target.classList.contains('raid-modal-overlay')) {
        closeModal();
        closeTextModal();
      }
    });
  }

  function escapeHtml(str) {
    var div = document.createElement('div');
    div.textContent = str;
    return div.innerHTML;
  }

  // ── Fullscreen toggle ──

  function bindFullscreen() {
    document.addEventListener('click', function (e) {
      var btn = e.target.closest('.raid-ss-fullscreen-btn');
      if (!btn) return;
      toggleFullscreen();
    });
  }

  function toggleFullscreen() {
    var container = document.getElementById('raid-manage-section');
    if (!container) return;

    var isFS = container.classList.toggle('raid-register-fullscreen');
    document.querySelectorAll('.btn-fs-expand').forEach(function (el) { el.style.display = isFS ? 'none' : ''; });
    document.querySelectorAll('.btn-fs-collapse').forEach(function (el) { el.style.display = isFS ? '' : 'none'; });
    document.querySelectorAll('.raid-ss-fullscreen-btn').forEach(function (btn) {
      btn.classList.toggle('raid-ss-btn-active', isFS);
    });
    syncAllHeaderScrollMetrics();
  }

  // ── Column resizing ──

  function wrapHeaderLabel(th) {
    if (th.querySelector('.raid-ss-th-label')) return;
    var label = document.createElement('span');
    label.className = 'raid-ss-th-label';
    var handle = th.querySelector('.raid-ss-resize-handle');
    var node = th.firstChild;
    while (node) {
      var next = node.nextSibling;
      if (node !== handle && !(node.classList && node.classList.contains('raid-ss-resize-handle'))) {
        label.appendChild(node);
      }
      node = next;
    }
    if (handle) {
      th.insertBefore(label, handle);
    } else {
      th.appendChild(label);
    }
    if (!label.textContent.trim() && th.getAttribute('data-col')) {
      label.textContent = th.getAttribute('data-col');
    }
  }

  function riskScoreBandClass(score) {
    if (score == null || score === '' || isNaN(score)) return '';
    var s = parseFloat(score);
    if (s >= 20) return 'raid-ss-score-badge--highest';
    if (s >= 15) return 'raid-ss-score-badge--elevated';
    if (s >= 8) return 'raid-ss-score-badge--medium';
    return 'raid-ss-score-badge--lower';
  }

  function riskRefScoreIndicatorClass(score) {
    if (score == null || score === '' || isNaN(score)) return '';
    var s = parseFloat(score);
    if (s >= 20) return 'raid-ss-ref-score--highest';
    if (s >= 15) return 'raid-ss-ref-score--elevated';
    if (s >= 8) return 'raid-ss-ref-score--medium';
    return 'raid-ss-ref-score--lower';
  }

  function renderScoreBadgeCell(cell, score) {
    if (!cell) return;
    cell.innerHTML = '';
    if (score == null || score === '' || isNaN(parseFloat(score))) {
      var dash = document.createElement('span');
      dash.className = 'dfe-c-text-muted';
      dash.textContent = '—';
      cell.appendChild(dash);
      return;
    }
    var val = Math.round(parseFloat(score));
    var badge = document.createElement('span');
    badge.className = 'raid-ss-score-badge ' + riskScoreBandClass(score);
    badge.textContent = val + '/25';
    cell.appendChild(badge);
  }

  function applyRefScoreIndicator(row, currentScore) {
    var refCell = row.querySelector('.raid-ss-sticky-col');
    if (!refCell) return;
    refCell.classList.remove(
      'raid-ss-ref-score--highest',
      'raid-ss-ref-score--elevated',
      'raid-ss-ref-score--medium',
      'raid-ss-ref-score--lower'
    );
    var cls = riskRefScoreIndicatorClass(currentScore);
    if (cls) refCell.classList.add(cls);
  }

  function setupColumnResize() {
    document.querySelectorAll('.raid-spreadsheet-table').forEach(function (table) {
      var headers = table.querySelectorAll('thead .raid-ss-header-row th');
      headers.forEach(function (th) {
        wrapHeaderLabel(th);
        applyStickyHeaderCellStyles(th);
        if (!th.hasAttribute('tabindex')) th.setAttribute('tabindex', '0');
        var labelEl = th.querySelector('.raid-ss-th-label');
        var headerLabel = (labelEl ? labelEl.textContent : th.textContent).trim() || th.getAttribute('data-col') || 'column';
        var handle = document.createElement('div');
        handle.className = 'raid-ss-resize-handle';
        handle.setAttribute('role', 'separator');
        handle.setAttribute('aria-orientation', 'vertical');
        handle.setAttribute('aria-label', 'Resize ' + headerLabel + '. Drag or focus header and use Alt+Left/Right arrow.');
        var resizeIcon = document.createElement('span');
        resizeIcon.className = 'material-symbols-outlined';
        resizeIcon.setAttribute('aria-hidden', 'true');
        resizeIcon.textContent = 'drag_indicator';
        handle.appendChild(resizeIcon);
        th.appendChild(handle);

        var startX, startW;
        handle.addEventListener('mousedown', function (e) {
          e.preventDefault();
          e.stopPropagation();
          startX = e.pageX;
          startW = th.offsetWidth;
          handle.classList.add('raid-ss-resizing');

          function onMove(ev) {
            var newW = Math.max(50, startW + ev.pageX - startX);
            th.style.width = newW + 'px';
            th.style.minWidth = newW + 'px';
            var colIdx = Array.from(th.parentNode.children).indexOf(th);
            table.querySelectorAll('tbody tr').forEach(function (row) {
              var cell = row.children[colIdx];
              if (cell) {
                cell.style.width = newW + 'px';
                cell.style.minWidth = newW + 'px';
              }
            });
          }

          function onUp() {
            handle.classList.remove('raid-ss-resizing');
            document.removeEventListener('mousemove', onMove);
            document.removeEventListener('mouseup', onUp);
            saveColumnWidths(table);
          }

          document.addEventListener('mousemove', onMove);
          document.addEventListener('mouseup', onUp);
        });
      });
    });
  }

  function storageKey(table, suffix) {
    return STORAGE_PREFIX + registerId + '-' + table.id + '-' + suffix;
  }

  function saveColumnWidths(table) {
    var headers = table.querySelectorAll('thead .raid-ss-header-row th');
    var widths = {};
    headers.forEach(function (th) {
      var col = th.getAttribute('data-col');
      if (col && th.style.width) {
        widths[col] = parseInt(th.style.width);
      }
    });
    try {
      localStorage.setItem(storageKey(table, 'colWidths'), JSON.stringify(widths));
    } catch (e) { /* localStorage unavailable */ }
  }

  function restoreColumnWidths() {
    document.querySelectorAll('.raid-spreadsheet-table').forEach(function (table) {
      try {
        var stored = localStorage.getItem(storageKey(table, 'colWidths'));
        if (!stored) return;
        var widths = JSON.parse(stored);
        var headers = table.querySelectorAll('thead .raid-ss-header-row th');
        headers.forEach(function (th) {
          var col = th.getAttribute('data-col');
          if (col && widths[col]) {
            var w = widths[col] + 'px';
            th.style.width = w;
            th.style.minWidth = w;
            var colIdx = Array.from(th.parentNode.children).indexOf(th);
            table.querySelectorAll('tbody tr').forEach(function (row) {
              var cell = row.children[colIdx];
              if (cell) {
                cell.style.width = w;
                cell.style.minWidth = w;
              }
            });
          }
        });
      } catch (e) { /* parse error or localStorage unavailable */ }
    });
  }


  // ── Comments (ref cell toggle + viewport modals) ──

  var commentContext = null;

  function bindCommentButtons() {
    var commentsModal = document.getElementById('raid-comments-modal');
    var addModal = document.getElementById('raid-comment-add-modal');
    if (!commentsModal || !addModal) return;

    document.addEventListener('click', function (e) {
      var toggle = e.target.closest('.raid-ss-comment-toggle');
      if (!toggle) return;
      e.preventDefault();
      e.stopPropagation();

      var entity = toggle.getAttribute('data-entity');
      var id = toggle.getAttribute('data-id');
      var refCell = toggle.closest('.raid-ss-ref-cell');
      var refLink = refCell ? refCell.querySelector('.raid-ss-ref-link') : null;
      var refLabel = refLink ? refLink.textContent.trim() : '';
      openCommentsModal(entity, id, toggle, refLabel);
    });

    commentsModal.querySelectorAll('[data-raid-comments-close]').forEach(function (btn) {
      btn.addEventListener('click', closeCommentsModal);
    });

    addModal.querySelectorAll('[data-raid-comment-add-close]').forEach(function (btn) {
      btn.addEventListener('click', closeAddCommentModal);
    });

    var addOpenBtn = document.getElementById('raid-comments-modal-add');
    if (addOpenBtn) {
      addOpenBtn.addEventListener('click', openAddCommentModal);
    }

    var saveBtn = document.getElementById('raid-comment-add-save');
    var addInput = document.getElementById('raid-comment-add-input');
    if (saveBtn && addInput) {
      saveBtn.addEventListener('click', saveCommentFromModal);
      addInput.addEventListener('keydown', function (e) {
        if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) {
          e.preventDefault();
          saveCommentFromModal();
        }
      });
    }
  }

  function openCommentsModal(entity, id, toggle, refLabel) {
    var modal = document.getElementById('raid-comments-modal');
    var grid = document.getElementById('raid-comments-modal-grid');
    var titleEl = document.getElementById('raid-comments-modal-title');
    if (!modal || !grid) return;

    commentContext = { entity: entity, id: id, toggle: toggle };

    document.querySelectorAll('.raid-ss-comment-toggle--open').forEach(function (b) {
      b.classList.remove('raid-ss-comment-toggle--open');
      b.setAttribute('aria-expanded', 'false');
    });
    toggle.classList.add('raid-ss-comment-toggle--open');
    toggle.setAttribute('aria-expanded', 'true');

    if (titleEl) {
      titleEl.textContent = refLabel ? 'Comments on ' + refLabel : 'Comments';
    }
    grid.innerHTML = '<div class="raid-ss-comment-loading">Loading comments\u2026</div>';
    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';

    loadCommentsIntoGrid(entity, id, grid);
  }

  function closeCommentsModal() {
    var modal = document.getElementById('raid-comments-modal');
    if (modal) modal.style.display = 'none';
    if (commentContext && commentContext.toggle) {
      commentContext.toggle.classList.remove('raid-ss-comment-toggle--open');
      commentContext.toggle.setAttribute('aria-expanded', 'false');
    }
    commentContext = null;
    if (!isAddCommentModalOpen()) {
      document.body.style.overflow = '';
    }
  }

  function openAddCommentModal() {
    if (!commentContext) return;
    var modal = document.getElementById('raid-comment-add-modal');
    var input = document.getElementById('raid-comment-add-input');
    var titleEl = document.getElementById('raid-comment-add-modal-title');
    if (!modal || !input) return;

    if (titleEl && commentContext.toggle) {
      var refCell = commentContext.toggle.closest('.raid-ss-ref-cell');
      var refLink = refCell ? refCell.querySelector('.raid-ss-ref-link') : null;
      var refLabel = refLink ? refLink.textContent.trim() : '';
      titleEl.textContent = refLabel ? 'Add comment to ' + refLabel : 'Add comment';
    }

    input.value = '';
    modal.style.display = 'flex';
    input.focus();
  }

  function closeAddCommentModal() {
    var modal = document.getElementById('raid-comment-add-modal');
    if (modal) modal.style.display = 'none';
    if (!isCommentsModalOpen()) {
      document.body.style.overflow = '';
    }
  }

  function isCommentsModalOpen() {
    var modal = document.getElementById('raid-comments-modal');
    return modal && modal.style.display === 'flex';
  }

  function isAddCommentModalOpen() {
    var modal = document.getElementById('raid-comment-add-modal');
    return modal && modal.style.display === 'flex';
  }

  function saveCommentFromModal() {
    if (!commentContext) return;
    var input = document.getElementById('raid-comment-add-input');
    var saveBtn = document.getElementById('raid-comment-add-save');
    if (!input || !saveBtn) return;

    var text = input.value.trim();
    if (!text) { input.focus(); return; }

    var entity = commentContext.entity;
    var id = commentContext.id;
    var toggle = commentContext.toggle;

    saveBtn.disabled = true;
    saveBtn.textContent = 'Saving\u2026';

    fetch('/api/comments', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': csrfToken
      },
      body: JSON.stringify({ entityType: commentEntityType(entity), entityId: parseInt(id, 10), commentText: text })
    })
    .then(function (r) {
      if (!r.ok) throw new Error('Save failed');
      return r.json();
    })
    .then(function (comment) {
      saveBtn.disabled = false;
      saveBtn.textContent = 'Save comment';
      closeAddCommentModal();

      var current = parseInt(toggle.getAttribute('data-comment-count'), 10) || 0;
      setCommentCount(toggle, current + 1);

      var grid = document.getElementById('raid-comments-modal-grid');
      if (grid && isCommentsModalOpen()) {
        appendCommentToGrid(grid, comment, true);
        grid.scrollTop = 0;
      }
    })
    .catch(function () {
      saveBtn.disabled = false;
      saveBtn.textContent = 'Save comment';
    });
  }

  function loadCommentsIntoGrid(entity, id, grid) {
    fetch('/api/comments/' + commentEntityType(entity) + '/' + id)
      .then(function (r) { return r.json(); })
      .then(function (comments) {
        grid.innerHTML = '';
        if (!comments || comments.length === 0) {
          grid.innerHTML = '<div class="raid-ss-comment-empty">No comments yet. Use Add comment to leave the first note.</div>';
          return;
        }
        comments.slice().sort(function (a, b) {
          return new Date(b.createdAt) - new Date(a.createdAt);
        }).forEach(function (c) {
          appendCommentToGrid(grid, c);
        });
        grid.scrollTop = 0;
      })
      .catch(function () {
        grid.innerHTML = '<div class="raid-ss-comment-empty">Failed to load comments.</div>';
      });
  }

  function appendCommentToGrid(grid, comment, prepend) {
    var empty = grid.querySelector('.raid-ss-comment-empty, .raid-ss-comment-loading');
    if (empty) empty.remove();

    var div = document.createElement('div');
    div.className = 'raid-ss-comment-item';
    div.setAttribute('role', 'listitem');
    div.setAttribute('data-comment-id', comment.id);

    var meta = document.createElement('div');
    meta.className = 'raid-ss-comment-meta';
    var who = comment.createdByUser ? comment.createdByUser.name || comment.createdByUser.email : 'Unknown';
    var when = comment.createdAt
      ? new Date(comment.createdAt).toLocaleString('en-GB', { day: '2-digit', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit' })
      : '';
    meta.textContent = who + ' \u00b7 ' + when;

    var text = document.createElement('div');
    text.className = 'raid-ss-comment-text';
    text.textContent = comment.commentText;

    div.appendChild(meta);
    div.appendChild(text);

    var firstItem = grid.querySelector('.raid-ss-comment-item');
    if (prepend && firstItem) {
      grid.insertBefore(div, firstItem);
    } else {
      grid.appendChild(div);
    }
  }

  function setCommentCount(toggle, count) {
    var span = toggle.querySelector('.raid-ss-comment-count');
    if (!span) return;
    var n = Math.max(0, count);
    span.textContent = String(n);
    span.classList.toggle('raid-ss-comment-count--visible', n > 0);
    toggle.classList.toggle('raid-ss-comment-toggle--has-comments', n > 0);
    toggle.setAttribute('data-comment-count', String(n));
    toggle.title = n > 0 ? n + ' comment' + (n === 1 ? '' : 's') : 'View comments';
  }

  function capitalise(str) {
    return str.charAt(0).toUpperCase() + str.slice(1);
  }

  // ── Word wrap toggle ──

  var TEXT_COLS = ['title', 'description', 'cause', 'impact', 'impactIfRealised', 'mitigations', 'response'];
  var wrapEnabled = false;

  function bindWrapToggle() {
    document.querySelectorAll('.raid-ss-wrap-toggle-btn').forEach(function (btn) {
      btn.addEventListener('click', function () {
        wrapEnabled = !wrapEnabled;
        btn.classList.toggle('raid-ss-btn-active', wrapEnabled);
        applyWrapState();
        try {
          localStorage.setItem(STORAGE_PREFIX + registerId + '-wrapText', wrapEnabled ? '1' : '0');
        } catch (e) { }
      });
    });
  }

  function restoreWrapState() {
    try {
      var stored = localStorage.getItem(STORAGE_PREFIX + registerId + '-wrapText');
      if (stored === '1') {
        wrapEnabled = true;
        document.querySelectorAll('.raid-ss-wrap-toggle-btn').forEach(function (btn) {
          btn.classList.add('raid-ss-btn-active');
        });
        applyWrapState();
      }
    } catch (e) { }
  }

  function applyWrapState() {
    document.querySelectorAll('.raid-spreadsheet-table').forEach(function (table) {
      var headerRow = table.querySelector('thead .raid-ss-header-row');
      if (!headerRow) return;
      var headers = headerRow.querySelectorAll('th');
      headers.forEach(function (th, colIdx) {
        var col = th.getAttribute('data-col');
        if (!col || TEXT_COLS.indexOf(col) === -1) return;
        table.querySelectorAll('tbody tr').forEach(function (row) {
          var cell = row.children[colIdx];
          if (!cell) return;
          var val = cell.querySelector('.raid-ss-val');
          if (val) {
            if (wrapEnabled) {
              val.classList.remove('raid-ss-val-truncate');
            } else {
              val.classList.add('raid-ss-val-truncate');
            }
          }
        });
      });
    });
  }

  // ── Column reorder via drag-and-drop ──

  var reorderMode = false;

  function bindReorderToggle() {
    document.querySelectorAll('.raid-ss-reorder-btn').forEach(function (btn) {
      btn.addEventListener('click', function () {
        reorderMode = !reorderMode;
        btn.classList.toggle('raid-ss-btn-active', reorderMode);
        document.querySelectorAll('.raid-spreadsheet-table').forEach(function (table) {
          var headers = table.querySelectorAll('thead .raid-ss-header-row th');
          headers.forEach(function (th) {
            if (th.classList.contains('raid-ss-sticky-col')) return;
            var lastTh = headers[headers.length - 1];
            if (th === lastTh) return;
            th.draggable = reorderMode;
            th.classList.toggle('raid-ss-draggable', reorderMode);
          });
        });
      });
    });

    document.addEventListener('dragstart', function (e) {
      var th = e.target.closest && e.target.closest('th.raid-ss-draggable');
      if (!th || !reorderMode) return;
      var table = th.closest('table');
      var headerRow = th.closest('.raid-ss-header-row');
      var colIdx = Array.from(headerRow.children).indexOf(th);
      e.dataTransfer.effectAllowed = 'move';
      e.dataTransfer.setData('text/plain', colIdx);
      th.classList.add('raid-ss-dragging');
      table._dragSourceIdx = colIdx;
    });

    document.addEventListener('dragover', function (e) {
      var th = e.target.closest && e.target.closest('th.raid-ss-draggable');
      if (!th) return;
      e.preventDefault();
      e.dataTransfer.dropEffect = 'move';
      document.querySelectorAll('.raid-ss-drag-over').forEach(function (el) {
        el.classList.remove('raid-ss-drag-over');
      });
      th.classList.add('raid-ss-drag-over');
    });

    document.addEventListener('dragleave', function (e) {
      var th = e.target.closest && e.target.closest('th.raid-ss-draggable');
      if (th) th.classList.remove('raid-ss-drag-over');
    });

    document.addEventListener('drop', function (e) {
      var th = e.target.closest && e.target.closest('th.raid-ss-draggable');
      if (!th) return;
      e.preventDefault();
      var table = th.closest('table');
      var headerRow = th.closest('.raid-ss-header-row');
      var fromIdx = table._dragSourceIdx;
      var toIdx = Array.from(headerRow.children).indexOf(th);
      if (fromIdx === undefined || fromIdx === toIdx) return;

      moveColumn(table, fromIdx, toIdx);
      saveColumnOrder(table);
      rebuildFilterRow(table);

      document.querySelectorAll('.raid-ss-dragging').forEach(function (el) {
        el.classList.remove('raid-ss-dragging');
      });
      document.querySelectorAll('.raid-ss-drag-over').forEach(function (el) {
        el.classList.remove('raid-ss-drag-over');
      });
      delete table._dragSourceIdx;
    });

    document.addEventListener('dragend', function () {
      document.querySelectorAll('.raid-ss-dragging').forEach(function (el) {
        el.classList.remove('raid-ss-dragging');
      });
      document.querySelectorAll('.raid-ss-drag-over').forEach(function (el) {
        el.classList.remove('raid-ss-drag-over');
      });
    });
  }

  function moveColumn(table, fromIdx, toIdx) {
    var allRows = table.querySelectorAll('thead tr, tbody tr, tfoot tr');
    allRows.forEach(function (row) {
      var cells = Array.from(row.children);
      if (fromIdx >= cells.length || toIdx >= cells.length) return;
      var moving = cells[fromIdx];
      var target = cells[toIdx];
      if (fromIdx < toIdx) {
        row.insertBefore(moving, target.nextSibling);
      } else {
        row.insertBefore(moving, target);
      }
    });
  }

  function saveColumnOrder(table) {
    var headerRow = table.querySelector('thead .raid-ss-header-row');
    var order = [];
    headerRow.querySelectorAll('th').forEach(function (th) {
      var col = th.getAttribute('data-col');
      order.push(col || '');
    });
    try {
      localStorage.setItem(storageKey(table, 'colOrder'), JSON.stringify(order));
    } catch (e) { }
  }

  function restoreColumnOrder() {
    document.querySelectorAll('.raid-spreadsheet-table').forEach(function (table) {
      try {
        var stored = localStorage.getItem(storageKey(table, 'colOrder'));
        if (!stored) return;
        var order = JSON.parse(stored);
        var headerRow = table.querySelector('thead .raid-ss-header-row');
        var currentHeaders = headerRow.querySelectorAll('th');
        if (order.length !== currentHeaders.length) return;

        var currentOrder = [];
        currentHeaders.forEach(function (th) {
          currentOrder.push(th.getAttribute('data-col') || '');
        });

        var orderMatch = order.every(function (col, i) { return col === currentOrder[i]; });
        if (orderMatch) return;

        for (var i = 0; i < order.length; i++) {
          var col = order[i];
          var currentIdx = -1;
          var headers = Array.from(headerRow.querySelectorAll('th'));
          for (var j = i; j < headers.length; j++) {
            var hCol = headers[j].getAttribute('data-col') || '';
            if (hCol === col) { currentIdx = j; break; }
          }
          if (currentIdx > i) {
            moveColumn(table, currentIdx, i);
          }
        }

        rebuildFilterRow(table);
      } catch (e) { }
    });
  }

  function rebuildFilterRow(table) {
    var filterRow = table.querySelector('.raid-ss-filter-row');
    if (!filterRow) return;
    var headerRow = filterRow.previousElementSibling;
    if (!headerRow) return;
    var headers = headerRow.querySelectorAll('th');
    var wasVisible = filterRow.style.display !== 'none';
    filterRow.innerHTML = '';

    headers.forEach(function (th) {
      var td = document.createElement('td');
      td.className = 'govuk-table__cell raid-ss-filter-cell';
      td.style.padding = '4px 6px';
      populateFilterCell(td, th, table, headerRow);

      if (th.classList.contains('raid-ss-sticky-col')) {
        td.classList.add('raid-ss-sticky-col');
      }

      filterRow.appendChild(td);
    });

    filterRow.style.display = wasVisible ? '' : 'none';
    syncHeaderScrollMetrics(table);
  }

  function applyStickyHeaderCellStyles(th) {
    th.style.position = 'sticky';
    th.style.top = '0';
    if (th.classList.contains('raid-ss-sticky-col')) {
      th.style.left = '0';
      th.style.zIndex = '10';
    } else {
      th.style.left = '';
      th.style.zIndex = '5';
    }
  }

  function syncHeaderScrollMetrics(table) {
    var scroll = table.closest('.raid-spreadsheet-scroll');
    var headerRow = table.querySelector('thead .raid-ss-header-row');
    if (!scroll || !headerRow) return;
    scroll.style.setProperty('--raid-ss-header-height', headerRow.offsetHeight + 'px');
  }

  function syncAllHeaderScrollMetrics() {
    document.querySelectorAll('.raid-spreadsheet-table').forEach(syncHeaderScrollMetrics);
  }

  function getTableScrollWrapper(table) {
    return table ? table.closest('.raid-spreadsheet-scroll') : null;
  }

  function getActiveSpreadsheetTable() {
    var panel = document.querySelector('.raid-ss-tab-panel.raid-ss-tab-active');
    if (panel) {
      var t = panel.querySelector('.raid-spreadsheet-table');
      if (t) return t;
    }
    return document.querySelector('.raid-spreadsheet-table');
  }

  function getReorderableHeaders(table) {
    var headerRow = table.querySelector('thead .raid-ss-header-row');
    if (!headerRow) return [];
    return Array.from(headerRow.querySelectorAll('th')).filter(function (th) {
      if (th.classList.contains('raid-ss-sticky-col')) return false;
      if (!th.getAttribute('data-col')) return false;
      return true;
    });
  }

  function bindAccessibleReorder() {
    var modal = document.getElementById('raid-reorder-a11y-modal');
    if (!modal) return;

    var select = document.getElementById('raid-reorder-a11y-select');
    var live = document.getElementById('raid-reorder-a11y-live');
    var upBtn = document.getElementById('raid-reorder-a11y-up');
    var downBtn = document.getElementById('raid-reorder-a11y-down');
    var doneBtn = document.getElementById('raid-reorder-a11y-done');

    function closeModal() {
      modal.style.display = 'none';
      document.body.style.overflow = '';
      a11yReorderTable = null;
    }

    function refreshSelect() {
      if (!a11yReorderTable || !select) return;
      var headers = getReorderableHeaders(a11yReorderTable);
      var current = select.value;
      select.innerHTML = '';
      headers.forEach(function (th) {
        var opt = document.createElement('option');
        opt.value = th.getAttribute('data-col');
        opt.textContent = th.textContent.trim();
        select.appendChild(opt);
      });
      if (current) select.value = current;
      if (!select.value && select.options.length) select.selectedIndex = 0;
    }

    function moveSelected(direction) {
      if (!a11yReorderTable || !select || !select.value) return;
      var headerRow = a11yReorderTable.querySelector('thead .raid-ss-header-row');
      var headers = getReorderableHeaders(a11yReorderTable);
      var col = select.value;
      var idx = headers.findIndex(function (th) { return th.getAttribute('data-col') === col; });
      if (idx < 0) return;
      var targetIdx = direction === 'up' ? idx - 1 : idx + 1;
      if (targetIdx < 0 || targetIdx >= headers.length) return;
      var fromIdx = Array.from(headerRow.children).indexOf(headers[idx]);
      var toIdx = Array.from(headerRow.children).indexOf(headers[targetIdx]);
      moveColumn(a11yReorderTable, fromIdx, toIdx);
      saveColumnOrder(a11yReorderTable);
      rebuildFilterRow(a11yReorderTable);
      refreshSelect();
      select.value = col;
      if (live) live.textContent = select.options[select.selectedIndex].text + ' moved ' + direction + '.';
    }

    function openModal() {
      a11yReorderTable = getActiveSpreadsheetTable();
      if (!a11yReorderTable) return;
      refreshSelect();
      modal.style.display = 'flex';
      document.body.style.overflow = 'hidden';
      if (select) select.focus();
    }

    document.querySelectorAll('.raid-ss-reorder-a11y-btn').forEach(function (btn) {
      btn.addEventListener('click', openModal);
    });

    if (upBtn) upBtn.addEventListener('click', function () { moveSelected('up'); });
    if (downBtn) downBtn.addEventListener('click', function () { moveSelected('down'); });
    if (doneBtn) doneBtn.addEventListener('click', closeModal);
    modal.querySelectorAll('.raid-modal-close').forEach(function (btn) {
      btn.addEventListener('click', closeModal);
    });
  }

  function resizeColumnByDelta(table, th, delta) {
    var newW = Math.max(50, th.offsetWidth + delta);
    th.style.width = newW + 'px';
    th.style.minWidth = newW + 'px';
    var colIdx = Array.from(th.parentNode.children).indexOf(th);
    table.querySelectorAll('tbody tr, .raid-ss-filter-row').forEach(function (row) {
      var cell = row.children[colIdx];
      if (cell) {
        cell.style.width = newW + 'px';
        cell.style.minWidth = newW + 'px';
      }
    });
    saveColumnWidths(table);
  }

  function setupHeaderKeyboardResize() {
    document.querySelectorAll('.raid-spreadsheet-table thead .raid-ss-header-row th').forEach(function (th) {
      if (!th.hasAttribute('tabindex')) th.setAttribute('tabindex', '0');
      th.addEventListener('keydown', function (e) {
        if (!e.altKey || (e.key !== 'ArrowLeft' && e.key !== 'ArrowRight')) return;
        var table = th.closest('table');
        if (!table) return;
        e.preventDefault();
        resizeColumnByDelta(table, th, e.key === 'ArrowRight' ? 20 : -20);
      });
    });
  }

  // ── Table zoom and reset ──

  var ZOOM_DEFAULT = 1;
  var ZOOM_MIN = 0.75;
  var ZOOM_MAX = 1.5;
  var ZOOM_STEP = 0.1;

  function captureDefaultColumnOrders() {
    document.querySelectorAll('.raid-spreadsheet-table').forEach(function (table) {
      if (table.dataset.defaultColOrder) return;
      var order = [];
      table.querySelectorAll('thead .raid-ss-header-row th').forEach(function (th) {
        order.push(th.getAttribute('data-col') || '');
      });
      table.dataset.defaultColOrder = JSON.stringify(order);
    });
  }

  function getTableZoom(table) {
    var wrapper = getTableScrollWrapper(table);
    if (!wrapper) return ZOOM_DEFAULT;
    var z = parseFloat(wrapper.style.zoom);
    return isNaN(z) || z <= 0 ? ZOOM_DEFAULT : z;
  }

  function applyTableZoom(table, zoom) {
    var wrapper = getTableScrollWrapper(table);
    if (!wrapper) return ZOOM_DEFAULT;
    var level = Math.round(zoom * 100) / 100;
    level = Math.max(ZOOM_MIN, Math.min(ZOOM_MAX, level));
    wrapper.style.zoom = level === ZOOM_DEFAULT ? '' : String(level);
    try {
      if (level === ZOOM_DEFAULT) {
        localStorage.removeItem(storageKey(table, 'zoom'));
      } else {
        localStorage.setItem(storageKey(table, 'zoom'), String(level));
      }
    } catch (e) { /* localStorage unavailable */ }
    syncHeaderScrollMetrics(table);
    return level;
  }

  function restoreTableZoom() {
    document.querySelectorAll('.raid-spreadsheet-table').forEach(function (table) {
      table.style.zoom = '';
      try {
        var stored = localStorage.getItem(storageKey(table, 'zoom'));
        if (stored) {
          var level = parseFloat(stored);
          if (!isNaN(level)) applyTableZoom(table, level);
        }
      } catch (e) { /* parse error */ }
    });
  }

  function clearColumnWidths(table) {
    try {
      localStorage.removeItem(storageKey(table, 'colWidths'));
    } catch (e) { /* localStorage unavailable */ }
    table.querySelectorAll('thead .raid-ss-header-row th, tbody tr td, .raid-ss-filter-row td').forEach(function (cell) {
      cell.style.width = '';
      cell.style.minWidth = '';
      cell.style.maxWidth = '';
    });
  }

  function resetColumnOrderToDefault(table) {
    var orderJson = table.dataset.defaultColOrder;
    if (!orderJson) return;
    try {
      var order = JSON.parse(orderJson);
      var headerRow = table.querySelector('thead .raid-ss-header-row');
      if (!headerRow || !order.length) return;

      for (var i = 0; i < order.length; i++) {
        var col = order[i];
        var headers = Array.from(headerRow.querySelectorAll('th'));
        var currentIdx = -1;
        for (var j = i; j < headers.length; j++) {
          var hCol = headers[j].getAttribute('data-col') || '';
          if (hCol === col) { currentIdx = j; break; }
        }
        if (currentIdx > i) {
          moveColumn(table, currentIdx, i);
        }
      }

      try {
        localStorage.removeItem(storageKey(table, 'colOrder'));
      } catch (e) { /* localStorage unavailable */ }
      rebuildFilterRow(table);
    } catch (e) { /* parse error */ }
  }

  function resetTableView(table) {
    if (!table) return;
    applyTableZoom(table, ZOOM_DEFAULT);
    clearColumnWidths(table);
    resetColumnOrderToDefault(table);
    syncHeaderScrollMetrics(table);
  }

  function bindClearFiltersButtons() {
    document.querySelectorAll('.raid-ss-clear-filters-btn').forEach(function (btn) {
      btn.addEventListener('click', clearFilters);
    });
  }

  function bindZoomControls() {
    document.querySelectorAll('.raid-ss-zoom-in-btn').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var table = getActiveSpreadsheetTable();
        if (!table) return;
        applyTableZoom(table, getTableZoom(table) + ZOOM_STEP);
      });
    });

    document.querySelectorAll('.raid-ss-zoom-out-btn').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var table = getActiveSpreadsheetTable();
        if (!table) return;
        applyTableZoom(table, getTableZoom(table) - ZOOM_STEP);
      });
    });
  }

  var resetPendingTable = null;

  function openResetTableModal(table) {
    resetPendingTable = table;
    var modal = document.getElementById('raid-reset-table-modal');
    if (!modal) return;
    modal.style.display = 'flex';
    document.body.style.overflow = 'hidden';
    var confirmBtn = document.getElementById('raid-reset-table-confirm');
    if (confirmBtn) confirmBtn.focus();
  }

  function closeResetTableModal() {
    resetPendingTable = null;
    var modal = document.getElementById('raid-reset-table-modal');
    if (modal) modal.style.display = 'none';
    document.body.style.overflow = '';
  }

  function bindResetTableModal() {
    document.querySelectorAll('.raid-ss-reset-table-btn').forEach(function (btn) {
      btn.addEventListener('click', function () {
        var table = getActiveSpreadsheetTable();
        if (!table) return;
        openResetTableModal(table);
      });
    });

    var confirmBtn = document.getElementById('raid-reset-table-confirm');
    if (confirmBtn) {
      confirmBtn.addEventListener('click', function () {
        if (resetPendingTable) resetTableView(resetPendingTable);
        closeResetTableModal();
      });
    }

    document.querySelectorAll('[data-raid-reset-table-close]').forEach(function (btn) {
      btn.addEventListener('click', closeResetTableModal);
    });

    var modal = document.getElementById('raid-reset-table-modal');
    if (modal) {
      modal.addEventListener('click', function (e) {
        if (e.target === modal) closeResetTableModal();
      });
    }
  }

  // ── Public API ──
  window.RaidSpreadsheet = { init: init, clearFilters: clearFilters, resetTable: resetTableView };
})();
