(function () {
  const form = document.getElementById('census-question');
  const body = document.getElementById('census-contacts-body');
  const modal = document.getElementById('census-contact-modal');
  const titleEl = document.getElementById('census-contact-modal-title');
  const roleGroup = document.getElementById('census-contact-modal-role-group');
  const roleInput = document.getElementById('census-contact-modal-role');
  const saveEl = document.getElementById('census-contact-modal-save');
  const removeModal = document.getElementById('census-remove-modal');
  const removeConfirm = document.getElementById('census-remove-modal-confirm');
  const pickerRoot = modal?.querySelector('[data-user-picker]');
  if (!body) return;

  if (window.DfeFrontend && typeof window.DfeFrontend.initModal === 'function') {
    if (modal) window.DfeFrontend.initModal(modal);
    if (removeModal) window.DfeFrontend.initModal(removeModal);
  }

  let editingRow = null;
  let pendingRemoveRow = null;

  function isAdded(row) {
    return row.getAttribute('data-added') === 'true';
  }

  function hasPerson(row) {
    const name = (row.querySelector('.js-census-contact-name')?.value || '').trim();
    const email = (row.querySelector('.js-census-contact-email')?.value || '').trim();
    return name !== '' || email !== '';
  }

  function actionLabel(row, button) {
    return hasPerson(row)
      ? (button.dataset.changeLabel || 'Change')
      : (button.dataset.addLabel || 'Add');
  }

  function openModal() {
    if (!modal) return;
    if (typeof modal.showModal === 'function') modal.showModal();
    else modal.setAttribute('open', '');
  }

  function closeModal() {
    if (!modal) return;
    if (typeof modal.close === 'function') modal.close();
    else modal.removeAttribute('open');
  }

  function pickerFields() {
    return {
      id: pickerRoot?.querySelector('.js-user-picker-value'),
      name: pickerRoot?.querySelector('.js-user-picker-name'),
      email: pickerRoot?.querySelector('.js-user-picker-email'),
      input: pickerRoot?.querySelector('.js-user-picker-input'),
      summary: pickerRoot?.querySelector('.js-user-picker-summary')
    };
  }

  function setPicker(id, name, email) {
    const fields = pickerFields();
    if (fields.id) fields.id.value = id || '';
    if (fields.name) fields.name.value = name || '';
    if (fields.email) fields.email.value = email || '';
    if (fields.input) fields.input.value = name || '';
    const summary = fields.summary;
    if (!summary) return;
    const empty = summary.dataset.defaultText || 'No person selected.';
    if (name || email) {
      summary.innerHTML = `<div class="govuk-inset-text govuk-!-margin-top-2 govuk-!-margin-bottom-0">
        <div class="user-picker__selected-name"></div>
        <div class="user-picker__selected-meta"></div>
        <button type="button" class="govuk-link govuk-link--no-visited-state js-user-picker-clear">Remove</button>
      </div>`;
      summary.querySelector('.user-picker__selected-name').textContent = name || '—';
      summary.querySelector('.user-picker__selected-meta').textContent = email || '';
      summary.querySelector('.js-user-picker-clear')?.addEventListener('click', () => setPicker('', '', ''));
    } else {
      summary.innerHTML = `<span class="govuk-hint">${empty}</span>`;
    }
  }

  function editRow(row) {
    editingRow = row;
    const added = isAdded(row);
    const person = hasPerson(row);
    const edit = row.querySelector('.js-census-contact-edit');
    if (titleEl) {
      titleEl.textContent = added && !person
        ? 'Add a contact'
        : person
          ? 'Change person'
          : 'Add a person';
    }
    if (roleGroup) roleGroup.hidden = !added;
    if (roleInput) roleInput.value = row.querySelector('.js-census-contact-role-name')?.value || '';
    setPicker(
      row.querySelector('.js-census-contact-user-id')?.value || '',
      row.querySelector('.js-census-contact-name')?.value || '',
      row.querySelector('.js-census-contact-email')?.value || ''
    );
    if (typeof window.initializeUserPickers === 'function') window.initializeUserPickers();
    openModal();
    setTimeout(() => {
      if (added) roleInput?.focus();
      else pickerFields().input?.focus();
    }, 50);
  }

  function refreshAction(row) {
    const button = row.querySelector('.js-census-contact-edit');
    if (!button) return;
    const label = actionLabel(row, button);
    button.textContent = label;
    button.classList.toggle('govuk-button--secondary', label !== (button.dataset.addLabel || 'Add'));
  }

  function paintRow(row) {
    const role = (row.querySelector('.js-census-contact-role-name')?.value || '').trim();
    const name = (row.querySelector('.js-census-contact-name')?.value || '').trim();
    const email = (row.querySelector('.js-census-contact-email')?.value || '').trim();
    const roleLabel = row.querySelector('.js-census-contact-role-label');
    if (roleLabel) roleLabel.textContent = role || 'Not set';
    const nameText = row.querySelector('.js-census-contact-name-text');
    if (nameText) nameText.textContent = name || 'Not set';
    const emailText = row.querySelector('.js-census-contact-email-text');
    if (emailText) emailText.textContent = email;
    refreshAction(row);
  }

  function reindex() {
    [...body.querySelectorAll('.census-contact-row')].forEach((row, index) => {
      row.setAttribute('data-contact-index', String(index));
      row.querySelectorAll('[name]').forEach(input => {
        input.name = input.name.replace(/Contacts\[\d+]/g, 'Contacts[' + index + ']');
      });
    });
  }

  function bindRow(row) {
    const edit = row.querySelector('.js-census-contact-edit');
    if (edit && !edit.dataset.bound) {
      edit.dataset.bound = 'true';
      edit.addEventListener('click', () => editRow(row));
    }

    const remove = row.querySelector('.js-census-contact-remove');
    if (remove && !remove.dataset.bound) {
      remove.dataset.bound = 'true';
      remove.addEventListener('click', () => {
        pendingRemoveRow = row;
        if (!removeModal) return;
        if (typeof removeModal.showModal === 'function') removeModal.showModal();
        else removeModal.setAttribute('open', '');
      });
    }
  }

  function addRow() {
    const index = body.querySelectorAll('.census-contact-row').length;
    const key = 'added-' + (window.crypto?.randomUUID?.() || String(Date.now()));
    const tr = document.createElement('tr');
    tr.className = 'govuk-table__row census-contact-row';
    tr.setAttribute('data-contact-index', String(index));
    tr.setAttribute('data-added', 'true');
    tr.innerHTML = '<td class="govuk-table__cell">'
      + '<input type="hidden" name="Contacts[' + index + '].RoleKey" value="' + key + '" />'
      + '<input type="hidden" name="Contacts[' + index + '].IsAdded" value="true" />'
      + '<input type="hidden" name="Contacts[' + index + '].OriginalName" value="" />'
      + '<input type="hidden" name="Contacts[' + index + '].OriginalEmail" value="" />'
      + '<input type="hidden" class="js-census-contact-role-name" name="Contacts[' + index + '].RoleName" value="" />'
      + '<span class="govuk-!-font-weight-bold js-census-contact-role-label">Not set</span>'
      + '</td>'
      + '<td class="govuk-table__cell">'
      + '<input type="hidden" class="js-census-contact-user-id" name="Contacts[' + index + '].SelectedUserId" value="" />'
      + '<input type="hidden" class="js-census-contact-name" name="Contacts[' + index + '].SelectedName" value="" />'
      + '<input type="hidden" class="js-census-contact-email" name="Contacts[' + index + '].SelectedEmail" value="" />'
      + '<p class="govuk-body govuk-!-margin-bottom-0 js-census-contact-name-text">Not set</p>'
      + '<p class="govuk-body govuk-!-margin-bottom-0 census-contact-email js-census-contact-email-text"></p>'
      + '</td>'
      + '<td class="govuk-table__cell"><div class="govuk-button-group census-contact-actions">'
      + '<button type="button" class="govuk-button js-census-contact-edit" data-add-label="Add" data-change-label="Change">Add</button>'
      + '<button type="button" class="govuk-button govuk-button--secondary js-census-contact-remove">Remove</button>'
      + '</div></td>';
    body.appendChild(tr);
    bindRow(tr);
    editRow(tr);
  }

  body.querySelectorAll('.census-contact-row').forEach(bindRow);
  document.querySelectorAll('.js-census-contact-add').forEach(button => {
    button.addEventListener('click', addRow);
  });
  form?.addEventListener('submit', reindex);

  saveEl?.addEventListener('click', () => {
    if (!editingRow) return;
    if (isAdded(editingRow)) {
      const role = (roleInput?.value || '').trim();
      if (!role) {
        roleInput?.focus();
        return;
      }
      const roleField = editingRow.querySelector('.js-census-contact-role-name');
      if (roleField) roleField.value = role;
    }
    const fields = pickerFields();
    const idField = editingRow.querySelector('.js-census-contact-user-id');
    const nameField = editingRow.querySelector('.js-census-contact-name');
    const emailField = editingRow.querySelector('.js-census-contact-email');
    if (idField) idField.value = fields.id?.value || '';
    if (nameField) nameField.value = fields.name?.value || '';
    if (emailField) emailField.value = fields.email?.value || '';
    paintRow(editingRow);
    window.censusMarkUnsaved?.();
    closeModal();
  });

  modal?.querySelectorAll('[data-dfe-modal-close]').forEach(button => {
    button.addEventListener('click', closeModal);
  });

  modal?.addEventListener('close', () => {
    editingRow = null;
  });

  removeConfirm?.addEventListener('click', () => {
    if (pendingRemoveRow) pendingRemoveRow.remove();
    pendingRemoveRow = null;
    reindex();
    window.censusMarkUnsaved?.();
    if (removeModal) {
      if (typeof removeModal.close === 'function') removeModal.close();
      else removeModal.removeAttribute('open');
    }
  });

  removeModal?.querySelectorAll('[data-dfe-modal-close]').forEach(button => {
    button.addEventListener('click', () => {
      pendingRemoveRow = null;
      if (typeof removeModal.close === 'function') removeModal.close();
      else removeModal.removeAttribute('open');
    });
  });
})();
