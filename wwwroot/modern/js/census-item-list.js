(function () {
  const root = document.querySelector('[data-module="census-item-list"]');
  const form = document.getElementById('census-question');
  const modal = document.getElementById('census-item-modal');
  if (!root || !form) return;

  const empty = document.getElementById('census-item-empty');
  const tableWrap = document.getElementById('census-item-table-wrap');
  const body = document.getElementById('census-item-body');
  const fields = document.getElementById('census-item-fields');
  const titleEl = document.getElementById('census-item-modal-title');
  const textEl = document.getElementById('census-item-modal-text');
  const urlEl = document.getElementById('census-item-modal-url');
  const saveEl = document.getElementById('census-item-modal-save');
  const removeModal = document.getElementById('census-remove-modal');
  const removeConfirm = document.getElementById('census-remove-modal-confirm');
  const addLabel = root.dataset.addLabel || 'Add an item';
  const captureUrl = root.dataset.captureUrl === 'true';
  const pickMode = root.dataset.pickRegister === 'true';
  const suggestUrl = root.dataset.suggestUrl || '';
  const listedRadio = document.getElementById('census-item-listed');
  const notListedRadio = document.getElementById('census-item-not-listed');
  const searchInput = document.getElementById('census-item-modal-search');
  const searchList = document.getElementById('census-item-search-results');
  const detailsEl = document.getElementById('census-item-modal-details');
  let editingIndex = null;
  let pendingRemoveIndex = null;
  let removedLast = false;
  let list = readList();
  let selectedPick = { code: '', title: '' };
  let suggestItems = [];
  let suggestActive = -1;
  let suggestTimer = null;
  let suggestAbort = null;

  if (window.DfeFrontend && typeof window.DfeFrontend.initModal === 'function') {
    if (modal) window.DfeFrontend.initModal(modal);
    if (removeModal) window.DfeFrontend.initModal(removeModal);
  }

  function readList() {
    return [...(body?.querySelectorAll('tr') || [])].map(row => ({
      title: (row.querySelector('.js-census-item-title')?.value
        || row.querySelector('.census-item-list__text')?.textContent
        || '').trim(),
      url: (row.querySelector('.js-census-item-url')?.value
        || row.getAttribute('data-url')
        || '').trim(),
      code: (row.querySelector('.js-census-item-code')?.value
        || row.getAttribute('data-code')
        || '').trim()
    })).filter(item => item.title || item.code);
  }

  function escapeAttr(value) {
    return value
      .replace(/&/g, '&amp;')
      .replace(/"/g, '&quot;')
      .replace(/</g, '&lt;');
  }

  function urlCellHtml(url) {
    if (!url) return 'Not set';
    const safe = escapeAttr(url);
    return '<a class="govuk-link" href="' + safe + '" rel="noreferrer noopener">' + safe + '</a>';
  }

  function rowTemplate(item, index) {
    const tr = document.createElement('tr');
    tr.className = 'govuk-table__row';
    tr.setAttribute('data-url', item.url || '');
    tr.setAttribute('data-code', item.code || '');
    const titleInput = document.createElement('input');
    titleInput.type = 'hidden';
    titleInput.className = 'js-census-item-title';
    titleInput.name = 'Items[' + index + '].Title';
    titleInput.value = item.title;
    const codeInput = document.createElement('input');
    codeInput.type = 'hidden';
    codeInput.className = 'js-census-item-code';
    codeInput.name = 'Items[' + index + '].LookupCode';
    codeInput.value = item.code || '';
    const titleText = document.createElement('span');
    titleText.className = 'census-item-list__text';
    titleText.textContent = item.title;
    const titleCell = document.createElement('td');
    titleCell.className = 'govuk-table__cell';
    titleCell.append(titleInput, codeInput, titleText);
    tr.appendChild(titleCell);
    if (captureUrl) {
      const urlCell = document.createElement('td');
      urlCell.className = 'govuk-table__cell census-item-list__url';
      const urlInput = document.createElement('input');
      urlInput.type = 'hidden';
      urlInput.className = 'js-census-item-url';
      urlInput.name = 'Items[' + index + '].ExternalUrl';
      urlInput.value = item.url || '';
      urlCell.appendChild(urlInput);
      const holder = document.createElement('span');
      holder.innerHTML = urlCellHtml(item.url);
      urlCell.appendChild(holder);
      tr.appendChild(urlCell);
    }
    const actions = document.createElement('td');
    actions.className = 'govuk-table__cell';
    actions.innerHTML = '<div class="govuk-button-group census-item-actions">'
      + '<button type="button" class="govuk-button govuk-button--secondary js-census-item-edit">Change</button>'
      + '<button type="button" class="govuk-button govuk-button--secondary js-census-item-remove">Remove</button>'
      + '</div>';
    tr.appendChild(actions);
    return tr;
  }

  function showList() {
    if (empty) empty.hidden = list.length > 0;
    if (tableWrap) tableWrap.hidden = list.length === 0;
  }

  function render() {
    if (!body) return;
    body.replaceChildren();
    list.forEach((item, index) => body.appendChild(rowTemplate(item, index)));
    if (fields) {
      fields.replaceChildren();
      if (list.length === 0 && removedLast) {
        const cleared = document.createElement('input');
        cleared.type = 'hidden';
        cleared.name = 'ItemListCleared';
        cleared.value = 'true';
        fields.appendChild(cleared);
      }
    }
    const json = document.getElementById('census-item-json');
    if (json) json.value = JSON.stringify(list.map(item => ({ title: item.title, url: item.url, code: item.code })));
    showList();
  }

  function openDialog(dialog) {
    if (!dialog) return;
    if (typeof dialog.showModal === 'function') dialog.showModal();
    else dialog.setAttribute('open', '');
  }

  function closeDialog(dialog) {
    if (!dialog) return;
    if (typeof dialog.close === 'function') dialog.close();
    else dialog.removeAttribute('open');
  }

  function hideSuggest() {
    if (!searchList) return;
    searchList.hidden = true;
    searchList.innerHTML = '';
    suggestItems = [];
    suggestActive = -1;
    searchInput?.setAttribute('aria-expanded', 'false');
  }

  function setSuggestActive(index) {
    const options = searchList?.querySelectorAll('[role="option"]') || [];
    options.forEach(el => {
      el.classList.remove('is-active');
      el.setAttribute('aria-selected', 'false');
    });
    suggestActive = index;
    if (suggestActive < 0 || !options[suggestActive]) {
      searchInput?.removeAttribute('aria-activedescendant');
      return;
    }
    options[suggestActive].classList.add('is-active');
    options[suggestActive].setAttribute('aria-selected', 'true');
    searchInput?.setAttribute('aria-activedescendant', options[suggestActive].id);
    options[suggestActive].scrollIntoView({ block: 'nearest' });
  }

  function choosePick(item) {
    selectedPick = { code: item.code || '', title: item.title || '' };
    if (searchInput) searchInput.value = selectedPick.title;
    hideSuggest();
  }

  function renderSuggest(results) {
    suggestItems = results || [];
    if (!searchList) return;
    if (!suggestItems.length) {
      searchList.hidden = false;
      searchList.innerHTML = '<li class="census-search__empty" role="status">No matching products or services. Use Not listed if it is missing.</li>';
      searchInput?.setAttribute('aria-expanded', 'true');
      suggestActive = -1;
      return;
    }
    searchList.hidden = false;
    searchInput?.setAttribute('aria-expanded', 'true');
    searchList.innerHTML = suggestItems.map((item, i) => {
      const hint = item.hint ? '<span class="census-search__option-hint">' + escapeAttr(item.hint) + '</span>' : '';
      return '<li role="option" id="census-dep-opt-' + i + '" class="census-search__option" data-code="'
        + escapeAttr(item.code) + '" data-title="' + escapeAttr(item.title) + '" aria-selected="false">'
        + '<span class="census-search__option-title">' + escapeAttr(item.title) + '</span>' + hint + '</li>';
    }).join('');
    setSuggestActive(0);
  }

  function searchDependencies(q) {
    if (!suggestUrl) return;
    suggestAbort?.abort();
    suggestAbort = new AbortController();
    const joiner = suggestUrl.indexOf('?') >= 0 ? '&' : '?';
    fetch(suggestUrl + joiner + 'q=' + encodeURIComponent(q), {
      credentials: 'same-origin',
      signal: suggestAbort.signal,
      headers: { Accept: 'application/json' }
    })
      .then(res => {
        if (!res.ok) throw new Error('suggest failed');
        return res.json();
      })
      .then(data => renderSuggest(data.results || []))
      .catch(err => {
        if (err.name !== 'AbortError') hideSuggest();
      });
  }

  function setListedMode(listed) {
    if (listedRadio) listedRadio.checked = listed;
    if (notListedRadio) notListedRadio.checked = !listed;
    listedRadio?.dispatchEvent(new Event('change', { bubbles: true }));
  }

  function openModal(index, value, url, code) {
    editingIndex = index;
    if (titleEl) titleEl.textContent = index == null ? addLabel : 'Change ' + (root.dataset.noun || 'item');
    if (textEl) textEl.value = value || '';
    if (urlEl) urlEl.value = url || '';
    if (pickMode) {
      if (index == null) {
        setListedMode(true);
        selectedPick = { code: '', title: '' };
        if (searchInput) searchInput.value = '';
        if (detailsEl) detailsEl.value = '';
      } else if (!code || code === 'not-listed') {
        setListedMode(false);
        selectedPick = { code: '', title: '' };
        if (searchInput) searchInput.value = '';
        if (detailsEl) detailsEl.value = value || '';
      } else {
        setListedMode(true);
        selectedPick = { code: code || '', title: value || '' };
        if (searchInput) searchInput.value = value || '';
        if (detailsEl) detailsEl.value = '';
      }
      hideSuggest();
    }
    openDialog(modal);
    setTimeout(() => {
      if (pickMode) (listedRadio?.checked ? searchInput : detailsEl)?.focus();
      else textEl?.focus();
    }, 50);
  }

  root.querySelectorAll('.js-census-item-add').forEach(button => {
    button.addEventListener('click', () => openModal(null, '', '', ''));
  });

  body.addEventListener('click', event => {
    const row = event.target.closest('tr');
    if (!row) return;
    const index = [...body.querySelectorAll('tr')].indexOf(row);
    const item = list[index] || { title: '', url: '', code: '' };
    if (event.target.closest('.js-census-item-edit')) {
      openModal(index, item.title, item.url, item.code);
    }
    if (event.target.closest('.js-census-item-remove')) {
      pendingRemoveIndex = index;
      openDialog(removeModal);
    }
  });

  saveEl?.addEventListener('click', () => {
    let next;
    if (pickMode) {
      if (notListedRadio?.checked) {
        const details = (detailsEl?.value || '').trim();
        if (!details) {
          detailsEl?.focus();
          return;
        }
        next = { title: details, url: '', code: 'not-listed' };
      } else {
        if (!selectedPick.code) {
          searchInput?.focus();
          return;
        }
        next = { title: selectedPick.title, url: '', code: selectedPick.code };
      }
    } else {
      const value = (textEl?.value || '').trim();
      const url = (urlEl?.value || '').trim();
      if (!value) {
        textEl?.focus();
        return;
      }
      next = { title: value, url: url, code: '' };
    }
    if (editingIndex == null) list.push(next);
    else list[editingIndex] = next;
    removedLast = false;
    render();
    window.censusMarkUnsaved?.();
    closeDialog(modal);
  });

  removeConfirm?.addEventListener('click', () => {
    if (pendingRemoveIndex != null) list.splice(pendingRemoveIndex, 1);
    pendingRemoveIndex = null;
    removedLast = list.length === 0;
    render();
    window.censusMarkUnsaved?.();
    closeDialog(removeModal);
  });

  modal?.querySelectorAll('[data-dfe-modal-close]').forEach(button => {
    button.addEventListener('click', () => closeDialog(modal));
  });
  removeModal?.querySelectorAll('[data-dfe-modal-close]').forEach(button => {
    button.addEventListener('click', () => {
      pendingRemoveIndex = null;
      closeDialog(removeModal);
    });
  });

  searchInput?.addEventListener('input', () => {
    selectedPick = { code: '', title: '' };
    const q = (searchInput.value || '').trim();
    clearTimeout(suggestTimer);
    if (q.length < 1) {
      hideSuggest();
      return;
    }
    suggestTimer = setTimeout(() => searchDependencies(q), 200);
  });

  searchInput?.addEventListener('keydown', event => {
    if (event.key === 'Escape') {
      hideSuggest();
      return;
    }
    if (event.key === 'ArrowDown') {
      event.preventDefault();
      if (searchList?.hidden) return;
      setSuggestActive(Math.min(suggestActive + 1, suggestItems.length - 1));
      return;
    }
    if (event.key === 'ArrowUp') {
      event.preventDefault();
      if (searchList?.hidden) return;
      setSuggestActive(Math.max(suggestActive - 1, 0));
      return;
    }
    if (event.key === 'Enter' && !searchList?.hidden && suggestActive >= 0 && suggestItems[suggestActive]) {
      event.preventDefault();
      choosePick(suggestItems[suggestActive]);
    }
  });

  searchList?.addEventListener('mousedown', event => {
    const option = event.target.closest('[data-code]');
    if (!option) return;
    event.preventDefault();
    choosePick({ code: option.getAttribute('data-code') || '', title: option.getAttribute('data-title') || '' });
  });

  form.addEventListener('submit', () => {
    render();
  });
})();
