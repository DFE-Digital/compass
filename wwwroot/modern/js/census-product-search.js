(function () {
  var DEBOUNCE_MS = 180;
  var root = document.querySelector("[data-module='census-product-search']");
  if (!root) return;

  var input = root.querySelector(".census-search__input");
  var list = root.querySelector(".census-search__results");
  var form = root.closest("form");
  var url = root.getAttribute("data-suggest-url") || "";
  if (!input || !list || !url) return;

  var debounce = null;
  var abort = null;
  var items = [];
  var active = -1;
  var query = "";

  function hide() {
    list.hidden = true;
    list.innerHTML = "";
    items = [];
    active = -1;
    input.removeAttribute("aria-activedescendant");
    input.setAttribute("aria-expanded", "false");
  }

  function setActive(index, scroll) {
    var options = list.querySelectorAll("[role='option']");
    options.forEach(function (el) {
      el.classList.remove("is-active");
      el.setAttribute("aria-selected", "false");
    });
    active = index;
    if (active < 0 || !options[active]) {
      input.removeAttribute("aria-activedescendant");
      return;
    }
    options[active].classList.add("is-active");
    options[active].setAttribute("aria-selected", "true");
    input.setAttribute("aria-activedescendant", options[active].id);
    if (scroll) options[active].scrollIntoView({ block: "nearest" });
  }

  function go(href) {
    if (href) window.location.href = href;
  }

  function highlight(title) {
    var safe = escapeHtml(title);
    if (!query) return safe;
    var lower = title.toLowerCase();
    var needle = query.toLowerCase();
    var i = lower.indexOf(needle);
    if (i < 0) return safe;
    return escapeHtml(title.slice(0, i))
      + '<mark class="census-search__mark">' + escapeHtml(title.slice(i, i + query.length)) + "</mark>"
      + escapeHtml(title.slice(i + query.length));
  }

  function render(results) {
    items = results || [];
    if (!items.length) {
      list.hidden = false;
      list.innerHTML = '<li class="census-search__empty" role="status">No matching products. Try a different name, or search to filter the table.</li>';
      input.setAttribute("aria-expanded", "true");
      active = -1;
      return;
    }
    list.hidden = false;
    input.setAttribute("aria-expanded", "true");
    list.innerHTML = items
      .map(function (item, i) {
        var hint = item.hint ? '<span class="census-search__option-hint">' + escapeHtml(item.hint) + "</span>" : "";
        return (
          '<li role="option" id="census-search-opt-' +
          i +
          '" class="census-search__option" data-href="' +
          escapeAttr(item.href) +
          '" aria-selected="false"><span class="census-search__option-title">' +
          highlight(item.title) +
          "</span>" +
          hint +
          "</li>"
        );
      })
      .join("");
    setActive(0, false);
  }

  function escapeHtml(value) {
    return String(value || "")
      .replace(/&/g, "&amp;")
      .replace(/</g, "&lt;")
      .replace(/>/g, "&gt;")
      .replace(/"/g, "&quot;");
  }

  function escapeAttr(value) {
    return escapeHtml(value).replace(/'/g, "&#39;");
  }

  function search(q) {
    query = q;
    abort && abort.abort();
    abort = new AbortController();
    fetch(url + (url.indexOf("?") >= 0 ? "&" : "?") + "q=" + encodeURIComponent(q), {
      credentials: "same-origin",
      signal: abort.signal,
      headers: { Accept: "application/json" }
    })
      .then(function (res) {
        if (!res.ok) throw new Error("suggest failed");
        return res.json();
      })
      .then(function (data) {
        render(data.results || []);
      })
      .catch(function (err) {
        if (err.name !== "AbortError") hide();
      });
  }

  function requestSearch() {
    clearTimeout(debounce);
    debounce = setTimeout(function () {
      search((input.value || "").trim());
    }, DEBOUNCE_MS);
  }

  input.setAttribute("role", "combobox");
  input.setAttribute("aria-autocomplete", "list");
  input.setAttribute("aria-expanded", "false");
  input.setAttribute("aria-controls", list.id);
  input.setAttribute("aria-haspopup", "listbox");

  input.addEventListener("input", requestSearch);
  input.addEventListener("focus", function () {
    requestSearch();
  });

  input.addEventListener("keydown", function (e) {
    if (e.key === "Escape") {
      hide();
      return;
    }
    if (e.key === "ArrowDown") {
      e.preventDefault();
      if (list.hidden) {
        requestSearch();
        return;
      }
      setActive(Math.min(active + 1, items.length - 1), true);
      return;
    }
    if (e.key === "ArrowUp") {
      e.preventDefault();
      if (list.hidden) return;
      setActive(Math.max(active - 1, 0), true);
      return;
    }
    if (e.key === "Enter" && !list.hidden && active >= 0 && items[active] && items[active].href) {
      e.preventDefault();
      go(items[active].href);
    }
  });

  list.addEventListener("mousedown", function (e) {
    var option = e.target.closest("[data-href]");
    if (!option) return;
    e.preventDefault();
    go(option.getAttribute("data-href"));
  });

  list.addEventListener("mouseover", function (e) {
    var option = e.target.closest("[data-href]");
    if (!option || !list.contains(option)) return;
    var options = [].slice.call(list.querySelectorAll("[data-href]"));
    setActive(options.indexOf(option), false);
  });

  document.addEventListener("click", function (e) {
    if (!root.contains(e.target)) hide();
  });

  if (form) {
    form.addEventListener("submit", function () {
      hide();
    });
  }
})();
