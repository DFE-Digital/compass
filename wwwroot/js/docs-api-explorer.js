/* ===========================================================================
 * /docs/api-explorer — interactive Postman/Thunder-Client-style request UI.
 *
 *  - Loads the endpoint catalogue and user tokens from same-origin JSON endpoints (CSP-safe).
 *  - Builds a searchable collections sidebar grouped by section.
 *  - On endpoint click: hydrates the request builder (method pill, URL,
 *    params table, body editor, headers preview).
 *  - On Send: fires fetch(), shows status/time/size, pretty-prints + syntax
 *    highlights the JSON response, exposes Copy / JSON / CSV / Excel exports
 *    and a tabular preview.
 *  - Persists the bearer token in sessionStorage by default, localStorage
 *    when "Remember on this device" is ticked.
 *
 * CSP-safe (no eval, no inline handlers). Vanilla ES2017+.
 * =========================================================================== */
(function () {
    'use strict';

    var root = document.querySelector('[data-api-explorer]');
    if (!root) return;

    /* ----------------------------- helpers ----------------------------- */
    function $(sel, scope) { return (scope || root).querySelector(sel); }
    function $$(sel, scope) { return Array.prototype.slice.call((scope || root).querySelectorAll(sel)); }

    function el(tag, attrs, children) {
        var node = document.createElement(tag);
        if (attrs) Object.keys(attrs).forEach(function (k) {
            if (k === 'class') node.className = attrs[k];
            else if (k === 'text') node.textContent = attrs[k];
            else if (k === 'html') node.innerHTML = attrs[k];
            else if (k.indexOf('data-') === 0) node.setAttribute(k, attrs[k]);
            else if (attrs[k] !== undefined && attrs[k] !== null) node.setAttribute(k, attrs[k]);
        });
        if (children) (Array.isArray(children) ? children : [children]).forEach(function (c) {
            if (c === null || c === undefined) return;
            node.appendChild(typeof c === 'string' ? document.createTextNode(c) : c);
        });
        return node;
    }

    function escapeHtml(s) {
        return String(s).replace(/[&<>"']/g, function (c) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
        });
    }

    /* ----------------------------- catalogue ----------------------------- */
    var catalogueUrl = root.getAttribute('data-catalogue-url') || '/docs/api-explorer/catalogue';
    var userTokensUrl = root.getAttribute('data-user-tokens-url') || '/docs/api-explorer/user-tokens';
    var proxyUrl = root.getAttribute('data-proxy-url') || '/docs/api-explorer/proxy';

    var catalogue = [];
    var endpointsById = {};

    function indexCatalogue() {
        endpointsById = {};
        catalogue.forEach(function (section) {
            section.endpoints.forEach(function (ep) { endpointsById[ep.id] = ep; });
        });
    }

    function normalizeUserTokens(raw) {
        if (!Array.isArray(raw)) return [];
        return raw.map(function (t) {
            return {
                id: t.id != null ? t.id : t.Id,
                name: t.name || t.Name || 'API key',
                accessTier: t.accessTier || t.AccessTier || ''
            };
        }).filter(function (t) { return t.id != null; });
    }

    function loadExplorerData() {
        return Promise.all([
            fetch(catalogueUrl, { credentials: 'same-origin', headers: { Accept: 'application/json' } })
                .then(function (r) { return r.ok ? r.json() : []; })
                .catch(function () { return []; }),
            fetch(userTokensUrl, { credentials: 'same-origin', headers: { Accept: 'application/json' } })
                .then(function (r) { return r.ok ? r.json() : []; })
                .catch(function () { return []; })
        ]).then(function (parts) {
            catalogue = Array.isArray(parts[0]) ? parts[0] : [];
            userTokens = normalizeUserTokens(parts[1]);
            indexCatalogue();
        });
    }

    function isCrossOrigin(base) {
        try {
            return new URL(base).origin !== window.location.origin;
        } catch (_) {
            return true;
        }
    }

    /* ----------------------------- state ----------------------------- */
    var state = {
        currentEndpointId: null,
        bodyText: '',
        paramValues: {},
        lastResponse: null,
        lastResponseText: '',
        lastResponseHeaders: {},
        lastResponseRows: []
    };

    /* ----------------------------- collections tree ----------------------------- */
    function renderTree(filter) {
        var tree = $('[data-explorer-tree]');
        var empty = $('[data-explorer-empty]');
        if (!tree) return;
        tree.innerHTML = '';

        var needle = (filter || '').trim().toLowerCase();
        var anyVisible = false;

        catalogue.forEach(function (section) {
            var matchedEndpoints = section.endpoints.filter(function (ep) {
                if (!needle) return true;
                return (ep.path + ' ' + ep.method + ' ' + ep.description + ' ' + section.title)
                    .toLowerCase().indexOf(needle) !== -1;
            });
            if (matchedEndpoints.length === 0) return;
            anyVisible = true;

            var sectionEl = el('section', {
                'class': 'api-explorer__section',
                'data-section-id': section.id
            });

            var btn = el('button', {
                type: 'button',
                'class': 'api-explorer__section-button',
                'aria-expanded': 'true'
            });
            btn.appendChild(document.createTextNode(section.title));
            btn.appendChild(el('span', {
                'class': 'api-explorer__section-count',
                text: String(matchedEndpoints.length)
            }));
            btn.addEventListener('click', function () {
                sectionEl.classList.toggle('api-explorer__section--collapsed');
                btn.setAttribute('aria-expanded',
                    sectionEl.classList.contains('api-explorer__section--collapsed') ? 'false' : 'true');
            });
            sectionEl.appendChild(btn);

            var ul = el('ul', { 'class': 'api-explorer__section-list' });
            matchedEndpoints.forEach(function (ep) {
                var li = el('li', { 'class': 'api-explorer__endpoint', 'data-endpoint-id': ep.id });
                if (state.currentEndpointId === ep.id) {
                    li.classList.add('api-explorer__endpoint--active');
                }
                var epBtn = el('button', {
                    type: 'button',
                    'class': 'api-explorer__endpoint-button',
                    'data-endpoint-trigger': ep.id
                }, [
                    el('span', {
                        'class': 'api-explorer__method api-explorer__endpoint-method',
                        'data-method': ep.method,
                        text: ep.method
                    }),
                    el('span', { 'class': 'api-explorer__endpoint-path', text: ep.path })
                ]);
                epBtn.title = ep.method + ' ' + ep.path + '\n' + ep.description;
                epBtn.addEventListener('click', function () { selectEndpoint(ep.id); });
                li.appendChild(epBtn);
                ul.appendChild(li);
            });
            sectionEl.appendChild(ul);
            tree.appendChild(sectionEl);
        });

        if (empty) {
            if (anyVisible) empty.setAttribute('hidden', '');
            else empty.removeAttribute('hidden');
        }
    }

    /* ----------------------------- endpoint selection ----------------------------- */
    function selectEndpoint(id) {
        var ep = endpointsById[id];
        if (!ep) return;
        state.currentEndpointId = id;
        state.paramValues = {};
        state.bodyText = ep.bodyExample ? ep.bodyExample.trim() : '';
        state.lastResponse = null;
        state.lastResponseText = '';
        state.lastResponseHeaders = {};
        state.lastResponseRows = [];

        // Highlight in the sidebar
        $$('[data-endpoint-id]').forEach(function (li) {
            li.classList.toggle('api-explorer__endpoint--active', li.getAttribute('data-endpoint-id') === id);
        });

        // Toggle the empty stage / builder
        var empty = $('[data-explorer-empty-stage]');
        var builder = $('[data-explorer-builder]');
        if (empty) empty.setAttribute('hidden', '');
        if (builder) builder.removeAttribute('hidden');

        hydrateBuilder(ep);
        resetResponse();
        try { history.replaceState(null, '', '#' + id); } catch (_) { /* ignore */ }
    }

    /* ----------------------------- request builder hydration ----------------------------- */
    function hydrateBuilder(ep) {
        // Method pill
        var methodEl = $('[data-explorer-method]');
        if (methodEl) {
            methodEl.textContent = ep.method;
            methodEl.setAttribute('data-method', ep.method);
        }

        // Meta
        var scopeEl = $('[data-explorer-scope]');
        if (scopeEl) scopeEl.textContent = ep.scope || '—';
        var descEl = $('[data-explorer-desc]');
        if (descEl) descEl.textContent = ep.description || '';

        // Params table
        var paramsBody = $('[data-explorer-params-body]');
        var paramsTable = $('[data-explorer-params-table]');
        var paramsEmpty = $('[data-explorer-params-empty]');
        if (paramsBody) paramsBody.innerHTML = '';
        var allParams = []
            .concat((ep.routeParams || []).map(function (p) { return Object.assign({}, p, { kind: 'route' }); }))
            .concat((ep.queryParams || []).map(function (p) { return Object.assign({}, p, { kind: 'query' }); }));
        if (allParams.length === 0) {
            if (paramsTable) paramsTable.setAttribute('hidden', '');
            if (paramsEmpty) paramsEmpty.removeAttribute('hidden');
        } else {
            if (paramsEmpty) paramsEmpty.setAttribute('hidden', '');
            if (paramsTable) paramsTable.removeAttribute('hidden');
            allParams.forEach(function (p) {
                paramsBody.appendChild(buildParamRow(p));
            });
        }
        updateParamsCount();

        // Body tab
        var bodyTab = $('[data-explorer-request-tab="body"]');
        var bodyPane = $('[data-explorer-request-pane="body"]');
        var bodyEditor = $('[data-explorer-body]');
        var bodyAllowed = ep.method !== 'GET' && ep.method !== 'HEAD' && ep.method !== 'DELETE';
        if (bodyTab) {
            if (bodyAllowed) bodyTab.removeAttribute('hidden');
            else bodyTab.setAttribute('hidden', '');
        }
        if (bodyEditor) {
            bodyEditor.value = state.bodyText;
            bodyEditor.disabled = !bodyAllowed;
        }
        if (!bodyAllowed) {
            // Ensure body tab is not the active one when irrelevant.
            activateRequestTab('params');
        }

        rebuildHeadersTable();
        updateUrlPreview();
    }

    function buildParamRow(p) {
        var row = el('tr');

        // Key column with type hint
        var keyCell = el('td');
        var keyCode = el('code', { text: p.name });
        keyCell.appendChild(keyCode);
        if ((p.type || '').toLowerCase().indexOf('required') !== -1) {
            keyCell.appendChild(el('span', {
                'class': 'api-explorer__kv-required',
                'aria-label': 'required',
                text: '*'
            }));
        }

        // Value input
        var valueCell = el('td');
        var input = el('input', {
            type: 'text',
            'class': 'api-explorer__kv-input',
            placeholder: p.type || '',
            'data-param-name': p.name,
            'data-param-kind': p.kind,
            autocomplete: 'off',
            spellcheck: 'false'
        });
        input.value = state.paramValues[p.kind + ':' + p.name] || '';
        input.addEventListener('input', function () {
            state.paramValues[p.kind + ':' + p.name] = input.value;
            updateUrlPreview();
        });
        valueCell.appendChild(input);

        // Type column
        var typeCell = el('td');
        typeCell.appendChild(el('code', { text: p.type || '' }));
        typeCell.appendChild(document.createTextNode(' ' + (p.kind === 'route' ? '(path)' : '(query)')));

        // Description column
        var descCell = el('td', { text: p.description || '' });

        row.appendChild(keyCell);
        row.appendChild(valueCell);
        row.appendChild(typeCell);
        row.appendChild(descCell);
        return row;
    }

    function updateParamsCount() {
        var n = ($('[data-explorer-params-body]') || { children: [] }).children.length;
        var badge = $('[data-explorer-params-count]');
        if (badge) badge.textContent = n ? '· ' + n : '';
    }

    /* ----------------------------- URL preview ----------------------------- */
    function getBaseUrl() {
        var sel = $('[data-explorer-env]');
        var v = sel ? sel.value : '';
        if (!v) return window.location.origin;
        return v.replace(/\/$/, '');
    }

    function buildPath() {
        var ep = endpointsById[state.currentEndpointId];
        if (!ep) return '';
        var path = ep.path;
        var query = [];
        $$('[data-param-kind]').forEach(function (input) {
            var name = input.getAttribute('data-param-name');
            var kind = input.getAttribute('data-param-kind');
            var v = input.value.trim();
            if (!v) return;
            if (kind === 'route') {
                path = path.replace('{' + name + '}', encodeURIComponent(v));
            } else {
                query.push(encodeURIComponent(name) + '=' + encodeURIComponent(v));
            }
        });
        if (query.length) path += (path.indexOf('?') === -1 ? '?' : '&') + query.join('&');
        return path;
    }

    function updateUrlPreview() {
        var host = $('[data-explorer-url-host]');
        var pathEl = $('[data-explorer-url-path]');
        var base = getBaseUrl();
        if (host) host.textContent = base;
        if (!pathEl) return;
        var ep = endpointsById[state.currentEndpointId];
        if (!ep) { pathEl.textContent = ''; return; }
        var path = ep.path;
        var values = {};
        $$('[data-param-kind]').forEach(function (input) {
            var name = input.getAttribute('data-param-name');
            var v = input.value.trim();
            if (v) values[name] = v;
        });
        // Highlight {param} placeholders that are still unresolved.
        var rendered = '';
        var idx = 0;
        var re = /\{([^}]+)\}/g;
        var m;
        while ((m = re.exec(path)) !== null) {
            rendered += escapeHtml(path.slice(idx, m.index));
            var name = m[1];
            var v = values[name];
            if (v) {
                rendered += escapeHtml(encodeURIComponent(v));
            } else {
                rendered += '<span class="api-explorer__url-param">{' + escapeHtml(name) + '}</span>';
            }
            idx = m.index + m[0].length;
        }
        rendered += escapeHtml(path.slice(idx));
        // Query string
        var query = [];
        $$('[data-param-kind="query"]').forEach(function (input) {
            var name = input.getAttribute('data-param-name');
            var v = input.value.trim();
            if (!v) return;
            query.push(escapeHtml(encodeURIComponent(name)) + '=' + escapeHtml(encodeURIComponent(v)));
        });
        if (query.length) rendered += (path.indexOf('?') === -1 ? '?' : '&') + query.join('&');
        pathEl.innerHTML = rendered;
    }

    /* ----------------------------- headers preview ----------------------------- */
    function rebuildHeadersTable() {
        var body = $('[data-explorer-headers-body]');
        var ep = endpointsById[state.currentEndpointId];
        if (!body || !ep) return;
        body.innerHTML = '';
        var rows = [
            ['Accept', 'application/json'],
            ['Authorization', authHeaderPreview()]
        ];
        if (ep.method !== 'GET' && ep.method !== 'HEAD' && ep.method !== 'DELETE') {
            rows.push(['Content-Type', 'application/json']);
        }
        rows.forEach(function (r) {
            var tr = el('tr');
            tr.appendChild(el('td', null, el('code', { text: r[0] })));
            tr.appendChild(el('td', { text: r[1] }));
            body.appendChild(tr);
        });
        var badge = $('[data-explorer-headers-count]');
        if (badge) badge.textContent = '· ' + rows.length;
    }

    function maskToken(token) {
        if (!token || token.length <= 10) return '••••••';
        return token.slice(0, 4) + '••••••' + token.slice(-4);
    }

    /* ----------------------------- tabs ----------------------------- */
    function activateRequestTab(name) {
        $$('[data-explorer-request-tab]').forEach(function (tab) {
            var active = tab.getAttribute('data-explorer-request-tab') === name;
            tab.setAttribute('aria-selected', active ? 'true' : 'false');
        });
        $$('[data-explorer-request-pane]').forEach(function (pane) {
            var active = pane.getAttribute('data-explorer-request-pane') === name;
            if (active) pane.removeAttribute('hidden');
            else pane.setAttribute('hidden', '');
        });
    }

    function activateResponseTab(name) {
        $$('[data-explorer-response-tab]').forEach(function (tab) {
            var active = tab.getAttribute('data-explorer-response-tab') === name;
            tab.setAttribute('aria-selected', active ? 'true' : 'false');
        });
        $$('[data-explorer-response-pane]').forEach(function (pane) {
            var active = pane.getAttribute('data-explorer-response-pane') === name;
            if (active) pane.removeAttribute('hidden');
            else pane.setAttribute('hidden', '');
        });
    }

    function wireTabs() {
        $$('[data-explorer-request-tab]').forEach(function (tab) {
            tab.addEventListener('click', function () {
                activateRequestTab(tab.getAttribute('data-explorer-request-tab'));
            });
        });
        $$('[data-explorer-response-tab]').forEach(function (tab) {
            tab.addEventListener('click', function () {
                activateResponseTab(tab.getAttribute('data-explorer-response-tab'));
            });
        });
    }

    /* ----------------------------- authentication ----------------------------- */
    var TOKEN_KEY = 'compass-docs-api-token';
    var userTokens = [];

    var authState = {
        mode: 'session',
        managedTokenId: null,
        managedBearer: null,
        managedTokenName: null
    };

    function readToken() {
        if (authState.mode === 'managed' && authState.managedBearer) {
            return authState.managedBearer;
        }
        if (authState.mode === 'paste') {
            var input = $('[data-explorer-token]');
            return input ? input.value.trim() : '';
        }
        return '';
    }

    function authHeaderPreview() {
        if (authState.mode === 'managed' && authState.managedBearer) {
            var label = authState.managedTokenName || 'your API key';
            return 'Bearer ' + maskToken(authState.managedBearer) + ' (using ' + label + ' — not shown)';
        }
        var token = readToken();
        if (token) return 'Bearer ' + maskToken(token);
        return '(no token set — request will use your session cookie)';
    }

    function updateAuthUi() {
        var managedWrap = $('[data-explorer-auth-managed]');
        var pasteWrap = $('[data-explorer-paste-wrap]');
        var rememberWrap = $('[data-explorer-remember-wrap]');
        var statusEl = $('[data-explorer-managed-status]');
        var reveal = $('[data-explorer-token-reveal]');
        var tokenInput = $('[data-explorer-token]');

        if (managedWrap) {
            managedWrap.hidden = userTokens.length === 0;
        }

        var isManaged = authState.mode === 'managed';
        var isPaste = authState.mode === 'paste';

        if (pasteWrap) pasteWrap.hidden = !isPaste;
        if (rememberWrap) rememberWrap.hidden = !isPaste;
        if (reveal) reveal.hidden = !isPaste;

        if (statusEl) {
            if (isManaged && authState.managedTokenName) {
                statusEl.hidden = false;
                statusEl.textContent = 'Using ' + authState.managedTokenName + '. The bearer token is not shown.';
            } else {
                statusEl.hidden = true;
                statusEl.textContent = '';
            }
        }

        if (tokenInput && !isPaste) {
            tokenInput.value = '';
            tokenInput.type = 'password';
        }

        $$('[data-explorer-auth-mode]').forEach(function (radio) {
            radio.checked = radio.value === authState.mode;
        });
    }

    function clearManagedBearer() {
        authState.managedBearer = null;
        authState.managedTokenId = null;
        authState.managedTokenName = null;
    }

    function loadManagedBearer(tokenId) {
        return fetch('/docs/api-explorer/bearer/' + encodeURIComponent(tokenId), {
            credentials: 'same-origin',
            headers: { Accept: 'application/json' }
        }).then(function (res) {
            if (!res.ok) throw new Error('Could not load API key');
            return res.json();
        }).then(function (data) {
            authState.managedBearer = data.bearer || null;
            authState.managedTokenId = tokenId;
            var summary = null;
            for (var i = 0; i < userTokens.length; i++) {
                if (userTokens[i].id === tokenId) { summary = userTokens[i]; break; }
            }
            authState.managedTokenName = summary ? summary.name : null;
            rebuildHeadersTable();
            updateAuthUi();
        });
    }

    function wireAuthUi() {
        var select = $('[data-explorer-my-token-select]');
        var tokenInput = $('[data-explorer-token]');

        if (select && userTokens.length > 0) {
            select.innerHTML = '';
            userTokens.forEach(function (t) {
                var label = t.name;
                if (t.accessTier) label += ' (' + t.accessTier + ')';
                select.appendChild(el('option', { value: String(t.id) }, label));
            });
            select.addEventListener('change', function () {
                var id = parseInt(select.value, 10);
                if (!id) return;
                loadManagedBearer(id).catch(function () {
                    clearManagedBearer();
                    updateAuthUi();
                });
            });
        }

        $$('[data-explorer-auth-mode]').forEach(function (radio) {
            radio.addEventListener('change', function () {
                authState.mode = radio.value;
                if (authState.mode === 'managed') {
                    if (select && select.value) {
                        loadManagedBearer(parseInt(select.value, 10));
                    } else if (userTokens.length > 0) {
                        select.value = String(userTokens[0].id);
                        loadManagedBearer(userTokens[0].id);
                    }
                } else {
                    clearManagedBearer();
                    rebuildHeadersTable();
                    updateAuthUi();
                }
            });
        });

        if (tokenInput) {
            tokenInput.addEventListener('input', function () {
                if (authState.mode === 'paste') {
                    persistToken();
                    rebuildHeadersTable();
                }
            });
        }

        updateAuthUi();
    }

    function restoreToken() {
        var input = $('[data-explorer-token]');
        var remember = $('[data-explorer-token-remember]');
        var stored = '';
        try { stored = localStorage.getItem(TOKEN_KEY) || sessionStorage.getItem(TOKEN_KEY) || ''; }
        catch (_) { /* ignore */ }

        if (stored && input) {
            authState.mode = 'paste';
            input.value = stored;
            if (remember) {
                try { remember.checked = !!localStorage.getItem(TOKEN_KEY); } catch (_) { /* ignore */ }
            }
        } else if (userTokens.length > 0) {
            authState.mode = 'managed';
            var select = $('[data-explorer-my-token-select]');
            var managedRadio = document.getElementById('api-explorer-auth-managed');
            if (managedRadio) managedRadio.checked = true;
            if (select) select.value = String(userTokens[0].id);
            loadManagedBearer(userTokens[0].id).catch(function () {
                clearManagedBearer();
                updateAuthUi();
            });
        } else {
            authState.mode = stored ? 'paste' : 'session';
        }

        updateAuthUi();
    }

    function persistToken() {
        if (authState.mode !== 'paste') return;
        var input = $('[data-explorer-token]');
        var remember = $('[data-explorer-token-remember]');
        if (!input) return;
        var v = input.value.trim();
        try {
            if (!v) {
                localStorage.removeItem(TOKEN_KEY);
                sessionStorage.removeItem(TOKEN_KEY);
                return;
            }
            if (remember && remember.checked) {
                localStorage.setItem(TOKEN_KEY, v);
                sessionStorage.removeItem(TOKEN_KEY);
            } else {
                sessionStorage.setItem(TOKEN_KEY, v);
                localStorage.removeItem(TOKEN_KEY);
            }
        } catch (_) { /* private mode */ }
    }

    /* ----------------------------- sending ----------------------------- */
    function setStatus(label, meta, kind) {
        var pill = $('[data-explorer-status-pill]');
        var metaEl = $('[data-explorer-status-meta]');
        if (pill) {
            pill.textContent = label;
            pill.classList.remove(
                'api-explorer__status-pill--idle',
                'api-explorer__status-pill--busy',
                'api-explorer__status-pill--ok-2xx',
                'api-explorer__status-pill--redirect-3xx',
                'api-explorer__status-pill--client-4xx',
                'api-explorer__status-pill--server-5xx',
                'api-explorer__status-pill--err'
            );
            if (kind) pill.classList.add('api-explorer__status-pill--' + kind);
        }
        if (metaEl) metaEl.textContent = meta || '';
    }

    function statusKindFor(code) {
        if (code >= 200 && code < 300) return 'ok-2xx';
        if (code >= 300 && code < 400) return 'redirect-3xx';
        if (code >= 400 && code < 500) return 'client-4xx';
        if (code >= 500) return 'server-5xx';
        return 'err';
    }

    function applyResponsePayload(status, statusText, text, hdrs, elapsed) {
        var size = new Blob([text]).size;
        var sizeLabel = size < 1024
            ? size + ' B'
            : size < 1024 * 1024
                ? (size / 1024).toFixed(1) + ' KB'
                : (size / 1024 / 1024).toFixed(2) + ' MB';
        var meta = status + ' ' + (statusText || '') + ' · ' + elapsed + ' ms · ' + sizeLabel;
        setStatus(String(status), meta, statusKindFor(status));
        state.lastResponseText = text;
        state.lastResponseHeaders = hdrs || {};
        try { state.lastResponse = JSON.parse(text); }
        catch (_) { state.lastResponse = text; }
        state.lastResponseRows = extractRows(state.lastResponse);
        renderResponseBody(state.lastResponse, text);
        renderResponseHeaders(state.lastResponseHeaders);
        renderResponsePreview(state.lastResponseRows);
        setExportEnabled(true, state.lastResponseRows.length > 0);
    }

    function send() {
        var ep = endpointsById[state.currentEndpointId];
        if (!ep) return;
        var base = getBaseUrl();
        var path = buildPath();
        var headers = { 'Accept': 'application/json' };
        var token = readToken();
        if (token) headers['Authorization'] = 'Bearer ' + token;
        var requestBody = null;

        if (ep.method !== 'GET' && ep.method !== 'HEAD' && ep.method !== 'DELETE') {
            var bodyEditor = $('[data-explorer-body]');
            var raw = bodyEditor ? bodyEditor.value : '';
            if (raw && raw.trim()) {
                try { JSON.parse(raw); }
                catch (e) {
                    var errEl = $('[data-explorer-body-error]');
                    if (errEl) {
                        errEl.textContent = 'Request body is not valid JSON: ' + e.message;
                        errEl.removeAttribute('hidden');
                    }
                    activateRequestTab('body');
                    return;
                }
                headers['Content-Type'] = 'application/json';
                requestBody = raw;
            }
        }

        var sendBtn = $('[data-explorer-send]');
        if (sendBtn) { sendBtn.disabled = true; sendBtn.classList.add('api-explorer__send--busy'); sendBtn.textContent = 'Sending…'; }
        setStatus('Sending', '', 'busy');

        var t0 = performance.now();
        // Cross-origin targets must use the server proxy (browser CSP connect-src). Same-origin
        // session-cookie auth uses a direct fetch so cookies are sent by the browser.
        var useProxy = isCrossOrigin(base);

        function finish() {
            if (sendBtn) {
                sendBtn.disabled = false;
                sendBtn.classList.remove('api-explorer__send--busy');
                sendBtn.textContent = 'Send';
            }
        }

        if (useProxy) {
            fetch(proxyUrl, {
                method: 'POST',
                credentials: 'same-origin',
                headers: { 'Accept': 'application/json', 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    baseUrl: base,
                    method: ep.method,
                    path: path,
                    body: requestBody,
                    authorization: token ? 'Bearer ' + token : null
                })
            })
                .then(function (res) {
                    return res.json().then(function (payload) {
                        if (!res.ok) {
                            throw new Error(payload && payload.error ? payload.error : 'Proxy request failed');
                        }
                        applyResponsePayload(
                            payload.status,
                            payload.statusText || '',
                            payload.body || '',
                            payload.headers || {},
                            Math.round(performance.now() - t0));
                    });
                })
                .catch(function (err) {
                    setStatus('Network error', err.message || String(err), 'err');
                    setExportEnabled(false, false);
                    renderResponseBody('Network error: ' + (err.message || String(err)), '');
                })
                .finally(finish);
            return;
        }

        var init = { method: ep.method, headers: headers, credentials: 'include' };
        if (requestBody) init.body = requestBody;

        fetch(path, init)
            .then(function (res) {
                var elapsed = Math.round(performance.now() - t0);
                var hdrs = {};
                try {
                    res.headers.forEach(function (v, k) { hdrs[k] = v; });
                } catch (_) { /* ignore */ }
                return res.text().then(function (text) {
                    applyResponsePayload(res.status, res.statusText || '', text, hdrs, elapsed);
                });
            })
            .catch(function (err) {
                setStatus('Network error', err.message || String(err), 'err');
                setExportEnabled(false, false);
                renderResponseBody('Network error: ' + (err.message || String(err)), '');
            })
            .finally(finish);
    }

    function resetResponse() {
        setStatus('Ready', '', 'idle');
        var code = $('[data-explorer-response-code]');
        if (code) {
            code.innerHTML = '<span class="api-explorer__response-placeholder">Send a request to see the response here.</span>';
        }
        var hdrBody = $('[data-explorer-resp-headers-body]');
        if (hdrBody) hdrBody.innerHTML = '';
        var hdrEmpty = $('[data-explorer-resp-headers-empty]');
        if (hdrEmpty) hdrEmpty.removeAttribute('hidden');
        var prevWrap = $('[data-explorer-preview-wrapper]');
        if (prevWrap) prevWrap.setAttribute('hidden', '');
        var prevEmpty = $('[data-explorer-preview-empty]');
        if (prevEmpty) prevEmpty.removeAttribute('hidden');
        setExportEnabled(false, false);
        activateResponseTab('body');
    }

    function setExportEnabled(any, tabular) {
        var copy = $('[data-explorer-copy]');
        var dlJson = $('[data-explorer-download-json]');
        var dlCsv = $('[data-explorer-download-csv]');
        var dlXlsx = $('[data-explorer-download-xlsx]');
        if (copy) copy.disabled = !any;
        if (dlJson) dlJson.disabled = !any;
        if (dlCsv) dlCsv.disabled = !tabular;
        if (dlXlsx) dlXlsx.disabled = !tabular;
    }

    /* ----------------------------- response rendering ----------------------------- */
    function renderResponseBody(parsed, rawText) {
        var code = $('[data-explorer-response-code]');
        if (!code) return;
        if (parsed === null || parsed === undefined) {
            code.textContent = rawText || '(empty response)';
            return;
        }
        if (typeof parsed === 'string') {
            code.textContent = parsed;
            return;
        }
        var pretty = JSON.stringify(parsed, null, 2);
        code.innerHTML = syntaxHighlight(pretty);
    }

    function renderResponseHeaders(hdrs) {
        var body = $('[data-explorer-resp-headers-body]');
        var empty = $('[data-explorer-resp-headers-empty]');
        var count = $('[data-explorer-resp-headers-count]');
        if (!body) return;
        body.innerHTML = '';
        var names = Object.keys(hdrs).sort();
        if (empty) {
            if (names.length === 0) empty.removeAttribute('hidden');
            else empty.setAttribute('hidden', '');
        }
        names.forEach(function (n) {
            var tr = el('tr');
            tr.appendChild(el('td', null, el('code', { text: n })));
            tr.appendChild(el('td', { text: hdrs[n] }));
            body.appendChild(tr);
        });
        if (count) count.textContent = names.length ? '· ' + names.length : '';
    }

    function renderResponsePreview(rows) {
        var wrap = $('[data-explorer-preview-wrapper]');
        var empty = $('[data-explorer-preview-empty]');
        var summary = $('[data-explorer-preview-summary]');
        var table = $('[data-explorer-preview-table]');
        if (!wrap || !table) return;
        if (!rows || rows.length === 0) {
            wrap.setAttribute('hidden', '');
            if (empty) empty.removeAttribute('hidden');
            return;
        }
        if (empty) empty.setAttribute('hidden', '');
        wrap.removeAttribute('hidden');
        var flat = flattenRows(rows);
        // Cap preview to first 50 rows so giant payloads don't lock the browser.
        var capped = flat.rows.slice(0, 50);
        if (summary) {
            summary.textContent = capped.length + ' of ' + flat.rows.length + ' row' +
                (flat.rows.length === 1 ? '' : 's') + ' · ' + flat.headers.length + ' columns';
        }
        table.innerHTML = '';
        var thead = el('thead', { 'class': 'govuk-table__head' });
        var headerRow = el('tr', { 'class': 'govuk-table__row' });
        flat.headers.forEach(function (h) {
            headerRow.appendChild(el('th', {
                scope: 'col', 'class': 'govuk-table__header', text: h
            }));
        });
        thead.appendChild(headerRow);
        table.appendChild(thead);
        var tbody = el('tbody', { 'class': 'govuk-table__body' });
        capped.forEach(function (r) {
            var tr = el('tr', { 'class': 'govuk-table__row' });
            flat.headers.forEach(function (h) {
                var v = r[h];
                tr.appendChild(el('td', {
                    'class': 'govuk-table__cell',
                    text: (v === null || v === undefined) ? '' : String(v)
                }));
            });
            tbody.appendChild(tr);
        });
        table.appendChild(tbody);
    }

    /* ----------------------------- JSON syntax highlight ----------------------------- */
    function syntaxHighlight(json) {
        // Standard implementation: classify tokens via a single regex.
        var escaped = escapeHtml(json);
        return escaped.replace(
            /("(\\u[a-fA-F0-9]{4}|\\[^u]|[^\\"])*"(\s*:)?|\b(true|false|null)\b|-?\d+(?:\.\d+)?(?:[eE][+-]?\d+)?)/g,
            function (match) {
                var cls = 'api-explorer__json-number';
                if (/^"/.test(match)) {
                    cls = /:$/.test(match) ? 'api-explorer__json-key' : 'api-explorer__json-string';
                } else if (/true|false/.test(match)) {
                    cls = 'api-explorer__json-bool';
                } else if (/null/.test(match)) {
                    cls = 'api-explorer__json-null';
                }
                return '<span class="' + cls + '">' + match + '</span>';
            }
        );
    }

    /* ----------------------------- rows / export ----------------------------- */
    function extractRows(data) {
        if (!data) return [];
        if (Array.isArray(data)) return data;
        if (typeof data === 'object') {
            if (Array.isArray(data.data)) return data.data;
            if (Array.isArray(data.items)) return data.items;
            return [data];
        }
        return [];
    }

    function flattenRow(obj, prefix, out) {
        out = out || {};
        prefix = prefix || '';
        if (obj === null || obj === undefined) {
            out[prefix.replace(/\.$/, '')] = '';
            return out;
        }
        if (typeof obj !== 'object') {
            out[prefix.replace(/\.$/, '')] = obj;
            return out;
        }
        if (Array.isArray(obj)) {
            if (obj.every(function (v) { return typeof v !== 'object' || v === null; })) {
                out[prefix.replace(/\.$/, '')] = obj.join(';');
            } else {
                out[prefix.replace(/\.$/, '')] = JSON.stringify(obj);
            }
            return out;
        }
        Object.keys(obj).forEach(function (k) {
            flattenRow(obj[k], prefix + k + '.', out);
        });
        return out;
    }

    function flattenRows(rows) {
        var flat = rows.map(function (r) { return flattenRow(r); });
        var keys = {};
        flat.forEach(function (r) { Object.keys(r).forEach(function (k) { keys[k] = true; }); });
        return { headers: Object.keys(keys), rows: flat };
    }

    function csvEscape(v) {
        if (v === null || v === undefined) return '';
        var s = String(v);
        if (/[",\n\r]/.test(s)) return '"' + s.replace(/"/g, '""') + '"';
        return s;
    }

    function downloadBlob(blob, name) {
        var a = document.createElement('a');
        a.href = URL.createObjectURL(blob);
        a.download = name;
        document.body.appendChild(a);
        a.click();
        setTimeout(function () {
            document.body.removeChild(a);
            URL.revokeObjectURL(a.href);
        }, 200);
    }

    function fileBaseName() {
        var ep = endpointsById[state.currentEndpointId];
        if (!ep) return 'compass-api';
        var slug = (ep.method + '-' + ep.path).toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/(^-|-$)/g, '');
        return 'compass-' + slug + '-' + new Date().toISOString().slice(0, 10);
    }

    function downloadJson() {
        if (state.lastResponse === null) return;
        var text = typeof state.lastResponse === 'string'
            ? state.lastResponse
            : JSON.stringify(state.lastResponse, null, 2);
        var blob = new Blob([text], { type: 'application/json' });
        downloadBlob(blob, fileBaseName() + '.json');
    }

    function downloadCsv() {
        if (!state.lastResponseRows.length) return;
        var flat = flattenRows(state.lastResponseRows);
        var lines = [flat.headers.map(csvEscape).join(',')];
        flat.rows.forEach(function (r) {
            lines.push(flat.headers.map(function (h) { return csvEscape(r[h]); }).join(','));
        });
        var blob = new Blob(['\uFEFF' + lines.join('\n')], { type: 'text/csv;charset=utf-8;' });
        downloadBlob(blob, fileBaseName() + '.csv');
    }

    var xlsxLoader = null;
    function loadXlsx() {
        if (xlsxLoader) return xlsxLoader;
        xlsxLoader = new Promise(function (resolve, reject) {
            if (window.XLSX) return resolve(window.XLSX);
            var script = document.createElement('script');
            script.src = 'https://cdn.jsdelivr.net/npm/xlsx@0.18.5/dist/xlsx.full.min.js';
            var nonceCarrier = document.querySelector('script[nonce]');
            if (nonceCarrier && nonceCarrier.nonce) script.nonce = nonceCarrier.nonce;
            script.onload = function () { resolve(window.XLSX); };
            script.onerror = function () { reject(new Error('Failed to load Excel export library.')); };
            document.head.appendChild(script);
        });
        return xlsxLoader;
    }

    function downloadXlsx() {
        if (!state.lastResponseRows.length) return;
        loadXlsx().then(function (XLSX) {
            var flat = flattenRows(state.lastResponseRows);
            var aoa = [flat.headers].concat(flat.rows.map(function (r) {
                return flat.headers.map(function (h) {
                    var v = r[h];
                    return (v === undefined || v === null) ? '' : v;
                });
            }));
            var sheet = XLSX.utils.aoa_to_sheet(aoa);
            var book = XLSX.utils.book_new();
            XLSX.utils.book_append_sheet(book, sheet, 'Response');
            var data = XLSX.write(book, { bookType: 'xlsx', type: 'array' });
            var blob = new Blob([data], { type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet' });
            downloadBlob(blob, fileBaseName() + '.xlsx');
        }).catch(function (err) {
            setStatus('Export error', err.message || String(err), 'err');
        });
    }

    function copyResponse() {
        if (state.lastResponse === null) return;
        var text = typeof state.lastResponse === 'string'
            ? state.lastResponse
            : JSON.stringify(state.lastResponse, null, 2);
        if (navigator.clipboard) {
            navigator.clipboard.writeText(text).catch(function () { /* ignore */ });
        }
        var btn = $('[data-explorer-copy]');
        if (btn) {
            var old = btn.textContent;
            btn.textContent = 'Copied';
            setTimeout(function () { btn.textContent = old; }, 1200);
        }
    }

    /* ----------------------------- body editor tools ----------------------------- */
    function formatBody() {
        var editor = $('[data-explorer-body]');
        var errEl = $('[data-explorer-body-error]');
        if (!editor) return;
        var raw = editor.value;
        if (!raw.trim()) return;
        try {
            editor.value = JSON.stringify(JSON.parse(raw), null, 2);
            if (errEl) errEl.setAttribute('hidden', '');
        } catch (e) {
            if (errEl) {
                errEl.textContent = 'Not valid JSON: ' + e.message;
                errEl.removeAttribute('hidden');
            }
        }
    }

    function resetBody() {
        var ep = endpointsById[state.currentEndpointId];
        var editor = $('[data-explorer-body]');
        if (editor) editor.value = ep && ep.bodyExample ? ep.bodyExample.trim() : '';
        var errEl = $('[data-explorer-body-error]');
        if (errEl) errEl.setAttribute('hidden', '');
    }

    /* ----------------------------- bootstrap ----------------------------- */
    function wire() {
        wireTabs();

        var search = $('[data-explorer-search]');
        if (search) search.addEventListener('input', function () { renderTree(search.value); });

        var env = $('[data-explorer-env]');
        if (env) env.addEventListener('change', function () { updateUrlPreview(); });

        var token = $('[data-explorer-token]');
        var remember = $('[data-explorer-token-remember]');
        if (token) token.addEventListener('input', function () { persistToken(); rebuildHeadersTable(); });
        if (remember) remember.addEventListener('change', persistToken);

        var reveal = $('[data-explorer-token-reveal]');
        if (reveal && token) {
            reveal.addEventListener('click', function () {
                var pressed = reveal.getAttribute('aria-pressed') === 'true';
                reveal.setAttribute('aria-pressed', pressed ? 'false' : 'true');
                token.type = pressed ? 'password' : 'text';
                reveal.textContent = pressed ? 'Show' : 'Hide';
                reveal.setAttribute('aria-label', pressed ? 'Show token' : 'Hide token');
            });
        }

        var send_ = $('[data-explorer-send]');
        if (send_) send_.addEventListener('click', send);

        var requestToggle = $('[data-explorer-request-toggle]');
        if (requestToggle) {
            requestToggle.addEventListener('click', function () {
                var builder = $('[data-explorer-builder]');
                if (!builder) return;
                var collapsed = builder.classList.toggle('api-explorer__builder--request-collapsed');
                requestToggle.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
                var label = $('[data-explorer-request-toggle-label]');
                if (label) label.textContent = collapsed ? 'Show request' : 'Hide request';
                var hint = $('[data-explorer-request-toggle-hint]');
                if (hint) hint.textContent = collapsed
                    ? 'Scope, parameters and body hidden'
                    : 'Scope, parameters and body';
            });
        }

        document.addEventListener('keydown', function (e) {
            if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
                var builder = $('[data-explorer-builder]');
                if (builder && !builder.hasAttribute('hidden')) {
                    e.preventDefault();
                    send();
                }
            }
        });

        var format = $('[data-explorer-body-format]');
        if (format) format.addEventListener('click', formatBody);
        var resetBtn = $('[data-explorer-body-reset]');
        if (resetBtn) resetBtn.addEventListener('click', resetBody);

        var bodyEditor = $('[data-explorer-body]');
        if (bodyEditor) bodyEditor.addEventListener('input', function () {
            state.bodyText = bodyEditor.value;
            var errEl = $('[data-explorer-body-error]');
            if (errEl) errEl.setAttribute('hidden', '');
        });

        var copyBtn = $('[data-explorer-copy]');
        if (copyBtn) copyBtn.addEventListener('click', copyResponse);
        var dlJson = $('[data-explorer-download-json]');
        if (dlJson) dlJson.addEventListener('click', downloadJson);
        var dlCsv = $('[data-explorer-download-csv]');
        if (dlCsv) dlCsv.addEventListener('click', downloadCsv);
        var dlXlsx = $('[data-explorer-download-xlsx]');
        if (dlXlsx) dlXlsx.addEventListener('click', downloadXlsx);
    }

    function init() {
        wire();
        wireAuthUi();
        restoreToken();
        renderTree('');

        var hash = (window.location.hash || '').slice(1);
        if (hash && endpointsById[hash]) {
            selectEndpoint(hash);
        } else {
            var firstSection = catalogue.find(function (s) { return s.endpoints.length > 0; });
            if (firstSection) selectEndpoint(firstSection.endpoints[0].id);
        }

        window.addEventListener('hashchange', function () {
            var h = (window.location.hash || '').slice(1);
            if (h && endpointsById[h] && h !== state.currentEndpointId) selectEndpoint(h);
        });
    }

    function bootstrap() {
        loadExplorerData().then(init).catch(function () { init(); });
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', bootstrap);
    } else {
        bootstrap();
    }
})();
