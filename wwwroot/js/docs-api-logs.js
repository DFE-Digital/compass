(function () {
    'use strict';

    var root = document.querySelector('[data-api-logs-split]');
    if (!root) return;

    var logs = [];
    var node = document.getElementById('api-logs-data');
    if (node) {
        try { logs = JSON.parse(node.textContent || '[]'); }
        catch (_) { logs = []; }
    }

    var listEl = root.querySelector('[data-api-logs-entries]');
    var detailEl = root.querySelector('[data-api-logs-detail]');
    var selectedId = root.getAttribute('data-selected-log-id');
    var initialId = selectedId ? parseInt(selectedId, 10) : (logs[0] ? logs[0].id : null);

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
        });
    }

    function findLog(id) {
        for (var i = 0; i < logs.length; i++) {
            if (logs[i].id === id) return logs[i];
        }
        return null;
    }

    function setActive(id) {
        var buttons = root.querySelectorAll('[data-log-id]');
        buttons.forEach(function (btn) {
            var active = parseInt(btn.getAttribute('data-log-id'), 10) === id;
            btn.setAttribute('aria-current', active ? 'true' : 'false');
        });
        if (window.history && window.history.replaceState) {
            var url = new URL(window.location.href);
            if (id) url.searchParams.set('logId', String(id));
            else url.searchParams.delete('logId');
            window.history.replaceState(null, '', url);
        }
    }

    function renderDetail(log) {
        if (!detailEl) return;
        if (!log) {
            detailEl.innerHTML = '<div class="api-logs-split__detail-empty"><p class="govuk-body">Select a log entry to view the request and response.</p></div>';
            return;
        }

        var errorBlock = log.error
            ? '<div class="govuk-error-summary govuk-!-margin-bottom-3" role="alert"><h2 class="govuk-error-summary__title">Error</h2><div class="govuk-error-summary__body">' + escapeHtml(log.error) + '</div></div>'
            : '';

        var reqBody = log.requestBody
            ? '<pre class="api-logs-split__payload">' + escapeHtml(log.requestBody) + '</pre>'
            : '<p class="api-logs-split__muted">No request body recorded.</p>';

        var resBody = log.responseBody
            ? '<pre class="api-logs-split__payload">' + escapeHtml(log.responseBody) + '</pre>'
            : '<p class="api-logs-split__muted">No response body recorded.</p>';

        detailEl.innerHTML =
            '<div class="api-logs-split__detail-header">' +
            '<h2 class="govuk-heading-m govuk-!-margin-bottom-1">' + escapeHtml(log.method) + ' <code class="govuk-body">' + escapeHtml(log.path) + '</code></h2>' +
            '<p class="govuk-body-s govuk-!-margin-bottom-0">' + escapeHtml(log.at) + ' UTC · ' + escapeHtml(log.token || '—') + ' · ' +
            log.status + ' · ' + log.ms + ' ms' + (log.ip ? ' · ' + escapeHtml(log.ip) : '') + '</p>' +
            errorBlock +
            '</div>' +
            '<div class="api-logs-split__detail-panels">' +
            '<section class="api-logs-split__panel" aria-label="Request">' +
            '<h3 class="api-logs-split__panel-title">Request</h3>' +
            '<div class="api-logs-split__panel-body">' + reqBody + '</div>' +
            '</section>' +
            '<section class="api-logs-split__panel" aria-label="Response">' +
            '<h3 class="api-logs-split__panel-title">Response</h3>' +
            '<div class="api-logs-split__panel-body">' + resBody + '</div>' +
            '</section>' +
            '</div>';
    }

    function selectLog(id) {
        setActive(id);
        renderDetail(findLog(id));
    }

    if (listEl) {
        listEl.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-log-id]');
            if (!btn) return;
            selectLog(parseInt(btn.getAttribute('data-log-id'), 10));
        });
    }

    if (initialId && findLog(initialId)) {
        selectLog(initialId);
    } else if (logs.length > 0) {
        selectLog(logs[0].id);
    } else {
        renderDetail(null);
    }
})();
