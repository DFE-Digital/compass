/**
 * Autocomplete + removable table rows for Service line form (FIPS products + work items).
 * Expects a root .js-service-line-pick with data-sl-search-url, data-sl-field-name (ProductIds | ProjectIds),
 * data-sl-confirm-title, data-sl-confirm-body (for DfE confirm modal),
 * and child elements: .js-sl-search, .js-sl-results, tbody.js-sl-selected.
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
            this.confirmTitle = root.dataset.slConfirmTitle || "Remove item?";
            this.confirmBody =
                root.dataset.slConfirmBody ||
                "This item will be removed from the list. You can add it again before saving.";
            this.singleSubmit = root.dataset.slSingleSubmit === "true";
            this.linkForm = root.querySelector(".js-sl-link-form");
            this.linkIdInput = root.querySelector(".js-sl-link-id");
            this.selectedPreview = root.querySelector(".js-sl-selected-preview");
            this.selectedTitleEl = root.querySelector(".js-sl-selected-title");
            this.selectedMetaEl = root.querySelector(".js-sl-selected-meta");
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
                const removeEl = e.target.closest(".js-sl-remove");
                if (!removeEl) return;
                e.preventDefault();
                const row = removeEl.closest("tr");
                if (!row || !this.selectedEl?.contains(row)) return;
                const doRemove = () => {
                    row.remove();
                };
                if (typeof window.showConfirmModal === "function") {
                    window.showConfirmModal(this.confirmTitle, this.confirmBody, doRemove);
                } else {
                    doRemove();
                }
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
                let resultSub = sub;
                const workCode =
                    row.workCode != null && String(row.workCode).trim() !== ""
                        ? String(row.workCode).trim()
                        : sub && /^WI-/i.test(String(sub))
                          ? String(sub)
                          : "";
                if (row.uniqueId != null && String(row.uniqueId).trim() !== "") {
                    resultSub = "Service ID: " + row.uniqueId;
                    if (sub) {
                        resultSub += " · CMDB " + sub;
                    }
                } else if (workCode) {
                    resultSub = workCode;
                }
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
                    (resultSub
                        ? "<span class='service-line-pick__result-sub'> — " + esc(String(resultSub)) + "</span>"
                        : "");
                btn.addEventListener("click", () => {
                    if (this.singleSubmit) {
                        this.selectForLink(id, title, sub, row);
                    } else {
                        this.addItem(id, title, sub);
                    }
                    this.hideResults();
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

        selectForLink(id, label, subtitle, row) {
            if (this.linkIdInput) this.linkIdInput.value = id;
            if (this.linkForm) {
                this.linkForm.hidden = false;
                this.linkForm.classList.remove("govuk-!-display-none");
            }
            if (this.searchInput) {
                this.searchInput.value = label;
                this.searchInput.setAttribute("aria-expanded", "false");
            }
            if (this.selectedPreview) {
                this.selectedPreview.hidden = false;
            }
            if (this.selectedTitleEl) {
                this.selectedTitleEl.textContent = label;
            }
            if (this.selectedMetaEl) {
                const parts = [];
                const uniqueId = row && row.uniqueId != null ? String(row.uniqueId) : "";
                const workCode =
                    row && row.workCode != null && String(row.workCode).trim() !== ""
                        ? String(row.workCode).trim()
                        : subtitle && /^WI-/i.test(String(subtitle))
                          ? String(subtitle)
                          : "";
                const serviceOwner =
                    row && row.serviceOwner != null && String(row.serviceOwner).trim() !== ""
                        ? String(row.serviceOwner).trim()
                        : "";
                if (uniqueId) {
                    parts.push("Service ID: " + uniqueId);
                }
                if (workCode) {
                    parts.push("Work item: " + workCode);
                }
                if (serviceOwner) {
                    parts.push("Service owner: " + serviceOwner);
                } else if (subtitle && !workCode) {
                    parts.push("CMDB ID: " + subtitle);
                }
                this.selectedMetaEl.textContent = parts.join(" · ");
            }
        }

        addItem(id, label, subtitle) {
            if (!this.selectedEl) return;
            if (this.getSelectedIds().has(id)) return;
            const tr = document.createElement("tr");
            tr.dataset.id = id;
            const tdName = document.createElement("td");
            tdName.className = "govuk-table__cell";
            const hid = document.createElement("input");
            hid.type = "hidden";
            hid.name = this.fieldName;
            hid.value = id;
            tdName.appendChild(hid);
            const nameSpan = document.createElement("span");
            nameSpan.textContent = label;
            tdName.appendChild(nameSpan);
            if (subtitle) {
                const meta = document.createElement("div");
                meta.className = "govuk-body-s dfe-c-text-muted modern-work-table__meta";
                meta.textContent = subtitle;
                tdName.appendChild(meta);
            }
            const tdAct = document.createElement("td");
            tdAct.className = "govuk-table__cell govuk-!-text-align-right";
            const rem = document.createElement("a");
            rem.href = "#";
            rem.className = "govuk-link js-sl-remove";
            rem.setAttribute("aria-label", "Remove " + label);
            rem.textContent = "Remove";
            tdAct.appendChild(rem);
            tr.appendChild(tdName);
            tr.appendChild(tdAct);
            this.selectedEl.appendChild(tr);
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
