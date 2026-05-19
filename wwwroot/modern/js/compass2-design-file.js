/* ── Navigation maps ─────────────────────────────────────────── */
const NAV_MAP = {
  'home': 'home',
  'demand-dash': 'demand', 'demand-register': 'demand', 'demand-horizon': 'demand',
  'demand-review': 'demand', 'demand-scoring': 'demand', 'demand-triage': 'demand',
  'demand-capacity': 'demand', 'create-bc': 'demand', 'create-demand': 'demand', 'demand-horizon': 'demand', 'demand-score-item': 'demand', 'demand-meetings': 'demand', 'demand-closed': 'demand',
  'work-dash': 'work', 'work-register': 'work', 'work-yours': 'work',
  'work-portfolios': 'work', 'work-directorates': 'work', 'work-business-areas': 'work', 'work-priorities': 'work',
  'work-detail': 'work', 'work-by-theme': 'work', 'create-work': 'work',
  'work-add-update': 'work', 'work-view-update': 'work',
  'products-dash': 'products', 'products-index': 'products', 'products-yours': 'products',
  'products-portfolio': 'products', 'product-detail': 'products',
  'perf-dash': 'perf', 'perf-products': 'perf', 'perf-monthly': 'perf',
  'perf-quarterly': 'perf', 'perf-adhoc': 'perf', 'perf-submit': 'perf', 'perf-submissions': 'perf',
  'risks-dash': 'risks', 'risks-register': 'risks', 'risks-issues': 'risks', 'risk-view': 'risks', 'issue-view': 'risks', 'demand-view': 'demand',
  'risks-portfolio': 'risks', 'risks-intel': 'risks', 'create-risk': 'risks',
  'create-issue': 'risks', 'create-milestone': 'risks',
  'standards-dash': 'standards', 'standards-ddt': 'standards',
  'standards-functional': 'standards', 'assessments-list': 'standards',
  'govs005': 'standards', 'govs005-report': 'standards',
  'health-check': 'standards',
  'reporting-hub': 'reporting', 'reporting-cross': 'reporting',
  'reporting-monthly-report': 'reporting', 'reporting-risks': 'reporting',
  'reporting-standards': 'reporting', 'reporting-perf-report': 'reporting',
  'reporting-exports': 'reporting', 'reporting-monthly': 'reporting', 'reporting-performance': 'reporting', 'reporting-assessments': 'reporting', 'reporting-raid': 'reporting',
  'reporting-perf': 'reporting',
  'admin': 'admin', 'notifications': 'home',
  'global-search': 'home', 'user-profile': 'home',
  'product-create': 'products',
  'work-programmes': 'work',
  'risks-kri': 'risks', 'risk-accept': 'risks', 'risks-near-miss': 'risks', 'create-near-miss': 'risks', 'nm-view': 'risks',
  'demand-withdraw': 'demand', 'demand-bc-approve': 'demand',
};

const SUB_MAP = {
  'demand': 'sub-demand', 'work': 'sub-work', 'products': 'sub-products',
  'perf': 'sub-perf', 'risks': 'sub-risks', 'standards': 'sub-standards',
  'reporting': 'sub-reporting', 'admin': 'sub-admin',
};

const BREADCRUMBS = {
  'demand-dash': [['Home', 'home'], 'Demand dashboard'],
  'demand-register': [['Home', 'home'], ['Demand', 'demand-dash'], 'Demand register'],
  'demand-horizon': [['Home', 'home'], ['Demand', 'demand-dash'], 'Business cases'],
  'demand-review': [['Home', 'home'], ['Demand', 'demand-dash'], 'Explore'],
  'demand-scoring': [['Home', 'home'], ['Demand', 'demand-dash'], 'Scoring'],
  'demand-triage': [['Home', 'home'], ['Demand', 'demand-dash'], 'Triage'],
  'demand-score-item': [['Home', 'home'], ['Demand', 'demand-dash'], ['Scoring', 'demand-scoring'], 'DR-000040 — Score demand'],
  'demand-meetings': [['Home', 'home'], ['Demand', 'demand-dash'], 'Meetings'],
  'demand-closed': [['Home', 'home'], ['Demand', 'demand-dash'], 'Closed demands'],
  'demand-scoring': [['Home', 'home'], ['Demand', 'demand-dash'], 'Scoring'],
  'demand-triage': [['Home', 'home'], ['Demand', 'demand-dash'], 'Triage'],
  'demand-score-item': [['Home', 'home'], ['Demand', 'demand-dash'], ['Scoring', 'demand-scoring'], 'DR-000040 — Score demand'],
  'demand-meetings': [['Home', 'home'], ['Demand', 'demand-dash'], 'Meetings'],
  'demand-closed': [['Home', 'home'], ['Demand', 'demand-dash'], 'Closed demands'],
  'create-bc': [['Home', 'home'], ['Demand', 'demand-dash'], ['Business cases', 'demand-horizon'], 'Add business case'],
  'create-demand': [['Home', 'home'], ['Demand', 'demand-dash'], 'Submit demand'],
  'demand-horizon': [['Home', 'home'], ['Demand', 'demand-dash'], 'Business cases'],
  'work-dash': [['Home', 'home'], 'Your work dashboard'],
  'work-yours': [['Home', 'home'], ['Work', 'work-dash'], 'Your work'],
  'work-register': [['Home', 'home'], ['Work', 'work-dash'], 'All work'],
  'work-portfolios': [['Home', 'home'], ['Work', 'work-dash'], 'Portfolios'],
  'work-directorates': [['Home', 'home'], ['Work', 'work-dash'], 'Directorates'],
  'work-business-areas': [['Home', 'home'], ['Work', 'work-dash'], 'Business areas'],
  'work-priorities': [['Home', 'home'], ['Work', 'work-dash'], 'Priorities'],
  'work-detail': [['Home', 'home'], ['Work', 'work-register'], 'WRK-0042'],
  'work-add-update': [['Home', 'home'], ['Work', 'work-dash'], ['WRK-0042', 'work-detail'], 'Submit update'],
  'work-view-update': [['Home', 'home'], ['Work', 'work-dash'], ['WRK-0042', 'work-detail'], 'January update'],
  'create-work': [['Home', 'home'], ['Work', 'work-dash'], 'Create work item'],
  'create-milestone': [['Home', 'home'], ['Work', 'work-register'], ['WRK-0042', 'work-detail'], 'Add milestone'],
  'products-dash': [['Home', 'home'], 'Products dashboard'],
  'products-index': [['Home', 'home'], ['Products', 'products-dash'], 'All products'],
  'products-yours': [['Home', 'home'], ['Products', 'products-dash'], 'Your products'],
  'product-detail': [['Home', 'home'], ['Products', 'products-index'], 'FIPS-0142'],
  'perf-dash': [['Home', 'home'], 'Performance'],
  'perf-submit': [['Home', 'home'], ['Performance', 'perf-dash'], ['Monthly returns', 'perf-monthly'], 'Submit return'],
  'risks-dash': [['Home', 'home'], 'Risks &amp; issues'],
  'risks-register': [['Home', 'home'], ['Risks', 'risks-dash'], 'Risk register'],
  'risks-issues': [['Home', 'home'], ['Risks', 'risks-dash'], 'Issue register'],
  'create-risk': [['Home', 'home'], ['Risks', 'risks-dash'], 'Log risk'],
  'create-issue': [['Home', 'home'], ['Risks', 'risks-dash'], 'Log issue'],
  'standards-dash': [['Home', 'home'], 'Standards'],
  'standards-ddt': [['Home', 'home'], ['Standards', 'standards-dash'], 'DDT standards'],
  'assessments-list': [['Home', 'home'], ['Standards', 'standards-dash'], 'Assessments'],
  'govs005': [['Home', 'home'], ['Standards', 'assessments-list'], 'ASS-0006 — GovS 005'],
  'govs005-report': [['Home', 'home'], ['Standards', 'assessments-list'], ['ASS-0006', 'govs005'], 'Report'],
  'health-check': [['Home', 'home'], ['Standards', 'standards-dash'], 'Service health check'],
  'standards-mgmt': [['Home', 'home'], ['Standards', 'standards-dash'], 'Management queue'],
  'reporting-hub': [['Home', 'home'], 'Reporting'],
  'reporting-cross': [['Home', 'home'], ['Reporting', 'reporting-hub'], 'Cross-system report'],
  'reporting-monthly-report': [['Home', 'home'], ['Reporting', 'reporting-hub'], 'Monthly updates report'],
  'reporting-perf-report': [['Home', 'home'], ['Reporting', 'reporting-hub'], 'Performance report'],
  'reporting-risks': [['Home', 'home'], ['Reporting', 'reporting-hub'], 'Risk & issues'],
  'reporting-exports': [['Home', 'home'], ['Reporting', 'reporting-hub'], 'Exports'],
  'reporting-assessments': [['Home', 'home'], ['Reporting', 'reporting-hub'], 'Service assessments'],
  'admin': [['Home', 'home'], 'Admin'],
  'notifications': [['Home', 'home'], 'Notifications'],
};

/* ── Core showScreen ─────────────────────────────────────────── */
function ss(id) {
  document.querySelectorAll('.dfe-c-screen').forEach(s => s.classList.remove('dfe-c-screen--active'));
  const el = document.getElementById('sc-' + id);
  if (el) el.classList.add('dfe-c-screen--active');

  const group = NAV_MAP[id] || 'home';
  document.querySelectorAll('.dfe-f-header__service-nav a[data-nav]').forEach(a => {
    const match = a.getAttribute('data-nav') === group;
    if (match) {
      a.setAttribute('aria-current', 'page');
    } else {
      a.removeAttribute('aria-current');
    }
  });

  document.querySelectorAll('.dfe-f-sub-navigation.dfe-c-sub-nav').forEach(n => n.classList.remove('dfe-c-sub-nav--visible'));
  const subId = SUB_MAP[group];
  if (subId) { const s = document.getElementById(subId); if (s) s.classList.add('dfe-c-sub-nav--visible'); }

  document.querySelectorAll('.dfe-f-sub-navigation__link[data-sub]').forEach(a => {
    const token = (a.getAttribute('data-sub') || '').trim();
    const tokens = token ? token.split(/\s+/).filter(Boolean) : [];
    const match = tokens.length ? tokens.includes(id) : false;
    if (match) {
      a.setAttribute('aria-current', 'page');
    } else {
      a.removeAttribute('aria-current');
    }
  });

  const crumbs = BREADCRUMBS[id] || [];
  const bar = document.getElementById('bc-bar');
  const inner = document.getElementById('bc-inner');
  if (crumbs.length > 0) {
    bar.classList.add('dfe-c-breadcrumb--visible');
    inner.innerHTML = crumbs.map(c => Array.isArray(c) ? `<a onclick="ss('${c[1]}')">${c[0]}</a>` : `<span class="dfe-c-breadcrumb__sep">›</span><span>${c}</span>`).join('');
  } else {
    bar.classList.remove('dfe-c-breadcrumb--visible');
  }
  window.scrollTo({ top: 0, behavior: 'smooth' });
}

/* ── Tab switching ───────────────────────────────────────────── */
function st(btn, panelId) {
  const parent = btn.closest('.dfe-c-screen') || document.body;
  parent.querySelectorAll('.dfe-c-tabs__tab').forEach(t => t.classList.remove('dfe-c-tabs__tab--active'));
  parent.querySelectorAll('.dfe-c-tabs__panel').forEach(p => p.classList.remove('dfe-c-tabs__panel--active'));
  btn.classList.add('dfe-c-tabs__tab--active');
  const panel = document.getElementById(panelId);
  if (panel) panel.classList.add('dfe-c-tabs__panel--active');
}

/* ── Collapsible panels — + / - ─────────────────────────────── */
function toggleCollapsible(triggerBtn) {
  const panel = triggerBtn.closest('.dfe-c-collapsible');
  const icon = triggerBtn.querySelector('.dfe-c-collapsible__icon');
  const isOpen = panel.classList.contains('dfe-c-collapsible--open');
  panel.classList.toggle('dfe-c-collapsible--open', !isOpen);
  if (icon) icon.textContent = isOpen ? '+' : '−';
}

function scrollToSection(id) {
  const el = document.getElementById(id);
  if (!el) return;
  el.scrollIntoView({ behavior: 'smooth', block: 'start' });
  const coll = el.querySelector('.dfe-c-collapsible');
  if (coll && !coll.classList.contains('dfe-c-collapsible--open')) {
    const t = coll.querySelector('.dfe-c-collapsible__trigger');
    if (t) toggleCollapsible(t);
  }
}

/* ── Performance metric validation ──────────────────────────── */
function checkMetric(name, val, target, dir) {
  const numVal = parseFloat(val);
  const statusEl = document.getElementById('m-' + name + '-status');
  const warnEl = document.getElementById('m-' + name + '-warn');
  const commentEl = document.getElementById('m-' + name + '-comment');
  const card = document.getElementById('m-' + name + '-card');
  if (!statusEl || isNaN(numVal)) return;
  const below = (dir === 'gte') ? numVal < target : numVal > target;
  if (below) {
    statusEl.innerHTML = `<span style="color:var(--rag-r);font-weight:700;">↓ ${numVal.toLocaleString()} — below target of ${target.toLocaleString()}</span>`;
    if (warnEl) warnEl.style.display = '';
    if (commentEl) commentEl.style.display = '';
    if (card) card.style.borderLeftColor = 'var(--rag-r)';
  } else {
    statusEl.innerHTML = `<span style="color:var(--rag-g);font-weight:700;">✓ ${numVal.toLocaleString()} — on target</span>`;
    if (warnEl) warnEl.style.display = 'none';
    if (commentEl) commentEl.style.display = 'none';
    if (card) card.style.borderLeftColor = 'var(--rag-g)';
  }
}

/* ── Monthly update directorate drill ───────────────────────── */
function drilltoDirMU(name) {
  const drill = document.getElementById('mu-directorate-drill');
  const nameEl = document.getElementById('mu-dir-name');
  if (drill && nameEl) {
    nameEl.textContent = name;
    drill.style.display = '';
    drill.scrollIntoView({ behavior: 'smooth', block: 'start' });
  }
}

/* ── Score buttons ───────────────────────────────────────────── */
function scoreBtn(btn, group) {
  const row = btn.closest('.dfe-c-form__score-row');
  row.querySelectorAll('.dfe-c-form__score-btn').forEach(b => b.classList.remove('dfe-c-form__score-btn--selected'));
  btn.classList.add('dfe-c-form__score-btn--selected');
}

/* ── GovS005 ─────────────────────────────────────────────────── */
function g5Score(btn, val, ref) {
  const item = btn.closest('.dfe-c-assessment-item');
  item.classList.remove('dfe-c-assessment-item--full', 'dfe-c-assessment-item--partial');
  item.querySelectorAll('.dfe-c-assessment-item__options button').forEach(b => {
    b.style.background = ''; b.style.color = ''; b.style.borderColor = '';
    b.className = 'dfe-c-button dfe-c-button--secondary dfe-c-button--sm';
  });
  const c = { full: 'var(--rag-g)', partial: 'var(--rag-a)', none: 'var(--rag-r)' };
  if (c[val]) { btn.style.background = c[val]; btn.style.color = 'var(--white)'; btn.style.borderColor = c[val]; }
  if (val === 'full') item.classList.add('dfe-c-assessment-item--full');
  if (val === 'partial') item.classList.add('dfe-c-assessment-item--partial');
  if (val === 'none') item.style.borderLeftColor = 'var(--rag-r)';
  showToast('Progress saved');
}

/* ── Products view toggle ────────────────────────────────────── */
function toggleProdView() {
  const tv = document.getElementById('prod-table-view');
  const cv = document.getElementById('prod-card-view');
  const btn = document.getElementById('prod-view-btn');
  const isTable = tv.style.display !== 'none';
  tv.style.display = isTable ? 'none' : '';
  cv.style.display = isTable ? '' : 'none';
  btn.textContent = isTable ? '☰ Table view' : '🗂 Card view';
}
function filterProds() {
  const q = document.getElementById('prod-search').value.toLowerCase();
  document.querySelectorAll('#prod-tbody tr').forEach(r => {
    r.style.display = r.textContent.toLowerCase().includes(q) ? '' : 'none';
  });
}
function jumpProd(letter) {
  const tv = document.getElementById('prod-table-view');
  if (tv.style.display === 'none') toggleProdView();
  const row = document.querySelector(`#prod-tbody tr[data-letter="${letter}"]`);
  if (row) row.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

/* ── Chips ───────────────────────────────────────────────────── */
document.addEventListener('click', e => {
  if (e.target.classList.contains('dfe-c-chip') && !e.target.dataset.skipToggle)
    e.target.classList.toggle('dfe-c-chip--active');
});

/* ── Toast ───────────────────────────────────────────────────── */
function showToast(msg) {
  const t = document.getElementById('toast');
  const m = document.getElementById('toast-msg');
  if (!t || !m) return;
  m.textContent = msg;
  t.style.display = 'flex';
  clearTimeout(t._t);
  t._t = setTimeout(() => { t.style.display = 'none'; }, 2800);
}

/* ── Admin nav group toggle ──────────────────────────────── */
function toggleAdminGroup(id) {
  const group = document.getElementById(id);
  if (!group) return;
  const isOpen = group.classList.contains('admin-nav-group--open');
  // Accordion: toggle class only — CSS transition handles max-height/opacity
  document.querySelectorAll('.admin-nav-group').forEach(g => {
    if (g.id === id) return;
    g.classList.remove('admin-nav-group--open');
    const ic = g.querySelector('.admin-nav-group__icon');
    if (ic) ic.textContent = '+';
  });
  group.classList.toggle('admin-nav-group--open', !isOpen);
  const icon = group.querySelector('.admin-nav-group__icon');
  if (icon) icon.textContent = isOpen ? '+' : '−';
}

/* ── Feature flag toggle + modal ────────────────────────── */
let _flagPending = null;
function flagToggle(rowId, name) {
  const row = document.getElementById(rowId);
  const btn = row?.querySelector('.flag-toggle');
  const isOn = btn?.classList.contains('flag-toggle--on');
  _flagPending = { rowId, name, turningOn: !isOn };
  const modal = document.getElementById('flag-modal');
  const title = document.getElementById('flag-modal-title');
  const body = document.getElementById('flag-modal-body');
  if (!modal || !title || !body) return;
  title.textContent = (!isOn ? 'Enable' : 'Disable') + ' "' + name + '"?';
  body.textContent = !isOn
    ? 'This will enable the feature for all users immediately. Are you sure?'
    : 'This will disable the feature for all users immediately. Any in-progress actions may be interrupted. Are you sure?';
  modal.style.display = 'flex';
}
function flagModalConfirm() {
  const modal = document.getElementById('flag-modal');
  if (modal) modal.style.display = 'none';
  if (!_flagPending) return;
  const { rowId, name, turningOn } = _flagPending;
  const row = document.getElementById(rowId);
  const btn = row?.querySelector('.flag-toggle');
  const label = row?.querySelector('.flag-toggle__label');
  if (btn) {
    btn.classList.toggle('flag-toggle--on', turningOn);
    btn.classList.toggle('flag-toggle--off', !turningOn);
  }
  if (label) {
    label.textContent = turningOn ? 'ON' : 'OFF';
    label.classList.toggle('flag-on-label', turningOn);
    label.classList.toggle('flag-off-label', !turningOn);
  }
  showToast((turningOn ? 'Enabled: ' : 'Disabled: ') + name);
  _flagPending = null;
}
function flagModalCancel() {
  const modal = document.getElementById('flag-modal');
  if (modal) modal.style.display = 'none';
  _flagPending = null;
}
// Close modal on backdrop click
document.addEventListener('click', e => {
  const modal = document.getElementById('flag-modal');
  if (modal && e.target === modal) flagModalCancel();
});


/* ── Generic live filter helper ─────────────────────────── */
function _liveFilter(tbodyId, showingId, total, chipSelector, matchFn) {
  let shown = 0;
  document.querySelectorAll('#' + tbodyId + ' tr').forEach(row => {
    const show = matchFn(row);
    row.style.display = show ? '' : 'none';
    if (show) shown++;
  });
  const el = document.getElementById(showingId);
  if (el) el.textContent = 'Showing ' + shown + ' of ' + total;
}

/* ── Multi-select chip helper ─────────────────────────────────── */
function _getActiveChips(selector) {
  const active = new Set();
  document.querySelectorAll(selector).forEach(function (chip) {
    if (!chip.classList.contains('dfe-c-chip--active')) return;
    var oc = chip.getAttribute('onclick') || '';
    var m = oc.match(/[,\(]['"]([^'"]+)['"]\)/);
    if (m && m[1] !== 'all') active.add(m[1].toLowerCase());
  });
  return active;
}
function _findAllChip(selector) {
  var chips = document.querySelectorAll(selector);
  for (var i = 0; i < chips.length; i++) {
    var oc = chips[i].getAttribute('onclick') || '';
    if (oc.indexOf("'all'") !== -1 || oc.indexOf('"all"') !== -1) return chips[i];
  }
  return null;
}
function _chipToggle(chip, selector, value) {
  if (!chip) return;
  if (value === 'all') {
    document.querySelectorAll(selector).forEach(c => c.classList.remove('dfe-c-chip--active'));
    chip.classList.add('dfe-c-chip--active');
  } else {
    var allChip = _findAllChip(selector);
    if (allChip) allChip.classList.remove('dfe-c-chip--active');
    chip.classList.toggle('dfe-c-chip--active');
    var anyActive = false;
    document.querySelectorAll(selector).forEach(function (c) { if (c.classList.contains('dfe-c-chip--active')) anyActive = true; });
    if (!anyActive && allChip) allChip.classList.add('dfe-c-chip--active');
  }
}

/* ── Business cases filter ───────────────────────────────────── */
function filterBC(chip, stage) {
  if (chip) _chipToggle(chip, '[onclick^="filterBC"]', stage || 'all');
  var stages = _getActiveChips('[onclick^="filterBC"]');
  var dept = document.getElementById('bc-dept-filter')?.value || '';
  var search = (document.getElementById('bc-search')?.value || '').toLowerCase();
  _liveFilter('bc-tbody', 'bc-showing', '11 business cases', null, function (row) {
    var stageOk = stages.size === 0 || stages.has((row.dataset.stage || '').toLowerCase());
    var deptOk = !dept || row.dataset.dept === dept;
    var srchOk = !search || row.textContent.toLowerCase().includes(search);
    return stageOk && deptOk && srchOk;
  });
}

/* ── Demand register filter ──────────────────────────────────── */
function filterDemand(chip, filter) {
  if (chip) _chipToggle(chip, '[onclick^="filterDemand"]', filter || 'all');
  var active = _getActiveChips('[onclick^="filterDemand"]');
  var statusF = new Set([...active].filter(v => !['must', 'could', 'not'].includes(v)));
  var bandF = new Set([...active].filter(v => ['must', 'could', 'not'].includes(v)));
  var dept = document.getElementById('dr-dept-filter')?.value || '';
  var search = (document.getElementById('dr-search')?.value || '').toLowerCase();
  _liveFilter('dr-tbody', 'dr-showing', '23 demands', null, function (row) {
    var statusOk = statusF.size === 0 || statusF.has((row.dataset.status || '').toLowerCase());
    var bandOk = bandF.size === 0 || bandF.has((row.dataset.band || '').toLowerCase());
    var deptOk = !dept || row.dataset.dept === dept;
    var srchOk = !search || row.textContent.toLowerCase().includes(search);
    return statusOk && bandOk && deptOk && srchOk;
  });
}

/* ── Work register filter ────────────────────────────────────── */
function filterWork(chip, filter) {
  if (chip) _chipToggle(chip, '[onclick^="filterWork"]', filter || 'all');
  var active = _getActiveChips('[onclick^="filterWork"]');
  var ragFilters = new Set([...active].filter(v => ['red', 'amber', 'green'].includes(v)));
  var phaseFilters = new Set([...active].filter(v => ['discovery', 'alpha', 'beta', 'live'].includes(v)));
  var portfolio = document.getElementById('work-portfolio-filter')?.value || '';
  var dir = document.getElementById('work-dir-filter')?.value || '';
  var priority = document.getElementById('work-priority-filter')?.value || '';
  var update = document.getElementById('work-update-filter')?.value || '';
  var search = (document.getElementById('work-search')?.value || '').toLowerCase();
  var shown = 0;
  document.querySelectorAll('#work-tbody tr').forEach(function (row) {
    var ragOk = ragFilters.size === 0 || ragFilters.has((row.dataset.rag || '').toLowerCase());
    var phaseOk = phaseFilters.size === 0 || phaseFilters.has((row.dataset.phase || '').toLowerCase());
    var portOk = !portfolio || row.dataset.portfolio === portfolio;
    var dirOk = !dir || row.dataset.dir === dir;
    var priOk = !priority || (row.dataset.priority || '').toLowerCase() === priority.toLowerCase();
    var updOk = !update || (row.dataset.update || '').toLowerCase() === update.toLowerCase().replace(' ', '-');
    var srchOk = !search || row.textContent.toLowerCase().includes(search);
    var show = ragOk && phaseOk && portOk && dirOk && priOk && updOk && srchOk;
    row.style.display = show ? '' : 'none';
    if (show) shown++;
  });
  var cnt = document.getElementById('work-showing-count');
  if (cnt) cnt.textContent = 'Showing ' + shown + ' of 256 work items';
}

function clearWorkFilters() {
  ['work-portfolio-filter', 'work-dir-filter', 'work-priority-filter', 'work-update-filter'].forEach(function (id) {
    var el = document.getElementById(id); if (el) el.value = '';
  });
  var s = document.getElementById('work-search'); if (s) s.value = '';
  document.querySelectorAll('[onclick^="filterWork"]').forEach(c => c.classList.remove('dfe-c-chip--active'));
  var allChip = _findAllChip('[onclick^="filterWork"]');
  if (allChip) allChip.classList.add('dfe-c-chip--active');
  filterWork();
}

/* ── Risks by portfolio filter ───────────────────────────────── */
function filterRP() {
  var portfolio = document.getElementById('rp-portfolio-filter')?.value || '';
  var tier = document.getElementById('rp-tier-filter')?.value || '';
  var rating = document.getElementById('rp-rating-filter')?.value || '';
  var status = document.getElementById('rp-status-filter')?.value || '';
  var search = (document.getElementById('rp-search')?.value || '').toLowerCase();
  var riskShown = 0, issueShown = 0;
  document.querySelectorAll('#rp-risks-tbody tr').forEach(function (row) {
    var portOk = !portfolio || row.dataset.portfolio === portfolio;
    var tierOk = !tier || row.dataset.tier === tier.replace(' Enterprise', '').replace(' DDT', '').replace(' Project', '');
    var ratingOk = !rating || row.dataset.rating === rating;
    var statusOk = !status || row.dataset.status === status;
    var srchOk = !search || row.textContent.toLowerCase().includes(search);
    var show = portOk && tierOk && ratingOk && statusOk && srchOk;
    row.style.display = show ? '' : 'none';
    if (show) riskShown++;
  });
  document.querySelectorAll('#rp-issues-tbody tr').forEach(function (row) {
    var portOk = !portfolio || row.dataset.portfolio === portfolio;
    var ratingOk = !rating || row.dataset.rating === rating;
    var statusOk = !status || row.dataset.status === status;
    var srchOk = !search || row.textContent.toLowerCase().includes(search);
    var show = portOk && ratingOk && statusOk && srchOk;
    row.style.display = show ? '' : 'none';
    if (show) issueShown++;
  });
  var rc = document.getElementById('rp-risks-count'); if (rc) rc.textContent = riskShown;
  var ic = document.getElementById('rp-issues-count'); if (ic) ic.textContent = issueShown;
  var rs = document.getElementById('rp-risks-showing'); if (rs) rs.textContent = 'Showing ' + riskShown + ' of 42 open risks';
  var is_ = document.getElementById('rp-issues-showing'); if (is_) is_.textContent = 'Showing ' + issueShown + ' of 14 open issues';
  var cnt = document.getElementById('rp-count');
  if (cnt) {
    var active = [portfolio, tier, rating, status].filter(Boolean).join(' · ');
    cnt.textContent = active ? 'Filtered: ' + active : 'Showing all open risks and issues across 18 portfolios';
  }
}
function clearRPFilters() {
  ['rp-portfolio-filter', 'rp-tier-filter', 'rp-rating-filter', 'rp-status-filter'].forEach(function (id) {
    var el = document.getElementById(id); if (el) el.value = '';
  });
  var s = document.getElementById('rp-search'); if (s) s.value = '';
  filterRP();
}


/* ── Portfolio / Directorate report loaders ─────────────────── */
function loadPortfolioReport(name) {
  const content = document.getElementById('portfolio-report-content');
  const placeholder = document.getElementById('portfolio-report-placeholder');
  const nameEl = document.getElementById('portfolio-report-name');
  if (!name) {
    if (content) content.style.display = 'none';
    if (placeholder) placeholder.style.display = '';
    return;
  }
  if (nameEl) nameEl.textContent = name + ' portfolio';
  if (content) content.style.display = '';
  if (placeholder) placeholder.style.display = 'none';
}
function loadDirectorateReport(name) {
  const content = document.getElementById('directorate-report-content');
  const placeholder = document.getElementById('directorate-report-placeholder');
  const nameEl = document.getElementById('directorate-report-name');
  if (!name) {
    if (content) content.style.display = 'none';
    if (placeholder) placeholder.style.display = '';
    return;
  }
  if (nameEl) nameEl.textContent = name + ' directorate';
  if (content) content.style.display = '';
  if (placeholder) placeholder.style.display = 'none';
}

/* ── Saved filters ────────────────────────────────────────────── */
var _savedFilters = { work: [], rp: [] };
var _saveFilterCtx = null;

function openSaveFilter(ctx) {
  _saveFilterCtx = ctx;
  var m = document.getElementById('save-filter-modal');
  if (!m) return;
  document.getElementById('sfm-name').value = '';
  var desc = '';
  if (ctx === 'rp') {
    var parts = ['rp-portfolio-filter', 'rp-tier-filter', 'rp-rating-filter', 'rp-status-filter']
      .map(function (id) { return document.getElementById(id)?.value || ''; })
      .filter(Boolean);
    desc = parts.join(' · ') || 'No filters applied';
  } else if (ctx === 'work') {
    var chips = [...document.querySelectorAll('[onclick^="filterWork"]')]
      .filter(function (c) { return c.classList.contains('dfe-c-chip--active'); })
      .map(function (c) { return c.textContent.trim(); })
      .filter(function (v) { return v !== 'All'; });
    var p = document.getElementById('work-portfolio-filter')?.value || '';
    var d = document.getElementById('work-dir-filter')?.value || '';
    desc = [...chips, p, d].filter(Boolean).join(' · ') || 'No filters applied';
  }
  document.getElementById('sfm-desc').textContent = desc;
  m.style.display = 'flex';
}
function closeSaveFilter() {
  var m = document.getElementById('save-filter-modal');
  if (m) m.style.display = 'none';
}
function confirmSaveFilter() {
  var name = document.getElementById('sfm-name')?.value.trim();
  if (!name) { document.getElementById('sfm-name').focus(); return; }
  var ctx = _saveFilterCtx;
  if (!_savedFilters[ctx]) _savedFilters[ctx] = [];
  _savedFilters[ctx].push({ name: name, id: Date.now() });
  closeSaveFilter();
  _renderSavedFilters(ctx);
  showToast('Filter saved: ' + name);
}
function _renderSavedFilters(ctx) {
  var el = document.getElementById(ctx + '-saved-filters');
  if (!el) return;
  var filters = _savedFilters[ctx] || [];
  if (!filters.length) { el.style.display = 'none'; return; }
  el.style.display = 'flex';
  el.style.gap = '0.375rem';
  el.style.alignItems = 'center';
  el.style.flexWrap = 'wrap';
  var html = '<span style="font-size:0.8125rem;font-weight:700;color:var(--grey-4);">Saved:</span>';
  filters.forEach(function (f) {
    html += '<button onclick="applySavedFilter(\'' + ctx + '\',' + f.id + ')" style="font-size:0.8125rem;padding:0.25rem 0.625rem;background:var(--t-blue-bg);border:1px solid var(--blue);color:var(--blue);cursor:pointer;">&#9733; ' + f.name + '</button>';
  });
  el.innerHTML = html;
}
function applySavedFilter(ctx, id) {
  showToast('Filter applied');
}


/* ── Global search ────────────────────────────────────────────── */
function gsSearch(q) {
  var empty = document.getElementById('gs-empty');
  var results = document.getElementById('gs-results');
  var summary = document.getElementById('gs-summary');
  if (!q || q.length < 2) {
    if (empty) empty.style.display = '';
    if (results) results.style.display = 'none';
    return;
  }
  if (empty) empty.style.display = 'none';
  if (results) results.style.display = '';
  if (summary) summary.textContent = 'Showing results for “' + q + '”';
}

/* ── Near miss action form helpers ────────────────────────────── */
function nmToggleForm(id) {
  var el = document.getElementById(id);
  if (el) el.style.display = el.style.display === 'none' ? '' : 'none';
}

function nmAddImmediate() {
  var desc = (document.getElementById('imm-desc') || {}).value || '';
  var owner = (document.getElementById('imm-owner') || {}).value || '';
  var date = (document.getElementById('imm-date') || {}).value || '';
  var status = (document.getElementById('imm-status') || {}).value || 'complete';
  if (!desc.trim() || !owner.trim()) { showToast('Please fill in action and owner'); return; }

  var tbody = document.getElementById('imm-tbody');
  if (tbody) {
    var tr = document.createElement('tr');
    var td1 = document.createElement('td'); td1.textContent = desc;
    var td2 = document.createElement('td'); td2.textContent = owner;
    var td3 = document.createElement('td'); td3.textContent = date || '—';
    var td4 = document.createElement('td');
    var col = status === 'complete' ? 'green' : status === 'in-progress' ? 'blue' : 'grey';
    var tag = document.createElement('span');
    tag.className = 'dfe-c-tag dfe-c-tag--' + col;
    tag.textContent = status === 'complete' ? 'COMPLETE' : status === 'in-progress' ? 'IN PROGRESS' : 'NOT STARTED';
    td4.appendChild(tag);
    tr.appendChild(td1); tr.appendChild(td2); tr.appendChild(td3); tr.appendChild(td4);
    tbody.appendChild(tr);
  }

  ['imm-desc', 'imm-owner', 'imm-date'].forEach(function (id) {
    var el = document.getElementById(id); if (el) el.value = '';
  });
  nmToggleForm('nmv-imm-form');
  showToast('Immediate action added');
}

function nmAddPreventive() {
  var desc = (document.getElementById('prev-desc') || {}).value || '';
  var owner = (document.getElementById('prev-owner') || {}).value || '';
  var due = (document.getElementById('prev-due') || {}).value || '';
  var link = (document.getElementById('prev-link') || {}).value || '';
  var verify = (document.getElementById('prev-verify') || {}).value || '';
  if (!desc.trim() || !owner.trim() || !due) { showToast('Please fill in description, owner and due date'); return; }

  var tbody = document.getElementById('prev-tbody');
  if (tbody) {
    var idx = tbody.querySelectorAll('tr').length + 1;
    var tr = document.createElement('tr');
    tr.id = 'prev-row-' + idx;

    var td1 = document.createElement('td');
    var dv1 = document.createElement('div'); dv1.textContent = desc; dv1.style.fontWeight = '600';
    td1.appendChild(dv1);
    if (verify) {
      var dv2 = document.createElement('div');
      dv2.textContent = 'Verify: ' + verify;
      dv2.style.cssText = 'font-size:0.8125rem;color:var(--grey-4);';
      td1.appendChild(dv2);
    }

    // Inline edit panel
    var editDiv = document.createElement('div');
    editDiv.id = 'prev-edit-' + idx;
    editDiv.style.display = 'none';
    editDiv.style.cssText = 'display:none;background:var(--grey-1);border:1px solid var(--grey-2);padding:0.75rem;margin-top:0.5rem;';
    editDiv.innerHTML = '<div class="dfe-c-form__group"><label class="dfe-c-form__label" style="font-size:0.8125rem;">Description</label>' +
      '<input type="text" value="' + desc.replace(/"/g, '&quot;') + '" style="width:100%;"></div>' +
      '<div class="dfe-c-form__group dfe-c-mt-1" style="margin-bottom:0.5rem;"><label class="dfe-c-form__label" style="font-size:0.8125rem;">Update note</label><input type="text" placeholder="What changed?"></div>';
    var saveBtnEdit = document.createElement('button');
    saveBtnEdit.className = 'dfe-c-button dfe-c-button--primary dfe-c-button--sm';
    saveBtnEdit.textContent = 'Save';
    saveBtnEdit.addEventListener('click', function () { editDiv.style.display = 'none'; showToast('Action updated'); });
    var cancelBtnEdit = document.createElement('button');
    cancelBtnEdit.className = 'dfe-c-button dfe-c-button--secondary dfe-c-button--sm';
    cancelBtnEdit.textContent = 'Cancel';
    cancelBtnEdit.addEventListener('click', function () { editDiv.style.display = 'none'; });
    var btnGrp = document.createElement('div'); btnGrp.className = 'dfe-c-flex'; btnGrp.style.gap = '0.375rem;';
    btnGrp.appendChild(saveBtnEdit); btnGrp.appendChild(cancelBtnEdit);
    editDiv.appendChild(btnGrp);
    td1.appendChild(editDiv);

    var td2 = document.createElement('td'); td2.textContent = owner;
    var td3 = document.createElement('td'); td3.textContent = due;
    var td4 = document.createElement('td'); td4.textContent = link || '—';

    var td5 = document.createElement('td');
    var sel = document.createElement('select');
    sel.style.cssText = 'font-size:0.75rem;border:1px solid var(--grey-3);padding:2px 4px;';
    ['Not started', 'In progress', 'Complete', 'Overdue'].forEach(function (v, i) {
      var opt = document.createElement('option');
      opt.value = v; opt.textContent = v; opt.selected = i === 0;
      sel.appendChild(opt);
    });
    sel.addEventListener('change', function () { showToast('Status updated'); });
    td5.appendChild(sel);

    var td6 = document.createElement('td');
    var editBtn = document.createElement('button');
    editBtn.className = 'dfe-c-button dfe-c-button--secondary dfe-c-button--sm';
    editBtn.textContent = 'Edit';
    editBtn.addEventListener('click', function () {
      editDiv.style.display = editDiv.style.display === 'none' ? '' : 'none';
    });
    td6.appendChild(editBtn);

    [td1, td2, td3, td4, td5, td6].forEach(function (td) { tr.appendChild(td); });
    tbody.appendChild(tr);

    // Update count
    var countEl = document.getElementById('prev-count');
    if (countEl) {
      var total = tbody.querySelectorAll('tr').length;
      countEl.textContent = total + ' preventive action' + (total === 1 ? '' : 's');
    }
  }

  ['prev-desc', 'prev-owner', 'prev-due', 'prev-link', 'prev-verify'].forEach(function (id) {
    var el = document.getElementById(id); if (el) el.value = '';
  });
  nmToggleForm('nmv-prev-form');
  showToast('Preventive action added');
}

/* ── Near miss filter ─────────────────────────────────────────── */
var _nmCat = 'all';
function filterNM(chip, cat) {
  if (chip) _chipToggle(chip, '[onclick^="filterNM"]', cat || 'all');
  var cats = _getActiveChips('[onclick^="filterNM"]');
  document.querySelectorAll('#nm-tbody tr').forEach(function (row) {
    var show = cats.size === 0 || cats.has((row.dataset.cat || '').toLowerCase());
    row.style.display = show ? '' : 'none';
  });
}

/* ── Sortable tables ─────────────────────────────────────── */
function sortTable(th) {
  const table = th.closest('table');
  const tbody = table.querySelector('tbody');
  const rows = Array.from(tbody.querySelectorAll('tr'));
  const idx = th.dataset.sortCol != null ? parseInt(th.dataset.sortCol, 10) : Array.from(th.parentElement.children).indexOf(th);
  const asc = !th.classList.contains('sort-asc');
  th.closest('thead').querySelectorAll('th').forEach(t => t.classList.remove('sort-asc', 'sort-desc'));
  th.classList.add(asc ? 'sort-asc' : 'sort-desc');
  rows.sort((a, b) => {
    const av = (a.cells[idx]?.textContent || '').trim().replace(/[,£%↑↓→▲▼]/g, '').replace(/\s+/g, ' ');
    const bv = (b.cells[idx]?.textContent || '').trim().replace(/[,£%↑↓→▲▼]/g, '').replace(/\s+/g, ' ');
    const an = parseFloat(av), bn = parseFloat(bv);
    const cmp = (!isNaN(an) && !isNaN(bn)) ? an - bn : av.localeCompare(bv, 'en', { sensitivity: 'base' });
    return asc ? cmp : -cmp;
  });
  rows.forEach(r => tbody.appendChild(r));
}
document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('thead th').forEach(th => {
    if (th.dataset.sortBound === '1') return;
    th.dataset.sortBound = '1';
    th.classList.add('sortable');
    th.addEventListener('click', () => sortTable(th));
  });
});

/* ── Admin panel switcher ────────────────────────────────── */
function adminPanel(link, panelId) {
  document.querySelectorAll('.admin-nav-link').forEach(l => l.classList.remove('admin-nav-link--active'));
  document.querySelectorAll('.admin-panel').forEach(p => p.style.display = 'none');
  link.classList.add('admin-nav-link--active');
  const p = document.getElementById(panelId);
  if (p) p.style.display = '';
}

/* ── DDT standards filter ────────────────────────────────── */
let _ddtsCat = 'all', _ddtsOwner = 'all', _ddtsSearch = '';
function filterDDTS(chip, cat) {
  const sc = document.getElementById('sc-standards-ddt');
  if (sc) sc.querySelectorAll('[onclick^="filterDDTS"]').forEach(c => c.classList.remove('dfe-c-chip--active'));
  chip.classList.add('dfe-c-chip--active');
  _ddtsCat = cat; _applyDDTSFilter();
}
function filterDDTSOwner(v) { _ddtsOwner = v; _applyDDTSFilter(); }
function filterDDTSSearch() { _ddtsSearch = (document.getElementById('ddts-search')?.value || '').toLowerCase(); _applyDDTSFilter(); }
function _applyDDTSFilter() {
  document.querySelectorAll('#ddts-table tbody tr').forEach(r => {
    const catOk = _ddtsCat === 'all' || r.dataset.cat === _ddtsCat;
    const ownerOk = _ddtsOwner === 'all' || r.dataset.owner === _ddtsOwner;
    const searchOk = !_ddtsSearch || r.textContent.toLowerCase().includes(_ddtsSearch);
    r.style.display = (catOk && ownerOk && searchOk) ? '' : 'none';
  });
}

/* ── Demand scoring calculator ───────────────────────────── */
const _ss = { s1: { s1a: 0, s1b: 0, s1c: 0 }, s2: { s2a: 0, s2b: 0 }, s3: { s3a: 0, s3b: 0, s3c: 6 }, s4: { s4a: 2, s4b: 6, s4c: 4, s4d: 6 } };
const _sm = { s1: 15, s2: 10, s3: 22, s4: 42 };
function scoreSection(el, section, question, value) {
  if (el.tagName === 'BUTTON') {
    const row = el.closest('.dfe-c-form__score-row');
    if (row) row.querySelectorAll('.dfe-c-form__score-btn').forEach(b => b.classList.remove('dfe-c-form__score-btn--selected'));
    el.classList.add('dfe-c-form__score-btn--selected');
  }
  _ss[section][question] = value;
  _updScore();
}
function _updScore() {
  let grand = 0;
  for (const [sec, qs] of Object.entries(_ss)) {
    const tot = Object.values(qs).reduce((a, b) => a + b, 0);
    grand += tot;
    const de = document.getElementById(sec + '-score-display');
    if (de) de.textContent = tot + ' / ' + _sm[sec];
    const su = document.getElementById(sec + '-summary');
    if (su) su.querySelector('strong').textContent = tot + ' / ' + _sm[sec];
  }
  const te = document.getElementById('running-total'); if (te) te.textContent = grand;
  const bar = document.getElementById('score-bar');
  if (bar) { bar.style.width = grand + '%'; bar.style.background = grand >= 57 ? 'var(--rag-g)' : grand >= 21 ? 'var(--rag-a)' : 'var(--rag-r)'; }
  const bd = document.getElementById('score-band-display');
  if (bd) {
    if (grand >= 57) { bd.textContent = 'MUST DO (' + grand + '/100)'; bd.style.background = 'var(--t-green-bg)'; bd.style.color = 'var(--rag-g)'; }
    else if (grand >= 21) { bd.textContent = 'COULD DO (' + grand + '/100)'; bd.style.background = 'var(--t-amber-bg)'; bd.style.color = 'var(--t-amber-fg)'; }
    else { bd.textContent = 'DO NOT DO (' + grand + '/100)'; bd.style.background = 'var(--t-red-bg)'; bd.style.color = 'var(--t-red-fg)'; }
  }
}
function submitDemandScore() {
  let g = 0; for (const qs of Object.values(_ss)) g += Object.values(qs).reduce((a, b) => a + b, 0);
  showToast('Score submitted: ' + g + '/100 — ' + (g >= 57 ? 'Must do' : g >= 21 ? 'Could do' : 'Do not do') + ' · Sent to triage');
  setTimeout(() => ss('demand-triage'), 1800);
}
document.addEventListener('DOMContentLoaded', _updScore);


/* ── Saved filters (Work, Risks by portfolio) ──────────────── */
/* ── Risks by portfolio filter ───────────────────────────────── */
function filterRP() {
  const portfolio = document.getElementById('rp-portfolio-filter')?.value || '';
  const tier = document.getElementById('rp-tier-filter')?.value || '';
  const rating = document.getElementById('rp-rating-filter')?.value || '';
  const status = document.getElementById('rp-status-filter')?.value || '';
  const search = (document.getElementById('rp-search')?.value || '').toLowerCase();

  let riskShown = 0, issueShown = 0;
  document.querySelectorAll('#rp-risks-tbody tr').forEach(row => {
    const portOk = !portfolio || row.dataset.portfolio === portfolio;
    const tierOk = !tier || row.dataset.tier === tier.replace(' Enterprise', '').replace(' DDT', '').replace(' Project', '');
    const ratingOk = !rating || row.dataset.rating === rating;
    const statusOk = !status || row.dataset.status === status;
    const srchOk = !search || row.textContent.toLowerCase().includes(search);
    const show = portOk && tierOk && ratingOk && statusOk && srchOk;
    row.style.display = show ? '' : 'none';
    if (show) riskShown++;
  });
  document.querySelectorAll('#rp-issues-tbody tr').forEach(row => {
    const portOk = !portfolio || row.dataset.portfolio === portfolio;
    const ratingOk = !rating || row.dataset.rating === rating;
    const statusOk = !status || row.dataset.status === status;
    const srchOk = !search || row.textContent.toLowerCase().includes(search);
    const show = portOk && ratingOk && statusOk && srchOk;
    row.style.display = show ? '' : 'none';
    if (show) issueShown++;
  });

  const rc = document.getElementById('rp-risks-count');
  const ic = document.getElementById('rp-issues-count');
  const rs = document.getElementById('rp-risks-showing');
  const is = document.getElementById('rp-issues-showing');
  if (rc) rc.textContent = riskShown;
  if (ic) ic.textContent = issueShown;
  if (rs) rs.textContent = 'Showing ' + riskShown + ' of 42 open risks';
  if (is) is.textContent = 'Showing ' + issueShown + ' of 14 open issues';

  const cnt = document.getElementById('rp-count');
  if (cnt) {
    const active = [portfolio, tier, rating, status].filter(Boolean).join(', ');
    cnt.textContent = active ? 'Filtered: ' + active : 'Showing all open risks and issues across 18 portfolios';
  }
}
function clearRPFilters() {
  ['rp-portfolio-filter', 'rp-tier-filter', 'rp-rating-filter', 'rp-status-filter'].forEach(id => {
    const el = document.getElementById(id); if (el) el.value = '';
  });
  const s = document.getElementById('rp-search'); if (s) s.value = '';
  filterRP();
}

/* ── Portfolio / Directorate report loaders ─────────────────── */
function loadPortfolioReport(name) {
  const content = document.getElementById('portfolio-report-content');
  const placeholder = document.getElementById('portfolio-report-placeholder');
  const nameEl = document.getElementById('portfolio-report-name');
  if (!name) {
    if (content) content.style.display = 'none';
    if (placeholder) placeholder.style.display = '';
    return;
  }
  if (nameEl) nameEl.textContent = name + ' portfolio';
  if (content) content.style.display = '';
  if (placeholder) placeholder.style.display = 'none';
}
function loadDirectorateReport(name) {
  const content = document.getElementById('directorate-report-content');
  const placeholder = document.getElementById('directorate-report-placeholder');
  const nameEl = document.getElementById('directorate-report-name');
  if (!name) {
    if (content) content.style.display = 'none';
    if (placeholder) placeholder.style.display = '';
    return;
  }
  if (nameEl) nameEl.textContent = name + ' directorate';
  if (content) content.style.display = '';
  if (placeholder) placeholder.style.display = 'none';
}

/* ── Sortable tables ─────────────────────────────────────── */
function sortTable(th) {
  const table = th.closest('table');
  const tbody = table.querySelector('tbody');
  const rows = Array.from(tbody.querySelectorAll('tr'));
  const idx = th.dataset.sortCol != null ? parseInt(th.dataset.sortCol, 10) : Array.from(th.parentElement.children).indexOf(th);
  const asc = !th.classList.contains('sort-asc');
  th.closest('thead').querySelectorAll('th').forEach(t => t.classList.remove('sort-asc', 'sort-desc'));
  th.classList.add(asc ? 'sort-asc' : 'sort-desc');
  rows.sort((a, b) => {
    const av = (a.cells[idx]?.textContent || '').trim().replace(/[,£%↑↓→▲▼]/g, '').replace(/\s+/g, ' ');
    const bv = (b.cells[idx]?.textContent || '').trim().replace(/[,£%↑↓→▲▼]/g, '').replace(/\s+/g, ' ');
    const an = parseFloat(av), bn = parseFloat(bv);
    const cmp = (!isNaN(an) && !isNaN(bn)) ? an - bn : av.localeCompare(bv, 'en', { sensitivity: 'base' });
    return asc ? cmp : -cmp;
  });
  rows.forEach(r => tbody.appendChild(r));
}
document.addEventListener('DOMContentLoaded', () => {
  document.querySelectorAll('thead th').forEach(th => {
    if (th.dataset.sortBound === '1') return;
    th.dataset.sortBound = '1';
    th.classList.add('sortable');
    th.addEventListener('click', () => sortTable(th));
  });
});

/* ── Chip toggles ────────────────────────────────────────────── */
document.addEventListener('click', e => {
  if (e.target.classList.contains('dfe-c-chip') && !e.target.dataset.skipToggle)
    e.target.classList.toggle('dfe-c-chip--active');
});

/* ── Products toggle ─────────────────────────────────────────── */
function toggleProdView() {
  const tv = document.getElementById('prod-table-view');
  const cv = document.getElementById('prod-card-view');
  const btn = document.getElementById('prod-view-btn');
  const isTable = tv.style.display !== 'none';
  tv.style.display = isTable ? 'none' : '';
  cv.style.display = isTable ? '' : 'none';
  if (btn) btn.textContent = isTable ? '☰ Table view' : '🗂 Card view';
}

function filterProds() {
  const q = document.getElementById('prod-search').value.toLowerCase();
  document.querySelectorAll('#prod-tbody tr').forEach(row => {
    row.style.display = row.textContent.toLowerCase().includes(q) ? '' : 'none';
  });
}

function jumpProd(letter) {
  const tv = document.getElementById('prod-table-view');
  if (tv && tv.style.display === 'none') toggleProdView();
  const row = document.querySelector(`#prod-tbody tr[data-letter="${letter}"]`);
  if (row) row.scrollIntoView({ behavior: 'smooth', block: 'start' });
}

/* ── Demand score calculator ─────────────────────────── */
var _sectionScores = {};
function scoreSection(el, section, question, value) {
  if (!_sectionScores[section]) _sectionScores[section] = {};
  _sectionScores[section][question] = parseInt(value) || 0;
  var row = el.closest('.dfe-c-form__score-row');
  if (row) row.querySelectorAll('.dfe-c-form__score-btn').forEach(function (b) { b.classList.remove('dfe-c-form__score-btn--selected'); });
  el.classList.add('dfe-c-form__score-btn--selected');
  _updScore();
}
function _updScore() {
  var total = 0;
  Object.values(_sectionScores).forEach(function (sec) { Object.values(sec).forEach(function (v) { total += v; }); });
  var bar = document.getElementById('score-bar-fill');
  var num = document.getElementById('score-running-total');
  var band = document.getElementById('score-band-display');
  if (bar) bar.style.width = Math.min(100, total) + '%';
  if (num) num.textContent = total;
  if (band) {
    if (total >= 70) { band.textContent = 'MUST DO'; band.style.background = 'var(--rag-g)'; }
    else if (total >= 45) { band.textContent = 'COULD DO'; band.style.background = 'var(--rag-a)'; }
    else if (total >= 20) { band.textContent = 'LOW PRIORITY'; band.style.background = 'var(--navy)'; }
    else { band.textContent = 'DO NOT DO'; band.style.background = 'var(--rag-r)'; }
  }
}
function submitDemandScore() {
  var total = 0;
  Object.values(_sectionScores).forEach(function (sec) { Object.values(sec).forEach(function (v) { total += v; }); });
  var bandName = total >= 70 ? 'MUST DO' : total >= 45 ? 'COULD DO' : total >= 20 ? 'LOW PRIORITY' : 'DO NOT DO';
  showToast('Score submitted: ' + total + ' — ' + bandName);
  setTimeout(function () { ss('demand-triage'); }, 1800);
}

/* ── Init — prototype HTML only (has #sc-home). MVC pages must not rely on .dfe-c-screen + ss(). ── */
if (document.getElementById('sc-home')) { ss('home'); }

/* ── CSP-safe UI: <dialog> and collapsibles without inline event handlers ── */
document.addEventListener('click', function (e) {
  var openDlg = e.target.closest('[data-modal-open]');
  if (openDlg) {
    var oid = openDlg.getAttribute('data-modal-open');
    var od = oid && document.getElementById(oid);
    if (od && typeof od.showModal === 'function') {
      e.preventDefault();
      od.showModal();
    }
    return;
  }
  var closeDlg = e.target.closest('[data-modal-close]');
  if (closeDlg) {
    var cid = closeDlg.getAttribute('data-modal-close');
    var cd = cid && document.getElementById(cid);
    if (cd && typeof cd.close === 'function') {
      e.preventDefault();
      cd.close();
    }
    return;
  }
  var collTrigger = e.target.closest('.dfe-c-collapsible__trigger');
  if (collTrigger) {
    var nativeSummary = collTrigger.closest('summary');
    var nativeDetails = collTrigger.closest('details');
    if (nativeSummary && nativeDetails) {
      // Let native <details>/<summary> handle open/close behavior.
      return;
    }
    e.preventDefault();
    toggleCollapsible(collTrigger);
  }
});
