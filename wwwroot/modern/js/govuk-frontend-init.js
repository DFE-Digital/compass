import { initAll } from '/modern/js/govuk-frontend-6.1.0.min.js';

initAll();

if (typeof window.DfeFrontend !== 'undefined' && typeof window.DfeFrontend.initAll === 'function') {
  window.DfeFrontend.initAll();
}
