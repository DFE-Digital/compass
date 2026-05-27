(function () {
  'use strict';

  var bootEl = document.getElementById('raid-register-boot');
  if (!bootEl || !window.RaidRegisterDetail) return;

  try {
    var config = JSON.parse(bootEl.textContent || '{}');
    window.RaidRegisterDetail.init(config);
  } catch (err) {
    console.error('RAID register detail init failed', err);
  }
})();
