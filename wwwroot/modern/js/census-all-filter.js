(function () {
  var root = document.querySelector("[data-module='census-all-filter']");
  if (!root) return;

  var search = root.querySelector("#census-all-search");
  var peopleFind = root.querySelector("#census-people-find");
  var count = root.querySelector("[data-census-count]");
  var empty = root.querySelector("[data-census-empty]");
  var table = root.querySelector("[data-census-table]");
  var rows = root.querySelectorAll("[data-census-row]");
  var selectedPanel = root.querySelector("[data-census-selected]");
  var selectedTags = root.querySelector("[data-census-selected-tags]");

  function selected(name) {
    return Array.prototype.map.call(root.querySelectorAll('input[name="' + name + '"]:checked'), function (el) {
      return el.value;
    });
  }

  function escapeHtml(value) {
    return String(value || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function tagList(title, name, values) {
    if (!values.length) return "";
    return (
      '<h3 class="govuk-heading-s govuk-!-margin-bottom-0">' +
      escapeHtml(title) +
      '</h3><ul class="moj-filter-tags">' +
      values
        .map(function (value) {
          return (
            '<li><button type="button" class="moj-filter__tag" data-remove="' +
            escapeHtml(name) +
            '" data-value="' +
            escapeHtml(value) +
            '"><span class="govuk-visually-hidden">Remove this filter</span> ' +
            escapeHtml(value) +
            "</button></li>"
          );
        })
        .join("") +
      "</ul>"
    );
  }

  function renderSelected() {
    if (!selectedPanel || !selectedTags) return;
    var q = ((search && search.value) || "").trim();
    var html = "";
    if (q) html += tagList("Search", "search", [q]);
    html += tagList("Submission status", "status", selected("status"));
    html += tagList("Business area", "area", selected("area"));
    html += tagList("Contacts", "contact", selected("contact"));
    selectedTags.innerHTML = html;
    selectedPanel.hidden = html.length === 0;
  }

  function apply() {
    renderSelected();
    var q = ((search && search.value) || "").trim().toLowerCase();
    var people = selected("contact").map(function (v) { return v.toLowerCase(); });
    var areas = selected("area");
    var statuses = selected("status");
    var shown = 0;

    rows.forEach(function (row) {
      var hay = (row.getAttribute("data-search") || "").toLowerCase();
      var okSearch = !q || hay.indexOf(q) !== -1;
      var rowPeople = (row.getAttribute("data-contacts") || "").toLowerCase().split("|").filter(Boolean);
      var okPeople = people.length === 0 || people.some(function (p) { return rowPeople.indexOf(p) !== -1; });
      var rowAreas = (row.getAttribute("data-areas") || "").split("|").filter(Boolean);
      var okAreas = areas.length === 0 || areas.some(function (a) { return rowAreas.indexOf(a) !== -1; });
      var status = row.getAttribute("data-status") || "";
      var okStatus = statuses.length === 0 || statuses.indexOf(status) !== -1;
      var show = okSearch && okPeople && okAreas && okStatus;
      row.hidden = !show;
      if (show) shown += 1;
    });

    if (count) count.textContent = shown === 1 ? "1 product" : shown + " products";
    if (empty) empty.hidden = shown > 0;
    if (table) table.hidden = shown === 0;
  }

  if (search) {
    search.addEventListener("keydown", function (e) {
      if (e.key === "Enter") e.preventDefault();
    });
  }

  var peopleEmpty = root.querySelector("[data-people-empty]");

  function filterPeopleList() {
    if (!peopleFind) return;
    var q = (peopleFind.value || "").trim().toLowerCase();
    var shown = 0;
    root.querySelectorAll("[data-people-item]").forEach(function (item) {
      var label = (item.getAttribute("data-people-label") || "").toLowerCase();
      var input = item.querySelector("input");
      var checked = input && input.checked;
      var show = !q || label.indexOf(q) !== -1 || checked;
      item.classList.toggle("census-all__option--hidden", !show);
      if (show) shown += 1;
    });
    if (peopleEmpty) peopleEmpty.hidden = shown > 0;
  }

  if (peopleFind) {
    peopleFind.addEventListener("keydown", function (e) {
      if (e.key === "Enter") e.preventDefault();
    });
    peopleFind.addEventListener("input", function (e) {
      e.stopPropagation();
      filterPeopleList();
    });
  }

  var clear = root.querySelector("[data-census-clear]");
  if (clear) {
    clear.addEventListener("click", function (e) {
      e.preventDefault();
      if (search) search.value = "";
      if (peopleFind) peopleFind.value = "";
      root.querySelectorAll("input[type=checkbox]").forEach(function (el) {
        el.checked = false;
      });
      root.querySelectorAll("[data-people-item]").forEach(function (item) {
        item.classList.remove("census-all__option--hidden");
      });
      if (peopleEmpty) peopleEmpty.hidden = true;
      apply();
    });
  }

  if (selectedTags) {
    selectedTags.addEventListener("click", function (e) {
      var tag = e.target.closest("[data-remove]");
      if (!tag) return;
      var name = tag.getAttribute("data-remove");
      var value = tag.getAttribute("data-value") || "";
      if (name === "search") {
        if (search) search.value = "";
      } else {
        root.querySelectorAll('input[name="' + name + '"]').forEach(function (el) {
          if (el.value === value) el.checked = false;
        });
      }
      apply();
    });
  }

  root.addEventListener("input", function (e) {
    if (e.target && e.target.id === "census-people-find") return;
    apply();
  });
  root.addEventListener("change", apply);
  apply();
})();
