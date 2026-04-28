/**
 * DfE Sortable Table
 *
 * Initialise any element with data-module="dfe-sortable-table" that is either:
 *  - a <table> (recommended — matches compass2-app.css sortable header styles), or
 *  - a wrapper containing a single <table> (legacy pattern).
 *
 * Accessibility features:
 *  - Each sortable column header becomes a <button> inside the <th>
 *  - aria-sort="none|ascending|descending" is set on the active <th>
 *  - A visually hidden live region announces the sort state to screen readers
 *  - Sort direction indicator is purely visual (CSS) and hidden from AT
 *  - Keyboard: Tab to header button, Enter/Space to sort
 *  - Works without JavaScript (table renders as a normal static table)
 *
 * Markdown usage:
 *   {sortable}
 *   | Name | Role | Phase |
 *   |------|------|-------|
 *   | Jane | Lead | Beta  |
 */
(function () {
  'use strict';

  // ── Helpers ──────────────────────────────────────────────────────────────────

  /** Return the plain text content of a cell, trimmed. */
  function cellText(cell) {
    return (cell.textContent || cell.innerText || '').trim();
  }

  /**
   * Determine whether a column is numeric by checking whether the majority of
   * its body cells parse as finite numbers (ignoring empty cells).
   */
  function isNumericColumn(tbody, colIndex) {
    var cells = tbody.querySelectorAll('tr');
    var numeric = 0, total = 0;
    cells.forEach(function (row) {
      var cell = row.cells[colIndex];
      if (!cell) return;
      var text = cellText(cell);
      if (text === '') return;
      total++;
      if (!isNaN(parseFloat(text)) && isFinite(text)) numeric++;
    });
    return total > 0 && numeric / total >= 0.5;
  }

  /**
   * Compare two cell values for sorting.
   * Numeric columns compare as numbers; all others compare as locale strings.
   */
  function compareValues(a, b, numeric) {
    if (numeric) {
      return parseFloat(a) - parseFloat(b);
    }
    return a.localeCompare(b, undefined, { sensitivity: 'base' });
  }

  // ── Core sort function ────────────────────────────────────────────────────────

  function sortTable(table, colIndex, ascending) {
    var tbody = table.querySelector('tbody');
    if (!tbody) return;

    var numeric = isNumericColumn(tbody, colIndex);
    var rows = Array.prototype.slice.call(tbody.querySelectorAll('tr'));

    rows.sort(function (rowA, rowB) {
      var a = cellText(rowA.cells[colIndex] || {});
      var b = cellText(rowB.cells[colIndex] || {});
      var result = compareValues(a, b, numeric);
      return ascending ? result : -result;
    });

    // Re-append rows in sorted order
    rows.forEach(function (row) { tbody.appendChild(row); });
  }

  // ── Initialise a single table ─────────────────────────────────────────────────

  function initTable(wrapper) {
    var table =
      wrapper.tagName === 'TABLE'
        ? wrapper
        : wrapper.querySelector('table');
    if (!table || table.dataset.dfeSortableInit) return;
    table.dataset.dfeSortableInit = 'true';

    var thead = table.querySelector('thead');
    if (!thead) return;

    var headerRow = thead.querySelector('tr');
    if (!headerRow) return;

    var headers = headerRow.querySelectorAll('th');
    if (!headers.length) return;

    // ── Live region for screen reader announcements ───────────────────────────
    var liveRegion = document.createElement('p');
    liveRegion.setAttribute('aria-live', 'polite');
    liveRegion.setAttribute('aria-atomic', 'true');
    liveRegion.className = 'govuk-visually-hidden';
    liveRegion.id = 'dfe-sort-announce-' + Math.random().toString(36).slice(2);
    if (table.parentNode) {
      table.parentNode.insertBefore(liveRegion, table);
    }

    // Track current sort state
    var currentCol = -1;
    var currentAsc = true;

    headers.forEach(function (th, colIndex) {
      if (th.getAttribute('data-dfe-sortable') === 'false') {
        return;
      }

      // Mark all headers as sortable
      th.setAttribute('aria-sort', 'none');
      th.classList.add('dfe-sortable-table__header');

      // Wrap the header text in a button
      var label = cellText(th);
      th.innerHTML = '';

      var btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'dfe-sortable-table__sort-btn';
      btn.setAttribute('aria-label', label + ', activate to sort');

      var labelSpan = document.createElement('span');
      labelSpan.textContent = label;

      // Visual sort indicator (hidden from AT)
      var indicator = document.createElement('span');
      indicator.className = 'dfe-sortable-table__indicator';
      indicator.setAttribute('aria-hidden', 'true');

      btn.appendChild(labelSpan);
      btn.appendChild(indicator);
      th.appendChild(btn);

      btn.addEventListener('click', function () {
        var ascending;
        if (currentCol === colIndex) {
          // Toggle direction
          ascending = !currentAsc;
        } else {
          // New column — default ascending
          ascending = true;
        }

        // Reset all sortable headers
        headers.forEach(function (h) {
          if (h.getAttribute('data-dfe-sortable') === 'false') return;
          h.setAttribute('aria-sort', 'none');
          h.classList.remove('dfe-sortable-table__header--asc', 'dfe-sortable-table__header--desc');
        });

        // Set active header
        th.setAttribute('aria-sort', ascending ? 'ascending' : 'descending');
        th.classList.add(ascending ? 'dfe-sortable-table__header--asc' : 'dfe-sortable-table__header--desc');

        sortTable(table, colIndex, ascending);

        currentCol = colIndex;
        currentAsc = ascending;

        // Announce to screen readers
        liveRegion.textContent = 'Table sorted by ' + label + ', ' + (ascending ? 'ascending' : 'descending');
        // Clear after a moment so the same message can be re-announced
        setTimeout(function () { liveRegion.textContent = ''; }, 3000);
      });
    });
  }

  // ── Initialise all sortable tables on the page ────────────────────────────────

  function initAll() {
    var wrappers = document.querySelectorAll('[data-module="dfe-sortable-table"]');
    wrappers.forEach(initTable);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initAll);
  } else {
    initAll();
  }

  // Expose for dynamic content
  window.dfeSortableTableInit = initAll;
}());
