/**
 * Autocomplete + removable list for Service line form (FIPS products + work items).
 * Expects a root .js-service-line-pick with data-sl-search-url, data-sl-field-name (ProductIds | ProjectIds),
 * and child elements: .js-sl-search, .js-sl-results, .js-sl-selected.
 */
(function () {
    const DEBOUNCE_MS = 300;
    const MIN_CHARS = 2;

    function esc(s) {
        if (s == null) return "";
        const d = document.createElement("div");
        d.textContent = s;
        return d.innerHTML;
    }

    class ServiceLinePicker {
        /** @param {HTMLElement} root */
        constructor(root) {
            this.root = root;
            this.searchUrl = root.dataset.slSearchUrl || "";
            this.fieldName = root.dataset.slFieldName || "ProductIds";
            this.isGuid = (root.dataset.slIdKind || "guid") === "guid";
            this.searchInput = root.querySelector(".js-sl-search");
            this.resultsEl = root.querySelector(".js-sl-results");
            this.selectedEl = root.querySelector(".js-sl-selected");
            this.debounce = null;
            this.abort = null;
            this.boundDocClick = (e) => {
                if (!this.root.contains(e.target)) this.hideResults();
            };
            if (this.searchInput) {
                this.searchInput.addEventListener("input", () => this.onInput());
                this.searchInput.addEventListener("keydown", (e) => {
                    if (e.key === "Escape") this.hideResults();
                });
            }
            this.selectedEl?.addEventListener("click", (e) => {
                const btn = e.target.closest(".js-sl-remove");
                if (!btn) return;
                const li = btn.closest("li");
                if (li) li.remove();
            });
            document.addEventListener("click", this.boundDocClick);
        }

        destroy() {
            document.removeEventListener("click", this.boundDocClick);
        }

        getSelectedIds() {
            const ids = new Set();
            this.selectedEl?.querySelectorAll('input[type="hidden"]').forEach((inp) => {
                if (inp.value) ids.add(inp.value);
            });
            return ids;
        }

        onInput() {
            const q = (this.searchInput?.value || "").trim();
            clearTimeout(this.debounce);
            this.abort?.abort();
            if (q.length < MIN_CHARS) {
                this.hideResults();
                return;
            }
            this.debounce = setTimeout(() => this.search(q), DEBOUNCE_MS);
        }

        async search(q) {
            if (!this.searchUrl) return;
            this.abort?.abort();
            this.abort = new AbortController();
            try {
                const url = this.searchUrl + (this.searchUrl.includes("?") ? "&" : "?") + "q=" + encodeURIComponent(q);
                const res = await fetch(url, { signal: this.abort.signal, credentials: "same-origin" });
                if (res.status === 403) {
                    this.renderError("You do not have access to search.");
                    return;
                }
                if (!res.ok) throw new Error("search failed");
                const data = await res.json();
                const list = data.results;
                if (!Array.isArray(list) || list.length === 0) {
                    this.renderEmpty();
                    return;
                }
                this.renderResults(list);
            } catch (e) {
                if (e.name !== "AbortError") this.renderError("Search failed. Try again.");
            }
        }

        renderEmpty() {
            if (!this.resultsEl) return;
            this.resultsEl.hidden = false;
            this.resultsEl.innerHTML =
                '<p class="govuk-body-s govuk-!-margin-0" role="status">No matches. Try a different search.</p>';
        }

        renderError(msg) {
            if (!this.resultsEl) return;
            this.resultsEl.hidden = false;
            this.resultsEl.innerHTML = '<p class="govuk-body-s govuk-!-margin-0" role="alert">' + esc(msg) + "</p>";
        }

        /** @param {any[]} list */
        renderResults(list) {
            if (!this.resultsEl) return;
            const selected = this.getSelectedIds();
            const ul = document.createElement("ul");
            ul.className = "service-line-pick__results-list govuk-list";
            ul.setAttribute("role", "listbox");
            let i = 0;
            for (const row of list) {
                const id = this.isGuid ? String(row.id) : String(row.id);
                if (selected.has(id)) continue;
                const title = row.title || row.name || "—";
                const sub = row.subtitle || row.code || null;
                const li = document.createElement("li");
                li.setAttribute("role", "option");
                const btn = document.createElement("button");
                btn.type = "button";
                btn.className = "service-line-pick__result-btn govuk-link";
                btn.setAttribute("data-id", id);
                btn.setAttribute("data-title", title);
                btn.innerHTML =
                    "<span class='service-line-pick__result-title'>" +
                    esc(title) +
                    "</span>" +
                    (sub
                        ? "<span class='service-line-pick__result-sub'> — " + esc(String(sub)) + "</span>"
                        : "");
                btn.addEventListener("click", () => {
                    this.addItem(id, title);
                    this.hideResults();
                    if (this.searchInput) this.searchInput.value = "";
                });
                li.appendChild(btn);
                ul.appendChild(li);
                i++;
            }
            this.resultsEl.hidden = false;
            if (i === 0) {
                this.renderEmpty();
                return;
            }
            this.resultsEl.innerHTML = "";
            this.resultsEl.appendChild(ul);
        }

        addItem(id, label) {
            if (!this.selectedEl) return;
            if (this.getSelectedIds().has(id)) return;
            const li = document.createElement("li");
            li.className = "service-line-pick__row";
            li.dataset.id = id;
            const hid = document.createElement("input");
            hid.type = "hidden";
            hid.name = this.fieldName;
            hid.value = id;
            const span = document.createElement("span");
            span.className = "service-line-pick__row-label";
            span.textContent = label;
            const rem = document.createElement("button");
            rem.type = "button";
            rem.className = "govuk-link service-line-pick__remove js-sl-remove";
            rem.setAttribute("aria-label", "Remove " + label);
            rem.textContent = "Remove";
            li.appendChild(hid);
            li.appendChild(span);
            li.appendChild(rem);
            this.selectedEl.appendChild(li);
        }

        hideResults() {
            if (this.resultsEl) {
                this.resultsEl.hidden = true;
                this.resultsEl.innerHTML = "";
            }
        }
    }

    function init() {
        document.querySelectorAll(".js-service-line-pick").forEach((root) => {
            if (!(root instanceof HTMLElement) || root.dataset.slInitialized) return;
            root.dataset.slInitialized = "1";
            try {
                new ServiceLinePicker(root);
            } catch (e) {
                console.error("service-line-pickers", e);
            }
        });
    }

    if (document.readyState === "loading")
        document.addEventListener("DOMContentLoaded", init);
    else init();
})();
