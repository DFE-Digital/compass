(function () {
  const banner = document.getElementById('census-unsaved');
  const form = document.getElementById('census-question');
  if (!banner) return;

  function showUnsaved() {
    banner.hidden = false;
    banner.removeAttribute('hidden');
  }

  window.censusMarkUnsaved = showUnsaved;

  form?.addEventListener('input', event => {
    if (event.target.closest('[data-dfe-modal], dialog')) return;
    showUnsaved();
  });
  form?.addEventListener('change', event => {
    if (event.target.closest('[data-dfe-modal], dialog')) return;
    showUnsaved();
  });
})();
