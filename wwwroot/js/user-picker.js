(function () {
    const pickers = [];
    const MIN_CHARS = 3;
    const DEBOUNCE_MS = 250;

    class UserPicker {
        constructor(root) {
            this.root = root;
            this.input = root.querySelector('.js-user-picker-input');
            this.hidden = root.querySelector('.js-user-picker-value');
            this.nameInput = root.querySelector('.js-user-picker-name');
            this.emailInput = root.querySelector('.js-user-picker-email');
            this.results = root.querySelector('.js-user-picker-results');
            this.message = root.querySelector('.js-user-picker-message');
            this.spinner = root.querySelector('.js-user-picker-spinner');
            this.summary = root.querySelector('.js-user-picker-summary');
            this.defaultSummary = this.summary?.dataset.defaultText ?? 'No user selected.';
            this.searchUrl = root.dataset.searchUrl || '/api/users/search';
            this.selectUrl = root.dataset.selectUrl || '/api/users/select';
            this.timeout = null;
            this.abortController = null;
            this.lastQuery = '';
            this.bindEvents();
        }

        bindEvents() {
            if (!this.input) {
                return;
            }

            this.input.addEventListener('input', () => {
                const query = this.input.value.trim();
                if (query.length < MIN_CHARS) {
                    this.hideResults();
                    this.setMessage('Type at least three characters to start searching.');
                    this.lastQuery = '';
                    return;
                }

                if (query === this.lastQuery) {
                    return;
                }

                this.lastQuery = query;
                this.debounce(() => this.search(query), DEBOUNCE_MS);
            });

            document.addEventListener('click', (event) => {
                if (!this.root.contains(event.target)) {
                    this.hideResults();
                }
            });

            const clearButton = this.root.querySelector('.js-user-picker-clear');
            if (clearButton) {
                clearButton.addEventListener('click', () => {
                    this.clearSelection();
                });
            }
        }

        debounce(callback, delay) {
            clearTimeout(this.timeout);
            this.timeout = setTimeout(callback, delay);
        }

        async search(query) {
            this.setMessage('');
            this.showSpinner(true);
            this.abortController?.abort();
            this.abortController = new AbortController();
            try {
                const res = await fetch(`${this.searchUrl}?q=${encodeURIComponent(query)}`, {
                    signal: this.abortController.signal,
                    credentials: 'same-origin'
                });

                if (res.status === 429) {
                    this.setMessage('Microsoft Graph is rate-limiting search. Please wait a moment and try again.');
                    this.hideResults();
                    return;
                }

                if (!res.ok) {
                    throw new Error('Search failed');
                }

                const results = await res.json();
                if (!Array.isArray(results) || results.length === 0) {
                    this.setMessage('No matches yet. Refine your search.');
                    this.hideResults();
                    return;
                }

                this.renderResults(results);
            } catch (error) {
                if (error.name !== 'AbortError') {
                    this.setMessage('Unable to search right now. Please try again.');
                    this.hideResults();
                }
            } finally {
                this.showSpinner(false);
            }
        }

        renderResults(users) {
            if (!this.results) {
                return;
            }

            this.results.innerHTML = users.map(user => {
                const id = escapeHtml(user.id ?? '');
                const name = escapeHtml(user.name ?? '');
                const email = escapeHtml(user.email ?? '');
                const jobTitle = escapeHtml(user.jobTitle ?? '');
                const meta = jobTitle ? `${jobTitle} • ${email}` : email;
                return `
                    <button type="button" class="user-picker__result" data-object-id="${id}">
                        <span class="user-picker__result-name">${name}</span>
                        <span class="user-picker__result-meta">${meta}</span>
                    </button>
                `;
            }).join('');

            Array.from(this.results.querySelectorAll('.user-picker__result')).forEach(button => {
                button.addEventListener('click', () => {
                    const objectId = button.getAttribute('data-object-id');
                    if (objectId) {
                        this.selectUser(objectId);
                    }
                });
            });

            this.results.style.display = 'block';
        }

        hideResults() {
            if (this.results) {
                this.results.style.display = 'none';
                this.results.innerHTML = '';
            }
        }

        async selectUser(objectId) {
            this.showSpinner(true);
            this.setMessage('');
            try {
                const res = await fetch(this.selectUrl, {
                    method: 'POST',
                    credentials: 'same-origin',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify({ objectId })
                });

                if (res.status === 429) {
                    this.setMessage('Microsoft Graph is rate-limiting lookups. Please wait a moment and try again.');
                    return;
                }

                if (!res.ok) {
                    throw new Error('Select failed');
                }

                const user = await res.json();
                this.applySelection(user);
            } catch (error) {
                this.setMessage('Unable to fetch user details. Please try again.');
            } finally {
                this.showSpinner(false);
                this.hideResults();
            }
        }

        applySelection(user) {
            if (this.hidden) {
                this.hidden.value = user?.id ?? '';
            }
            if (this.nameInput) {
                this.nameInput.value = user?.name ?? '';
            }
            if (this.emailInput) {
                this.emailInput.value = user?.email ?? '';
            }

            if (this.summary) {
                if (user?.id) {
                    this.summary.innerHTML = `
                        <div class="user-picker__selected-card">
                            <div class="user-picker__selected-name">${escapeHtml(user.name ?? '')}</div>
                            <div class="user-picker__selected-meta">${escapeHtml(user.email ?? '')}</div>
                            <button type="button" class="btn btn-link btn-sm js-user-picker-clear">Remove</button>
                        </div>
                    `;
                    this.summary.querySelector('.js-user-picker-clear')?.addEventListener('click', () => this.clearSelection());
                } else {
                    this.summary.innerHTML = `<span class="text-muted">${this.defaultSummary}</span>`;
                }
            }

            if (this.input && user?.name) {
                this.input.value = user.name;
            }

            this.setMessage('User selected. Save your changes to store this contact.');
        }

        clearSelection() {
            if (this.hidden) {
                this.hidden.value = '';
            }
            if (this.nameInput) {
                this.nameInput.value = '';
            }
            if (this.emailInput) {
                this.emailInput.value = '';
            }
            if (this.summary) {
                this.summary.innerHTML = `<span class="text-muted">${this.defaultSummary}</span>`;
            }
            if (this.input) {
                this.input.value = '';
                this.input.focus();
            }
            this.setMessage('Selection cleared.');
        }

        setMessage(text) {
            if (!this.message) {
                return;
            }
            if (!text) {
                this.message.textContent = '';
                this.message.style.display = 'none';
            } else {
                this.message.textContent = text;
                this.message.style.display = 'block';
            }
        }

        showSpinner(visible) {
            if (this.spinner) {
                this.spinner.style.display = visible ? 'inline-block' : 'none';
            }
        }
    }

    function escapeHtml(value) {
        if (typeof value !== 'string') {
            return '';
        }
        return value.replace(/[&<>"']/g, (match) => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }[match]));
    }

    document.addEventListener('DOMContentLoaded', () => {
        document.querySelectorAll('[data-user-picker]').forEach(el => {
            pickers.push(new UserPicker(el));
        });
    });
})();


