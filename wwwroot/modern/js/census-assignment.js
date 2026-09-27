/**
 * Census assignment theme form: apply declarative visibility rules client-side
 * so conditional fields (e.g. public URL when has-public-url = Yes) update without a round-trip.
 * Also supports Allow-multiple text lists (add another / remove with confirm; keep ≥1 row).
 */
(function () {
  function parseVisibility(raw) {
    if (!raw || !String(raw).trim()) return null;
    try {
      return JSON.parse(raw);
    } catch {
      return null;
    }
  }

  function normalizeScalar(value) {
    if (value == null) return "";
    var s = String(value).trim();
    if (s.toLowerCase() === "true") return "Yes";
    if (s.toLowerCase() === "false") return "No";
    return s;
  }

  function fieldValue(root, fieldKey) {
    var container = root.querySelector('[data-field-key="' + fieldKey + '"]');
    if (!container) return "";

    var checkedRadio = container.querySelector('input[type="radio"]:checked');
    if (checkedRadio) return normalizeScalar(checkedRadio.value);

    var checkedBoxes = container.querySelectorAll('input[type="checkbox"]:checked');
    if (checkedBoxes.length) {
      return Array.prototype.map.call(checkedBoxes, function (el) {
        return normalizeScalar(el.value);
      }).join(",");
    }

    var input = container.querySelector("input, textarea, select");
    if (input) return normalizeScalar(input.value);
    return "";
  }

  function isAnswered(value) {
    return value != null && String(value).trim() !== "";
  }

  function applyVisibility(form) {
    var blocks = form.querySelectorAll("[data-field-id][data-visibility]");
    blocks.forEach(function (block) {
      var rule = parseVisibility(block.getAttribute("data-visibility"));
      if (!rule || !rule.fieldKey) {
        block.style.display = "";
        return;
      }

      var actual = fieldValue(form, rule.fieldKey);
      var visible = true;
      if (!isAnswered(actual)) {
        visible = false;
      } else if (Object.prototype.hasOwnProperty.call(rule, "equals")) {
        visible = normalizeScalar(rule.equals).toLowerCase() === actual.toLowerCase();
      } else if (Object.prototype.hasOwnProperty.call(rule, "notEquals")) {
        visible = normalizeScalar(rule.notEquals).toLowerCase() !== actual.toLowerCase();
      }

      block.style.display = visible ? "" : "none";
    });
  }

  function buildTextListRow(fieldName, multiline) {
    var row = document.createElement("div");
    row.className = "govuk-form-group govuk-!-margin-bottom-2 js-census-text-list-row";
    var control;
    if (multiline) {
      control = document.createElement("textarea");
      control.className = "govuk-textarea";
      control.rows = 3;
    } else {
      control = document.createElement("input");
      control.className = "govuk-input";
      control.type = "text";
    }
    control.name = fieldName;
    row.appendChild(control);
    var actions = document.createElement("p");
    actions.className =
      "govuk-body-s govuk-!-margin-top-1 govuk-!-margin-bottom-0 js-census-text-list-remove-wrap";
    var remove = document.createElement("a");
    remove.href = "#";
    remove.className = "govuk-link js-census-text-list-remove";
    remove.textContent = "Remove";
    actions.appendChild(remove);
    row.appendChild(actions);
    return row;
  }

  function syncTextListRemoveVisibility(items) {
    var rows = items.querySelectorAll(".js-census-text-list-row");
    var showRemove = rows.length > 1;
    rows.forEach(function (row) {
      var wrap = row.querySelector(".js-census-text-list-remove-wrap");
      if (!wrap) return;
      if (showRemove) wrap.removeAttribute("hidden");
      else wrap.setAttribute("hidden", "");
    });
  }

  function confirmRemove(title, message, onConfirm) {
    if (typeof window.showConfirmModal === "function") {
      window.showConfirmModal(title, message, onConfirm);
      return;
    }
    onConfirm();
  }

  function removeMessageForValue(value) {
    var trimmed = (value || "").trim();
    if (trimmed) {
      var preview = trimmed.length > 80 ? trimmed.slice(0, 77) + "…" : trimmed;
      return (
        'This will remove "' +
        preview +
        '" from the list. You can add it again before saving.'
      );
    }
    return "This empty entry will be removed from the list.";
  }

  function initTextLists(form) {
    form.querySelectorAll(".js-census-text-list").forEach(function (list) {
      if (list.getAttribute("data-readonly") === "true") return;
      var fieldName = list.getAttribute("data-field-name");
      var multiline = list.getAttribute("data-multiline") === "true";
      var items = list.querySelector(".js-census-text-list-items");
      var add = list.querySelector(".js-census-text-list-add");
      if (!items || !add || !fieldName) return;

      syncTextListRemoveVisibility(items);

      add.addEventListener("click", function (e) {
        e.preventDefault();
        items.appendChild(buildTextListRow(fieldName, multiline));
        syncTextListRemoveVisibility(items);
      });

      list.addEventListener("click", function (e) {
        var rem = e.target.closest(".js-census-text-list-remove");
        if (!rem) return;
        e.preventDefault();
        var row = rem.closest(".js-census-text-list-row");
        if (!row) return;
        var rows = items.querySelectorAll(".js-census-text-list-row");
        // Always keep at least one row; Remove is hidden when only one remains.
        if (rows.length <= 1) return;

        var input = row.querySelector("input, textarea");
        var value = input ? input.value : "";
        confirmRemove("Remove this entry?", removeMessageForValue(value), function () {
          if (items.querySelectorAll(".js-census-text-list-row").length <= 1) return;
          row.remove();
          syncTextListRemoveVisibility(items);
        });
      });
    });
  }

  function init(form) {
    applyVisibility(form);
    initTextLists(form);
    form.addEventListener("change", function () {
      applyVisibility(form);
    });
    form.addEventListener("input", function () {
      applyVisibility(form);
    });
  }

  document.addEventListener("DOMContentLoaded", function () {
    document.querySelectorAll('form[data-module="census-assignment-form"]').forEach(init);
  });
})();
