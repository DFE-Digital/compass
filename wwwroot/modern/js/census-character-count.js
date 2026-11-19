(function () {
  function format(n) {
    return n.toLocaleString('en-GB');
  }

  function bind(wrapper) {
    if (wrapper.getAttribute('data-census-count-bound') === 'true') return;
    if (wrapper.querySelector('.govuk-character-count__status')) return;

    var max = parseInt(wrapper.getAttribute('data-maxlength'), 10);
    var field = wrapper.querySelector('.govuk-js-character-count');
    var msg = wrapper.querySelector('.govuk-character-count__message');
    if (!field || !msg || !(max > 0)) return;

    wrapper.setAttribute('data-census-count-bound', 'true');

    function update() {
      var remaining = max - (field.value || '').length;
      msg.classList.remove('govuk-visually-hidden');
      if (remaining < 0) {
        var over = Math.abs(remaining);
        msg.textContent = 'You have ' + format(over) + ' character' + (over === 1 ? '' : 's') + ' too many';
        return;
      }
      msg.textContent = 'You have ' + format(remaining) + ' character' + (remaining === 1 ? '' : 's') + ' remaining';
    }

    field.addEventListener('input', update);
    field.addEventListener('change', update);
    update();
  }

  function bindAll() {
    document.querySelectorAll('.govuk-character-count[data-maxlength]').forEach(bind);
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
      setTimeout(bindAll, 0);
    });
  } else {
    setTimeout(bindAll, 0);
  }
  window.addEventListener('load', bindAll);
})();
