/* global window, document */
(function () {
  'use strict';

  function run() {
    var configEl = document.getElementById('mr-reporting-drilldown-config');
    if (!configEl || !configEl.textContent) return;

    var config;
    try {
      config = JSON.parse(configEl.textContent);
    } catch (e) {
      return;
    }

    var projectsByKey = config.projectsByKey || {};
    var scopeProjects = config.scopeProjects || [];
    var detailsById = config.detailsById || {};
    var workDetailPrefix = config.workDetailPrefix || '/modern/work/detail/';
    var exportBaseUrl = config.exportBaseUrl || '';
    var exportParams = config.exportParams || {};
    var dimensionLabels = config.dimensionLabels || {};
    var drillRootSelector = config.drillRootSelector || '.mr-dash';

    var filterLabels = {
      total: 'All work items',
      submitted: 'Submitted this period',
      new: 'New this month',
      'ms-done': 'Milestones completed',
      'ms-soon': 'Milestones due soon',
      'ms-late': 'Late milestones',
      'rag-Red': 'RAG: Red',
      'rag-Amber-Red': 'RAG: Amber–Red',
      'rag-Amber-Green': 'RAG: Amber–Green',
      'rag-Green': 'RAG: Green',
      'rag-Not Set': 'RAG: Not set',
      'pri-Critical': 'Priority: Critical',
      'pri-High': 'Priority: High',
      'pri-Medium': 'Priority: Medium',
      'pri-Low': 'Priority: Low',
      'pri-Not Set': 'Priority: Not set',
      'tab-active': 'Active and paused',
      'tab-completed': 'Completed'
    };

    var ragBadgeClass = {
      Red: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-red',
      'Amber-Red': 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-amber-red',
      'Amber-Green': 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-amber-green',
      Green: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-green',
      'Not Set': 'dfe-f-badge dfe-f-badge--small dfe-f-badge--rag-none'
    };
    var priDfeBadge = {
      Critical: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--red',
      High: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--red',
      Medium: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--orange',
      Low: 'dfe-f-badge dfe-f-badge--small dfe-f-badge--green',
      'Not Set': 'dfe-f-badge dfe-f-badge--small dfe-f-badge--grey'
    };

    var priorityOrder = { Critical: 0, High: 1, Medium: 2, Low: 3, 'Not Set': 4 };
    var ragOrder = { Red: 0, 'Amber-Red': 1, 'Amber-Green': 2, Green: 3, 'Not Set': 4 };

    var panel = document.getElementById('mr-ba-drilldown');
    var titleEl = document.getElementById('mr-ba-drilldown-title');
    var bodyEl = document.getElementById('mr-ba-drilldown-body');
    var extraHdr = document.getElementById('mr-ba-drilldown-extra-hdr');
    var exportLink = document.getElementById('mr-ba-drilldown-export');
    var activeCell = null;
    var activeDrill = null;
    var activeHostId = null;

    var modal = document.getElementById('mr-reporting-modal');
    var modalTitle = document.getElementById('mr-reporting-modal-title');
    var modalViewLink = document.getElementById('mr-reporting-modal-view-link');

    function escHtml(s) {
      var d = document.createElement('div');
      d.appendChild(document.createTextNode(s == null ? '' : String(s)));
      return d.innerHTML;
    }

    function escAttr(s) {
      return String(s == null ? '' : s)
        .replace(/&/g, '&amp;')
        .replace(/"/g, '&quot;')
        .replace(/</g, '&lt;');
    }

    function formatNum(n) {
      if (n == null || n === '') return null;
      var x = Number(n);
      if (Number.isNaN(x)) return null;
      return x % 1 === 0 ? String(x) : x.toFixed(2);
    }

    function formatFteMsc(p) {
      var parts = [];
      var fte = formatNum(p.permFte);
      var msc = formatNum(p.mspFte);
      if (fte != null) parts.push('FTE ' + fte);
      if (msc != null) parts.push('MSC ' + msc);
      return parts.length ? parts.join(' · ') : '—';
    }

    function drillContext(filter) {
      if (filter.indexOf('rag-') === 0 || filter.indexOf('pri-') === 0 || filter.indexOf('mx-') === 0) {
        return 'rag-justification';
      }
      return 'default';
    }

    function resolveDrillHost(cell) {
      var hostEl = cell.closest('[data-mr-drill-host]');
      if (hostEl && hostEl.getAttribute('data-mr-drill-host')) {
        return hostEl.getAttribute('data-mr-drill-host');
      }
      return 'mr-ba-drilldown-host';
    }

    function movePanelToHost(hostId) {
      if (!panel || !hostId) return;
      var host = document.getElementById(hostId);
      if (!host || host === panel.parentElement) {
        activeHostId = hostId;
        return;
      }
      host.appendChild(panel);
      activeHostId = hostId;
    }

    function filterItems(items, filter) {
      if (!items || !items.length) return [];
      if (filter.indexOf('mx-') === 0) {
        var parts = filter.substring(3).split('|');
        if (parts.length === 2) {
          return items.filter(function (p) {
            return p.rag === parts[0] && p.priority === parts[1];
          });
        }
      }
      if (filter === 'total') return items.slice();
      if (filter === 'submitted') return items.filter(function (p) { return p.submittedUpdate; });
      if (filter === 'new') return items.filter(function (p) { return p.isNew; });
      if (filter === 'ms-done') return items.filter(function (p) { return p.hasMilestoneCompletedInPeriod; });
      if (filter === 'ms-soon') return items.filter(function (p) { return p.hasMilestoneUpcomingInWindow; });
      if (filter === 'ms-late') return items.filter(function (p) { return p.hasLateMilestone; });
      if (filter.indexOf('rag-') === 0) {
        var rv = filter.substring(4);
        return items.filter(function (p) { return p.rag === rv; });
      }
      if (filter.indexOf('pri-') === 0) {
        var pv = filter.substring(4);
        return items.filter(function (p) { return p.priority === pv; });
      }
      if (filter === 'tab-active') {
        return items.filter(function (p) { return p.status === 'Active' || p.status === 'Paused'; });
      }
      if (filter === 'tab-completed') {
        return items.filter(function (p) { return p.status === 'Completed'; });
      }
      return items.slice();
    }

    function sortItems(items) {
      return items.slice().sort(function (a, b) {
        var pa = priorityOrder[a.priority] != null ? priorityOrder[a.priority] : 99;
        var pb = priorityOrder[b.priority] != null ? priorityOrder[b.priority] : 99;
        if (pa !== pb) return pa - pb;
        var ra = ragOrder[a.rag] != null ? ragOrder[a.rag] : 99;
        var rb = ragOrder[b.rag] != null ? ragOrder[b.rag] : 99;
        if (ra !== rb) return ra - rb;
        return String(a.title || '').localeCompare(String(b.title || ''), undefined, { sensitivity: 'base' });
      });
    }

    function resolveSource(dimension, groupKey) {
      if (groupKey === '__scope__' || groupKey === 'Total') return scopeProjects;
      var key = dimension ? dimension + '|' + groupKey : groupKey;
      return projectsByKey[key] || projectsByKey[groupKey] || [];
    }

    function buildTitle(dimension, groupKey, filter, count) {
      var dimPrefix = dimension && dimensionLabels[dimension] ? dimensionLabels[dimension] + ': ' : '';
      var label = groupKey === '__scope__' ? 'Current scope' : groupKey === 'Total' ? 'Total' : dimPrefix + groupKey;
      if (filter.indexOf('mx-') === 0) {
        var parts = filter.substring(3).split('|');
        return label + ' — RAG ' + parts[0] + ', Priority ' + parts[1] + ' (' + count + ')';
      }
      return label + ' — ' + (filterLabels[filter] || filter) + ' (' + count + ')';
    }

    function buildExportUrl(dimension, groupKey, filter) {
      if (!exportBaseUrl) return '';
      var params = new URLSearchParams();
      Object.keys(exportParams).forEach(function (k) {
        if (exportParams[k] != null && exportParams[k] !== '') params.set(k, String(exportParams[k]));
      });
      if (dimension) params.set('dimension', dimension);
      if (groupKey) params.set('ba', groupKey);
      if (filter) params.set('filter', filter);
      return exportBaseUrl + (exportBaseUrl.indexOf('?') >= 0 ? '&' : '?') + params.toString();
    }

    function workItemCellHtml(p) {
      var pid = String(p.id);
      var ba = p.businessArea && String(p.businessArea).trim() ? String(p.businessArea).trim() : '';
      var meta = ba
        ? '<div class="mr-work-item-meta dfe-c-body-s dfe-c-text-muted">' + escHtml(ba) + '</div>'
        : '';
      return (
        '<button type="button" class="mr-ba-drill-title-btn" data-mr-project-id="' + escAttr(pid) + '">' +
        escHtml(p.title) +
        '</button>' + meta
      );
    }

    function ragJustificationForItem(p) {
      var stored = detailsById[p.id] || detailsById[String(p.id)];
      var text = (stored && stored.ragJustification) || p.ragJustification || '';
      text = text && String(text).trim() ? String(text).trim() : '';
      return text || '—';
    }

    function renderRows(items, context) {
      if (!bodyEl) return;
      var showRagJust = context === 'rag-justification';
      if (extraHdr) extraHdr.hidden = !showRagJust;

      bodyEl.innerHTML = '';
      items.forEach(function (p) {
        var tr = document.createElement('tr');
        tr.className = 'govuk-table__row';
        var extraCell = showRagJust
          ? '<td class="govuk-table__cell mr-ba-drilldown-table__extra mr-change-justification">' +
            escHtml(ragJustificationForItem(p)) +
            '</td>'
          : '';
        tr.innerHTML =
          '<td class="govuk-table__cell mr-col-work-item">' + workItemCellHtml(p) + '</td>' +
          '<td class="govuk-table__cell"><span class="' + (ragBadgeClass[p.rag] || ragBadgeClass['Not Set']) + '">' + escHtml(p.rag) + '</span></td>' +
          '<td class="govuk-table__cell"><span class="' + (priDfeBadge[p.priority] || priDfeBadge['Not Set']) + '">' + escHtml(p.priority) + '</span></td>' +
          '<td class="govuk-table__cell govuk-table__cell--numeric mr-ba-drilldown-table__cell--num">' + escHtml(formatFteMsc(p)) + '</td>' +
          extraCell;
        bodyEl.appendChild(tr);
      });
    }

    function showDrill(dimension, groupKey, filter, hostId) {
      if (!panel || !titleEl) return;
      movePanelToHost(hostId || 'mr-ba-drilldown-host');
      var source = resolveSource(dimension, groupKey);
      var items = sortItems(filterItems(source, filter));
      var context = drillContext(filter);
      titleEl.textContent = buildTitle(dimension, groupKey, filter, items.length);
      renderRows(items, context);
      activeDrill = { dimension: dimension || '', groupKey: groupKey, filter: filter, hostId: hostId };
      if (exportLink && exportBaseUrl) {
        exportLink.href = buildExportUrl(dimension, groupKey, filter);
        exportLink.hidden = items.length === 0;
      }
      panel.hidden = false;
      panel.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
    }

    function hideDrill() {
      if (!panel) return;
      panel.hidden = true;
      if (activeCell) {
        activeCell.classList.remove('mr-ba-drill--active');
        activeCell = null;
      }
      activeDrill = null;
    }

    function overviewText(detail) {
      var parts = [];
      if (detail.businessArea) parts.push('Business area: ' + detail.businessArea);
      if (detail.rag) parts.push('RAG: ' + detail.rag);
      if (detail.priority) parts.push('Priority: ' + detail.priority);
      if (detail.ragJustification) parts.push('\n\nRAG justification:\n' + detail.ragJustification);
      return parts.join('\n') || '—';
    }

    function setPaneText(id, text) {
      var el = document.getElementById(id);
      if (!el) return;
      var t = text && String(text).trim() ? String(text).trim() : '—';
      el.textContent = t;
    }

    function switchTab(tabKey) {
      if (!modal) return;
      var tabs = modal.querySelectorAll('.mr-reporting-modal__tab');
      var panes = modal.querySelectorAll('.mr-reporting-modal__pane');
      tabs.forEach(function (tab) {
        var on = tab.getAttribute('data-mr-tab') === tabKey;
        tab.setAttribute('aria-selected', on ? 'true' : 'false');
      });
      panes.forEach(function (pane) {
        var on = pane.id === 'mr-reporting-pane-' + tabKey;
        pane.classList.toggle('is-active', on);
        pane.hidden = !on;
      });
    }

    function detailFromButton(btn) {
      var id = btn.getAttribute('data-mr-project-id');
      if (!id) return null;
      var stored = detailsById[id] || detailsById[String(id)];
      if (stored) return stored;
      return {
        id: id,
        title: btn.getAttribute('data-mr-title') || 'Work item',
        summary: btn.getAttribute('data-mr-summary') || null,
        latestMonthlyUpdateNarrative: btn.getAttribute('data-mr-narrative') || null,
        pathToGreen: btn.getAttribute('data-mr-ptg') || null,
        milestonesSummary: btn.getAttribute('data-mr-milestones') || null,
        ragJustification: btn.getAttribute('data-mr-rag-just') || null,
        businessArea: btn.getAttribute('data-mr-ba-label') || null,
        rag: btn.getAttribute('data-mr-rag') || null,
        priority: btn.getAttribute('data-mr-priority') || null
      };
    }

    function openModalFromButton(btn) {
      if (!modal || !modalTitle || !modalViewLink) return;
      var detail = detailFromButton(btn);
      if (!detail) return;
      modalTitle.textContent = detail.title || 'Work item';
      setPaneText('mr-reporting-overview-text', overviewText(detail));
      setPaneText('mr-reporting-aim-text', detail.summary);
      setPaneText('mr-reporting-update-text', detail.latestMonthlyUpdateNarrative);
      setPaneText('mr-reporting-ptg-text', detail.pathToGreen);
      setPaneText('mr-reporting-milestones-text', detail.milestonesSummary);
      modalViewLink.href = workDetailPrefix + detail.id;
      switchTab('overview');
      modal.hidden = false;
      modal.classList.add('is-open');
      document.body.style.overflow = 'hidden';
    }

    function closeModal() {
      if (!modal) return;
      modal.hidden = true;
      modal.classList.remove('is-open');
      document.body.style.overflow = '';
    }

    function onDrillClick(e) {
      var cell = e.target.closest('.mr-ba-drill');
      if (!cell) return;
      var groupKey = cell.getAttribute('data-ba');
      var filter = cell.getAttribute('data-filter');
      if (!groupKey || !filter) return;
      if (activeCell === cell) {
        hideDrill();
        return;
      }
      if (activeCell) activeCell.classList.remove('mr-ba-drill--active');
      activeCell = cell;
      cell.classList.add('mr-ba-drill--active');
      showDrill(
        cell.getAttribute('data-dimension') || '',
        groupKey,
        filter,
        resolveDrillHost(cell)
      );
    }

    function onDrillKeydown(e) {
      if (e.key !== 'Enter' && e.key !== ' ') return;
      var cell = e.target.closest('.mr-ba-drill');
      if (cell) {
        e.preventDefault();
        cell.click();
      }
    }

    function onDocumentClick(e) {
      var titleBtn = e.target.closest('.mr-ba-drill-title-btn, .mr-info-btn[data-mr-project-id]');
      if (titleBtn) {
        e.preventDefault();
        e.stopPropagation();
        openModalFromButton(titleBtn);
        return;
      }
      var drillRoot = document.querySelector(drillRootSelector);
      if (drillRoot && drillRoot.contains(e.target)) {
        onDrillClick(e);
      }
    }

    document.addEventListener('click', onDocumentClick);
    document.addEventListener('keydown', onDrillKeydown);

    var closeBtn = panel && panel.querySelector('.mr-ba-drilldown__close');
    if (closeBtn) closeBtn.addEventListener('click', hideDrill);

    if (modal) {
      modal.querySelectorAll('[data-mr-modal-close]').forEach(function (el) {
        el.addEventListener('click', closeModal);
      });
      modal.querySelectorAll('.mr-reporting-modal__tab').forEach(function (tab) {
        tab.addEventListener('click', function () {
          switchTab(tab.getAttribute('data-mr-tab'));
        });
      });
      document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape' && modal.classList.contains('is-open')) closeModal();
      });
    }
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', run);
  } else {
    run();
  }
})();
