(function () {
  function openViewAsModal() {
    var modal = document.getElementById('view-as-modal');
    if (!modal || typeof modal.showModal !== 'function') return;
    modal.showModal();
  }

  document.addEventListener('click', function (e) {
    var trigger = e.target.closest('[data-view-as-open]');
    if (!trigger) return;
    e.preventDefault();
    openViewAsModal();
  });
})();
