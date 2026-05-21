/**
 * Six-month RAG trend: drill-down work item table directly under the business area summary table.
 */
(function (global) {
  'use strict';

  var ragBadge = {
    Red: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-red',
    'Amber-Red': 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-amber-red',
    'Amber-Green': 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-amber-green',
    Green: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-green',
    'Not Set': 'dfe-f-badge dfe-f-badge--small dfe-f-badge--grey'
  };

  var trendBadge = {
    Stable: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--blue',
    Improving: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-green',
    Worsening: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-red',
    Stale: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--grey'
  };

  /** Display order for drill-down rows (worsening first). */
  var trendSortOrder = ['Worsening', 'Stale', 'Improving', 'Stable'];
  var trendSortRank = {};
  trendSortOrder.forEach(function (cat, idx) {
    trendSortRank[cat] = idx;
  });

  var ragAbbr = {
    Red: 'R',
    'Amber-Red': 'A-R',
    'Amber-Green': 'A-G',
    Green: 'G',
    'Not Set': '—'
  };

  function escHtml(s) {
    if (s == null) return '';
    return String(s)
      .replace(/&/g, '&amp;')
      .replace(/</g, '&lt;')
      .replace(/>/g, '&gt;')
      .replace(/"/g, '&quot;');
  }

  function escAttr(s) {
    return escHtml(s).replace(/'/g, '&#39;');
  }

  function readConfig() {
    var el = document.getElementById('mr-rag-six-month-config');
    if (!el || !el.textContent) return null;
    try {
      return JSON.parse(el.textContent);
    } catch (e) {
      return null;
    }
  }

  function readDetailPrefix() {
    var el = document.getElementById('mr-rag-six-month-detail-prefix');
    if (!el || !el.textContent) return '/modern/work/detail/';
    try {
      return JSON.parse(el.textContent);
    } catch (e) {
      return '/modern/work/detail/';
    }
  }

  function scopeMap(config) {
    var map = {};
    (config.scope || []).forEach(function (s) {
      map[s.id] = s;
      map[String(s.id)] = s;
    });
    return map;
  }

  function filterRows(rows, businessArea, trend) {
    return rows.filter(function (r) {
      if (businessArea !== '__total__') {
        var ba = r.businessArea || 'Not set';
        if (ba !== businessArea) return false;
      }
      if (trend && r.trendCategory !== trend) return false;
      return true;
    });
  }

  function sortRowsByTrend(rows) {
    return rows.slice().sort(function (a, b) {
      var ra = Object.prototype.hasOwnProperty.call(trendSortRank, a.trendCategory)
        ? trendSortRank[a.trendCategory]
        : trendSortOrder.length;
      var rb = Object.prototype.hasOwnProperty.call(trendSortRank, b.trendCategory)
        ? trendSortRank[b.trendCategory]
        : trendSortOrder.length;
      if (ra !== rb) return ra - rb;
      return String(a.title || '').localeCompare(String(b.title || ''), undefined, { sensitivity: 'base' });
    });
  }

  function buildTitle(businessArea, trend, count) {
    var baLabel = businessArea === '__total__' ? 'All business areas' : businessArea;
    var countLabel = count + ' work item' + (count === 1 ? '' : 's');
    if (trend) {
      return baLabel + ' — ' + trend + ' (' + countLabel + ')';
    }
    return baLabel + ' — all work items (' + countLabel + ')';
  }

  function renderTable(rows, scopeById) {
    if (rows.length === 0) {
      return '<p class="govuk-body-s dfe-c-text-muted govuk-!-margin-bottom-0">No work items match this selection.</p>';
    }
    var monthHeaders = rows[0].months || [];
    var html = '<div class="dfe-f-table-wrapper" data-module="dfe-f-table">';
    html += '<div class="govuk-visually-hidden" aria-live="polite" data-dfe-table-sort-live></div>';
    html += '<table class="govuk-table govuk-table--small-text-until-tablet dfe-f-table--condensed mr-rag-trend-table">';
    html += '<caption class="govuk-visually-hidden">Work items for selected trend</caption>';
    html += '<thead class="govuk-table__head"><tr class="govuk-table__row">';
    html += '<th scope="col" class="govuk-table__header mr-col-work-item" aria-sort="none"><button type="button" class="dfe-f-table-sort-button">Work item</button></th>';
    monthHeaders.forEach(function (mh) {
      html += '<th scope="col" class="govuk-table__header govuk-table__header--numeric mr-rag-trend-table__month" aria-sort="none"><button type="button" class="dfe-f-table-sort-button">' + escHtml(mh.label) + '</button></th>';
    });
    html += '<th scope="col" class="govuk-table__header" aria-sort="none"><button type="button" class="dfe-f-table-sort-button">Trend</button></th></tr></thead><tbody class="govuk-table__body">';

    rows.forEach(function (row) {
      var scope = scopeById[row.projectId] || scopeById[String(row.projectId)] || {};
      var ba = row.businessArea || 'Not set';
      html += '<tr class="govuk-table__row">';
      html += '<td class="govuk-table__cell mr-col-work-item">';
      html += '<button type="button" class="govuk-link govuk-!-font-weight-bold mr-info-btn mr-ba-drill-title-btn"';
      html += ' data-mr-project-id="' + escAttr(row.projectId) + '"';
      html += ' data-mr-title="' + escAttr(row.title) + '"';
      html += ' data-mr-summary="' + escAttr(scope.summary || '') + '"';
      html += ' data-mr-rag-just="' + escAttr(scope.ragJustification || '') + '"';
      html += ' data-mr-narrative="' + escAttr(scope.narrative || '') + '"';
      html += ' data-mr-ptg="' + escAttr(scope.pathToGreen || '') + '"';
      html += ' data-mr-milestones="' + escAttr(scope.milestones || '') + '"';
      html += ' data-mr-ba-label="' + escAttr(ba) + '"';
      html += ' data-mr-rag="' + escAttr(scope.rag || '') + '"';
      html += ' data-mr-priority="' + escAttr(scope.priority || '') + '"';
      html += ' aria-label="Open summary for ' + escAttr(row.title) + '">' + escHtml(row.title) + '</button>';
      html += '<div class="mr-work-item-meta dfe-c-body-s dfe-c-text-muted">' + escHtml(ba) + '</div></td>';

      (row.months || []).forEach(function (snap) {
        var rag = snap.rag || 'Not Set';
        var cls = ragBadge[rag] || ragBadge['Not Set'];
        var abbr = ragAbbr[rag] || '—';
        html += '<td class="govuk-table__cell govuk-table__cell--numeric mr-rag-trend-table__month">';
        html += '<span class="' + cls + '" title="' + escAttr(rag) + '">' + escHtml(abbr) + '</span></td>';
      });

      var tCls = trendBadge[row.trendCategory] || trendBadge.Stale;
      html += '<td class="govuk-table__cell"><span class="' + tCls + '">' + escHtml(row.trendCategory) + '</span></td>';
      html += '</tr>';
    });

    html += '</tbody></table></div></div>';
    return html;
  }

  function init() {
    var config = readConfig();
    var host = document.getElementById('mr-rag-six-month-drilldown-host');
    if (!config || !host || !config.rows) return;

    var scopeById = scopeMap(config);
    var panel = null;
    var activeBtn = null;

    function clearActiveControl() {
      if (!activeBtn) return;
      activeBtn.classList.remove('mr-rag-ba-drill--active');
      activeBtn.classList.remove('mr-rag-ba-link--active');
      activeBtn = null;
    }

    function hideDrill() {
      if (panel) {
        panel.hidden = true;
        panel = null;
      }
      clearActiveControl();
    }

    function showDrill(businessArea, trend, btn) {
      var items = sortRowsByTrend(filterRows(config.rows, businessArea, trend || null));
      if (activeBtn && activeBtn !== btn) clearActiveControl();
      activeBtn = btn;
      if (btn) {
        if (btn.classList.contains('mr-rag-ba-link')) {
          btn.classList.add('mr-rag-ba-link--active');
        } else {
          btn.classList.add('mr-rag-ba-drill--active');
        }
      }

      if (!panel) {
        panel = document.createElement('div');
        panel.id = 'mr-rag-six-month-drilldown';
        panel.className = 'mr-ba-drilldown govuk-!-margin-top-3';
        panel.innerHTML =
          '<div class="mr-ba-drilldown__header">' +
          '<h3 class="mr-ba-drilldown__title" id="mr-rag-six-month-drilldown-title"></h3>' +
          '<div class="mr-ba-drilldown__actions">' +
          '<button type="button" class="mr-ba-drilldown__close" aria-label="Close drill-down">&times;</button>' +
          '</div></div>' +
          '<div id="mr-rag-six-month-drilldown-body"></div>';
        host.appendChild(panel);
        panel.querySelector('.mr-ba-drilldown__close').addEventListener('click', hideDrill);
      }

      var titleEl = panel.querySelector('#mr-rag-six-month-drilldown-title');
      var bodyEl = panel.querySelector('#mr-rag-six-month-drilldown-body');
      if (titleEl) titleEl.textContent = buildTitle(businessArea, trend, items.length);
      if (bodyEl) bodyEl.innerHTML = renderTable(items, scopeById);
      panel.hidden = false;
      panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function onDrillControlClick(btn) {
      var ba = btn.getAttribute('data-mr-ba') || '';
      var trend = btn.getAttribute('data-mr-trend') || '';
      if (!ba) return;
      if (activeBtn === btn && panel && !panel.hidden) {
        hideDrill();
        return;
      }
      showDrill(ba, trend || null, btn);
    }

    var summaryTable = document.getElementById('mr-rag-ba-summary-table');
    if (summaryTable) {
      summaryTable.querySelectorAll('.mr-rag-ba-drill, .mr-rag-ba-link').forEach(function (btn) {
        btn.addEventListener('click', function () {
          onDrillControlClick(btn);
        });
      });
    }
  }

  global.MonthlyReportRagTrends = { init: init };
})(typeof window !== 'undefined' ? window : this);
