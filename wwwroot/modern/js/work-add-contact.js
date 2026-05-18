/**
 * Add contact — show custom role field when "Other (custom role)" is selected (role type id 5).
 */
(function () {
  'use strict';

  var CUSTOM_ROLE_TYPE_ID = 5;

  function bindAddContactRoleToggle() {
    var root = document.getElementById('sc-work-addcontact');
    var sel = document.getElementById('addcontact-role-type');
    var wrap = document.getElementById('addcontact-custom-role-wrap');
    var customRole = document.getElementById('CustomRole');
    if (!root || !sel || !wrap) return;

    function isCustomRole() {
      var n = parseInt(sel.value, 10);
      return n === CUSTOM_ROLE_TYPE_ID;
    }

    function toggle() {
      var show = isCustomRole();
      wrap.classList.toggle('addcontact-custom-role-wrap--hidden', !show);
      if (customRole) {
        customRole.required = show;
        if (!show) {
          customRole.setCustomValidity('');
        }
      }
    }

    sel.addEventListener('change', toggle);
    toggle();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', bindAddContactRoleToggle);
  } else {
    bindAddContactRoleToggle();
  }
})();
