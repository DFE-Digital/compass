(function () {
  'use strict';

  function initRaidDeleteSearch() {
    var searchInput = document.getElementById('raid-delete-search');
    var resultsDiv = document.getElementById('raid-delete-results');
    var selectedDiv = document.getElementById('raid-delete-selected');
    var confirmDiv = document.getElementById('raid-delete-confirm');
    var deleteBtn = document.getElementById('raid-del-btn');
    var cancelBtn = document.getElementById('raid-del-cancel');

    if (!searchInput || !resultsDiv || !selectedDiv || !confirmDiv || !deleteBtn || !cancelBtn) {
      return;
    }

    var debounceTimer = null;
    var selectedItem = null;

    function renderResults(items) {
      resultsDiv.innerHTML = '';
      if (!items.length) {
        var empty = document.createElement('div');
        empty.style.padding = '8px 12px';
        empty.className = 'govuk-body-s dfe-c-text-muted';
        empty.textContent = 'No results found';
        resultsDiv.appendChild(empty);
        resultsDiv.style.display = 'block';
        return;
      }

      items.forEach(function (item) {
        var row = document.createElement('div');
        row.className = 'raid-del-result-item';
        row.style.cssText = 'padding:8px 12px;cursor:pointer;border-bottom:1px solid #f3f2f1;';
        row.dataset.id = String(item.id);
        row.dataset.type = item.entityType || '';
        row.dataset.ref = item.reference || '';
        row.dataset.title = item.title || '';
        row.dataset.meta = item.metadata || '';

        var refStrong = document.createElement('strong');
        refStrong.className = 'dfe-c-mono';
        refStrong.textContent = item.reference || '';

        var tag = document.createElement('span');
        tag.className = 'govuk-tag govuk-tag--' + (item.entityType === 'Risk' ? 'red' : 'yellow') + ' govuk-!-margin-left-1';
        tag.style.fontSize = '.75rem';
        tag.textContent = item.entityType || '';

        var titleLine = document.createElement('span');
        titleLine.className = 'govuk-body-s govuk-!-margin-bottom-0';
        titleLine.textContent = item.title || '';

        var metaLine = document.createElement('span');
        metaLine.className = 'govuk-body-s dfe-c-text-muted govuk-!-margin-bottom-0';
        metaLine.textContent = item.metadata || '';

        row.appendChild(refStrong);
        row.appendChild(document.createTextNode(' '));
        row.appendChild(tag);
        row.appendChild(document.createElement('br'));
        row.appendChild(titleLine);
        row.appendChild(document.createElement('br'));
        row.appendChild(metaLine);

        row.addEventListener('mouseenter', function () { row.style.background = '#f3f2f1'; });
        row.addEventListener('mouseleave', function () { row.style.background = '#fff'; });
        row.addEventListener('click', function () {
          selectedItem = {
            id: row.dataset.id,
            type: row.dataset.type,
            ref: row.dataset.ref,
            title: row.dataset.title,
            meta: row.dataset.meta
          };
          document.getElementById('raid-del-ref').textContent = selectedItem.ref;
          document.getElementById('raid-del-title').textContent = selectedItem.title;
          document.getElementById('raid-del-type').textContent = selectedItem.type;
          document.getElementById('raid-del-meta').textContent = selectedItem.meta;
          selectedDiv.style.display = 'block';
          confirmDiv.style.display = 'none';
          resultsDiv.style.display = 'none';
          searchInput.value = '';
        });

        resultsDiv.appendChild(row);
      });

      resultsDiv.style.display = 'block';
    }

    searchInput.addEventListener('input', function () {
      clearTimeout(debounceTimer);
      var q = this.value.trim();
      if (q.length < 2) {
        resultsDiv.style.display = 'none';
        return;
      }
      debounceTimer = setTimeout(function () {
        fetch('/api/entities/search?entityType=RiskOrIssue&q=' + encodeURIComponent(q), {
          credentials: 'same-origin',
          headers: { Accept: 'application/json' }
        })
          .then(function (r) {
            if (!r.ok) {
              return r.json().catch(function () { return {}; }).then(function (d) {
                throw new Error(d.error || 'Search failed');
              });
            }
            return r.json();
          })
          .then(function (data) {
            renderResults(data.results || []);
          })
          .catch(function () {
            renderResults([]);
          });
      }, 300);
    });

    document.addEventListener('click', function (e) {
      if (!resultsDiv.contains(e.target) && e.target !== searchInput) {
        resultsDiv.style.display = 'none';
      }
    });

    deleteBtn.addEventListener('click', function () {
      if (!selectedItem) return;
      document.getElementById('raid-del-confirm-ref').textContent =
        selectedItem.ref + ' "' + selectedItem.title + '"';
      document.getElementById('raid-del-form-type').value = selectedItem.type;
      document.getElementById('raid-del-form-id').value = selectedItem.id;
      confirmDiv.style.display = 'block';
    });

    cancelBtn.addEventListener('click', function () {
      confirmDiv.style.display = 'none';
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initRaidDeleteSearch);
  } else {
    initRaidDeleteSearch();
  }
})();
