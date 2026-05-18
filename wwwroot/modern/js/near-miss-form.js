/**
 * Near miss create/edit — owners, actions and mitigations as add-to-table collections.
 */
(function () {
  'use strict';

  function escapeHtml(s) {
    if (s == null) return '';
    return String(s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function readGovUkDate(prefix) {
    var dayEl = document.getElementById(prefix + '-day');
    var monthEl = document.getElementById(prefix + '-month');
    var yearEl = document.getElementById(prefix + '-year');
    if (!dayEl || !monthEl || !yearEl) return null;
    var day = parseInt(dayEl.value.trim(), 10);
    var month = parseInt(monthEl.value.trim(), 10);
    var year = parseInt(yearEl.value.trim(), 10);
    if (!day || !month || !year) return null;
    return { day: day, month: month, year: year };
  }

  function clearGovUkDate(prefix) {
    ['day', 'month', 'year'].forEach(function (part) {
      var el = document.getElementById(prefix + '-' + part);
      if (el) el.value = '';
    });
  }

  function readUserPicker(root) {
    if (!root) return null;
    var scope = root.querySelector('.user-picker') || root;
    var hidden = scope.querySelector('.js-user-picker-value');
    var nameEl = scope.querySelector('.js-user-picker-input');
    var id = hidden && hidden.value ? parseInt(hidden.value, 10) : 0;
    if (!id) return null;
    return { id: id, name: nameEl ? nameEl.value.trim() : '' };
  }

  function toggleEmptyHint(tbodyId, hintId) {
    var tbody = document.getElementById(tbodyId);
    var hint = document.getElementById(hintId);
    if (!tbody || !hint) return;
    var hasRows = tbody.querySelectorAll('tr[data-collection-row]').length > 0;
    hint.hidden = hasRows;
  }

  function clearUserPicker(root) {
    if (!root) return;
    var scope = root.querySelector('.user-picker') || root;
    var clear = scope.querySelector('.js-user-picker-clear');
    if (clear) clear.click();
    else {
      var hidden = scope.querySelector('.js-user-picker-value');
      var input = scope.querySelector('.js-user-picker-input');
      if (hidden) hidden.value = '';
      if (input) input.value = '';
      var summary = scope.querySelector('.js-user-picker-summary');
      if (summary) {
        var def = summary.getAttribute('data-default-text') || 'No user selected.';
        summary.textContent = def;
      }
    }
  }

  function reindexRows(tbody, prefix, fields) {
    var rows = tbody.querySelectorAll('tr[data-collection-row]');
    rows.forEach(function (row, i) {
      fields.forEach(function (field) {
        var el = row.querySelector('[data-field="' + field + '"]');
        if (el) el.name = prefix + '[' + i + '].' + field;
      });
    });
  }

  function initOwners(root) {
    var tbody = document.getElementById('nm-owners-tbody');
    var addBtn = document.getElementById('nm-owner-add-btn');
    var pickerHost = document.getElementById('nm-owner-add-picker');
    if (!tbody || !addBtn || !pickerHost) return;

    var initial = [];
    try {
      initial = JSON.parse(root.getAttribute('data-owners') || '[]');
    } catch (e) { /* ignore */ }

    function ownerIds() {
      return Array.from(tbody.querySelectorAll('input[data-field="OwnerUserIds"]')).map(function (i) {
        return parseInt(i.value, 10);
      });
    }

    function addOwnerRow(user) {
      if (!user || !user.id) return;
      if (ownerIds().indexOf(user.id) >= 0) return;
      var tr = document.createElement('tr');
      tr.setAttribute('data-collection-row', '1');
      tr.innerHTML =
        '<td class="govuk-table__cell">' + escapeHtml(user.name || 'User') + '</td>' +
        '<td class="govuk-table__cell govuk-table__cell--numeric">' +
        '<button type="button" class="govuk-link nm-remove-row">Remove</button>' +
        '<input type="hidden" data-field="OwnerUserIds" name="OwnerUserIds" value="' + user.id + '" />' +
        '</td>';
      tbody.appendChild(tr);
      toggleEmptyHint('nm-owners-tbody', 'nm-owners-empty');
    }

    initial.forEach(addOwnerRow);
    toggleEmptyHint('nm-owners-tbody', 'nm-owners-empty');

    addBtn.addEventListener('click', function (e) {
      e.preventDefault();
      var user = readUserPicker(pickerHost);
      if (!user) {
        alert('Select a user before adding to the list.');
        return;
      }
      addOwnerRow({ id: user.id, name: user.name || 'User' });
      clearUserPicker(pickerHost);
    });

    tbody.addEventListener('click', function (e) {
      if (e.target.classList.contains('nm-remove-row')) {
        e.preventDefault();
        var row = e.target.closest('tr');
        if (row) {
          row.remove();
          toggleEmptyHint('nm-owners-tbody', 'nm-owners-empty');
        }
      }
    });
  }

  function initDatedCollection(opts) {
    var tbody = document.getElementById(opts.tbodyId);
    var addBtn = document.getElementById(opts.addBtnId);
    var datePrefix = opts.dateFieldPrefix;
    var textInput = document.getElementById(opts.textInputId);
    var pickerHost = document.getElementById(opts.pickerHostId);
    var root = document.getElementById('near-miss-form-root');
    if (!tbody || !addBtn) return;

    var initial = [];
    try {
      initial = JSON.parse(root.getAttribute(opts.dataAttr) || '[]');
    } catch (e) { /* ignore */ }

    var currentUserId = parseInt(root.getAttribute('data-current-user-id') || '0', 10) || null;
    var currentUserName = root.getAttribute('data-current-user-name') || '';

    function addHidden(row, field, value) {
      var input = document.createElement('input');
      input.type = 'hidden';
      input.setAttribute('data-field', field);
      input.value = value == null ? '' : String(value);
      row.querySelector('td:last-child').appendChild(input);
    }

    function addRow(data) {
      var d = data.day, m = data.month, y = data.year;
      var text = data.text || '';
      var recId = data.recordedByUserId || currentUserId || '';
      var recName = data.recordedByName || currentUserName || '—';
      var tr = document.createElement('tr');
      tr.setAttribute('data-collection-row', '1');
      var dateDisplay = d && m && y ? (String(d).padStart(2, '0') + '/' + String(m).padStart(2, '0') + '/' + y) : '—';
      tr.innerHTML =
        '<td class="govuk-table__cell">' + escapeHtml(dateDisplay) + '</td>' +
        '<td class="govuk-table__cell nm-collection-text"></td>' +
        '<td class="govuk-table__cell">' + escapeHtml(recName) + '</td>' +
        '<td class="govuk-table__cell govuk-table__cell--numeric">' +
        '<button type="button" class="govuk-link nm-remove-row">Remove</button>' +
        '</td>';
      tr.querySelector('.nm-collection-text').textContent = text;
      addHidden(tr, opts.dayField, d || '');
      addHidden(tr, opts.monthField, m || '');
      addHidden(tr, opts.yearField, y || '');
      addHidden(tr, opts.textField, text);
      addHidden(tr, opts.recordedByField, recId);
      tbody.appendChild(tr);
      toggleEmptyHint(opts.tbodyId, opts.emptyHintId);
    }

    initial.forEach(addRow);
    toggleEmptyHint(opts.tbodyId, opts.emptyHintId);

    addBtn.addEventListener('click', function (e) {
      e.preventDefault();
      var parts = readGovUkDate(datePrefix);
      var text = textInput ? textInput.value.trim() : '';
      if (!parts || !text) {
        alert('Enter a date and ' + opts.textLabel + ' before adding to the list.');
        return;
      }
      var rec = readUserPicker(pickerHost);
      addRow({
        day: parts.day,
        month: parts.month,
        year: parts.year,
        text: text,
        recordedByUserId: rec ? rec.id : currentUserId,
        recordedByName: rec ? rec.name : currentUserName
      });
      if (datePrefix) clearGovUkDate(datePrefix);
      if (textInput) textInput.value = '';
      if (pickerHost) clearUserPicker(pickerHost);
    });

    tbody.addEventListener('click', function (e) {
      if (e.target.classList.contains('nm-remove-row')) {
        e.preventDefault();
        var row = e.target.closest('tr');
        if (row) {
          row.remove();
          toggleEmptyHint(opts.tbodyId, opts.emptyHintId);
        }
      }
    });

    return {
      reindex: function () {
        reindexRows(tbody, opts.prefix, opts.indexFields);
      }
    };
  }

  function init() {
    var root = document.getElementById('near-miss-form-root');
    var form = document.getElementById('nm-form');
    if (!root || !form) return;

    initOwners(root);

    var actions = initDatedCollection({
      tbodyId: 'nm-actions-tbody',
      addBtnId: 'nm-action-add-btn',
      dateFieldPrefix: 'nm-action-add',
      textInputId: 'nm-action-add-text',
      pickerHostId: 'nm-action-add-picker',
      dataAttr: 'data-actions',
      prefix: 'Actions',
      dayField: 'ActionDay',
      monthField: 'ActionMonth',
      yearField: 'ActionYear',
      textField: 'ActionText',
      recordedByField: 'RecordedByUserId',
      textLabel: 'description',
      emptyHintId: 'nm-actions-empty',
      indexFields: ['ActionDay', 'ActionMonth', 'ActionYear', 'ActionText', 'RecordedByUserId']
    });

    var mitigations = initDatedCollection({
      tbodyId: 'nm-mitigations-tbody',
      addBtnId: 'nm-mitigation-add-btn',
      dateFieldPrefix: 'nm-mitigation-add',
      textInputId: 'nm-mitigation-add-text',
      pickerHostId: 'nm-mitigation-add-picker',
      dataAttr: 'data-mitigations',
      prefix: 'Mitigations',
      dayField: 'MitigationDay',
      monthField: 'MitigationMonth',
      yearField: 'MitigationYear',
      textField: 'AssuranceTakenPlace',
      recordedByField: 'RecordedByUserId',
      textLabel: 'assurance taken place',
      emptyHintId: 'nm-mitigations-empty',
      indexFields: ['MitigationDay', 'MitigationMonth', 'MitigationYear', 'AssuranceTakenPlace', 'RecordedByUserId']
    });

    form.addEventListener('submit', function () {
      if (actions) actions.reindex();
      if (mitigations) mitigations.reindex();
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', init);
  } else {
    init();
  }
})();
