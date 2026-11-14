(function () {
  document.querySelectorAll("[data-enabling]").forEach(function (root) {
    var follow = root.querySelector("[data-enabling-follow]");
    function sync() {
      var yes = root.querySelector("input[value='yes']");
      if (follow) follow.hidden = !(yes && yes.checked);
    }
    root.querySelectorAll("input[name='lookupCode']").forEach(function (input) {
      input.addEventListener("change", sync);
    });
    sync();
  });

  document.querySelectorAll("[data-ss-picker]").forEach(function (root) {
    var input = root.querySelector("[data-ss-input]");
    var idField = root.querySelector("[data-ss-id]");
    var createField = root.querySelector("[data-ss-create]");
    var list = root.querySelector("[data-ss-list]");
    var searchUrl = root.getAttribute("data-ss-search");
    var raw = root.querySelector("[data-ss-options]");
    var options = [];
    try { options = JSON.parse(raw ? raw.textContent : "[]"); } catch (e) { options = []; }
    var searchTimer = 0;
    if (!input || !idField || !list) return;
    if (searchUrl) {
      input.addEventListener("input", function () {
        idField.value = "";
        window.clearTimeout(searchTimer);
        var query = input.value.trim().replace(/^\//, "").trim();
        if (query.length < 2) {
          list.innerHTML = "";
          if (input.value.trim()) {
            var hint = document.createElement("li");
            hint.className = "ss-picker-empty";
            hint.textContent = "Type at least 2 letters of the product name.";
            list.appendChild(hint);
            list.hidden = false;
          } else {
            list.hidden = true;
          }
          return;
        }
        searchTimer = window.setTimeout(function () {
          var joiner = searchUrl.indexOf("?") >= 0 ? "&" : "?";
          fetch(searchUrl + joiner + "q=" + encodeURIComponent(query), { headers: { Accept: "application/json" } })
            .then(function (response) { return response.json(); })
            .then(function (payload) {
              var results = payload && payload.results ? payload.results : [];
              list.innerHTML = "";
              results.forEach(function (item) {
                var li = document.createElement("li");
                var button = document.createElement("button");
                button.type = "button";
                button.textContent = item.name;
                button.addEventListener("click", function () {
                  input.value = item.name;
                  idField.value = item.id;
                  list.hidden = true;
                });
                li.appendChild(button);
                list.appendChild(li);
              });
              if (results.length === 0) {
                var empty = document.createElement("li");
                empty.className = "ss-picker-empty";
                empty.textContent = "No products match.";
                list.appendChild(empty);
              }
              list.hidden = false;
            })
            .catch(function () { list.hidden = true; });
        }, 200);
      });
      document.addEventListener("click", function (event) {
        if (!root.contains(event.target)) list.hidden = true;
      });
      return;
    }
    function parseQuery(value) {
      var typed = value.trim();
      var browsing = typed.charAt(0) === "/";
      return { browsing: browsing, query: browsing ? typed.slice(1).trim() : typed };
    }
    function hide() { list.hidden = true; list.innerHTML = ""; }
    function show(items, query, listAll) {
      list.innerHTML = "";
      var visible = listAll ? items : items.slice(0, 12);
      visible.forEach(function (item) {
        var li = document.createElement("li");
        var button = document.createElement("button");
        button.type = "button";
        button.textContent = item.name;
        button.addEventListener("click", function () {
          input.value = item.name;
          idField.value = item.id;
          if (createField) createField.value = "";
          hide();
        });
        li.appendChild(button);
        list.appendChild(li);
      });
      if (listAll && items.length === 0) {
        var empty = document.createElement("li");
        empty.className = "ss-picker-empty";
        empty.textContent = "Nothing in this list yet. Type a name to add one.";
        list.appendChild(empty);
      }
      var exact = options.some(function (item) { return item.name.toLowerCase() === query.toLowerCase(); });
      if (query && !exact && createField) {
        var add = document.createElement("li");
        var addButton = document.createElement("button");
        addButton.type = "button";
        addButton.className = "is-add";
        addButton.textContent = "Add \"" + query + "\" to the list";
        addButton.addEventListener("click", function () {
          input.value = query;
          idField.value = "";
          createField.value = query;
          hide();
        });
        add.appendChild(addButton);
        list.appendChild(add);
      }
      list.hidden = list.children.length === 0;
    }
    input.addEventListener("input", function () {
      idField.value = "";
      if (createField) createField.value = "";
      var parsed = parseQuery(input.value);
      if (!parsed.browsing && !parsed.query) { hide(); return; }
      var matches = parsed.query
        ? options.filter(function (item) { return item.name.toLowerCase().indexOf(parsed.query.toLowerCase()) >= 0; })
        : options.slice();
      show(matches, parsed.query, parsed.browsing && !parsed.query);
    });
    input.addEventListener("focus", function () {
      if (input.value.trim()) input.dispatchEvent(new Event("input"));
    });
    document.addEventListener("click", function (event) {
      if (!root.contains(event.target)) hide();
    });
    var form = root.closest("form");
    if (form) {
      form.addEventListener("submit", function () {
        if (idField.value || (createField && createField.value)) return;
        var parsed = parseQuery(input.value);
        if (parsed.query && createField) createField.value = parsed.query;
      });
    }
  });
})();
