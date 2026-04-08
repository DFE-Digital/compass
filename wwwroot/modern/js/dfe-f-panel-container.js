/**
 * DfE Manual Panel Container – toggle behaviour
 *
 * Handles expand/collapse for .dfe-f-panel-component panels that contain a
 * .dfe-f-panel-component__toggle button and a .dfe-f-panel-component__body element.
 *
 * Usage: included automatically when the page loads.
 * No configuration required – works entirely from data attributes
 * and ARIA on the markup.
 */
(function () {
  'use strict';

  /**
   * Initialise all panel containers on the page.
   * Safe to call multiple times (e.g. after dynamic content is added).
   */
  function initDfeMpc() {
    var toggles = document.querySelectorAll('.dfe-f-panel-component__toggle');

    toggles.forEach(function (toggle) {
      // Avoid double-binding
      if (toggle.dataset.dfeMpcInit) return;
      toggle.dataset.dfeMpcInit = 'true';

      toggle.addEventListener('click', function () {
        var card = toggle.closest('.dfe-f-panel-component');
        if (!card) return;

        var body = card.querySelector('.dfe-f-panel-component__body');
        if (!body) return;

        var isExpanded = toggle.getAttribute('aria-expanded') === 'true';

        if (isExpanded) {
          // Collapse
          toggle.setAttribute('aria-expanded', 'false');
          body.classList.remove('dfe-f-panel-component__body--visible');
          toggle.querySelector('.dfe-f-panel-component__toggle-label').textContent = 'Detail';
        } else {
          // Expand
          toggle.setAttribute('aria-expanded', 'true');
          body.classList.add('dfe-f-panel-component__body--visible');
          toggle.querySelector('.dfe-f-panel-component__toggle-label').textContent = 'Close';
        }
      });

      // Keyboard: Space and Enter already trigger click on <button>,
      // but ensure focus styles are visible (handled by CSS :focus).
    });
  }

  // Run on DOM ready
  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initDfeMpc);
  } else {
    initDfeMpc();
  }

  // Expose for use after dynamic content insertion
  window.dfeMpcInit = initDfeMpc;

  /**
   * Phase picker – set active phase card when a card is clicked.
   * On mobile, injects a collapsible toggle and shows phases as a list.
   */
  function initPhasePicker() {
    var chevronSvg = '<svg class="dfe-f-phase-picker__toggle-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 12 8" fill="none" aria-hidden="true"><path stroke="currentColor" stroke-width="2" stroke-linecap="round" d="M1 1l5 5 5-5"/></svg>';

    document.querySelectorAll('.dfe-f-phase-picker').forEach(function (picker) {
      if (!picker.id) {
        picker.id = 'dfe-f-phase-picker-' + Math.random().toString(36).slice(2, 9);
      }

      if (!picker.querySelector('.dfe-f-phase-picker__toggle')) {
        var toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'dfe-f-phase-picker__toggle';
        toggle.setAttribute('aria-expanded', 'false');
        toggle.setAttribute('aria-controls', picker.id);
        toggle.setAttribute('aria-haspopup', 'true');
        toggle.innerHTML = '<span class="dfe-f-phase-picker__toggle-text">' + getActivePhaseLabel(picker) + '</span>' + chevronSvg;
        picker.insertBefore(toggle, picker.firstChild);

        toggle.addEventListener('click', function () {
          var open = picker.classList.toggle('dfe-f-phase-picker--open');
          toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
        });
      }

      picker.querySelectorAll('.dfe-f-phase-card').forEach(function (card) {
        if (card.dataset.dfePhasePickerInit) return;
        card.dataset.dfePhasePickerInit = 'true';
        card.addEventListener('click', function () {
          picker.querySelectorAll('.dfe-f-phase-card').forEach(function (c) { c.classList.remove('dfe-f-phase-card--active'); });
          card.classList.add('dfe-f-phase-card--active');
          if (picker.classList.contains('dfe-f-phase-picker--open')) {
            picker.classList.remove('dfe-f-phase-picker--open');
            var t = picker.querySelector('.dfe-f-phase-picker__toggle');
            if (t) {
              t.setAttribute('aria-expanded', 'false');
              var label = t.querySelector('.dfe-f-phase-picker__toggle-text');
              if (label) label.textContent = getActivePhaseLabel(picker);
            }
          }
        });
      });
    });
  }

  function getActivePhaseLabel(picker) {
    var active = picker.querySelector('.dfe-f-phase-card--active .dfe-f-phase-card__name');
    return active ? active.textContent.trim() : 'Choose phase';
  }

  /**
   * Phase nav – on mobile injects a collapsible toggle and shows phases as a list.
   */
  function initPhaseNavCollapsible() {
    var chevronSvg = '<svg class="dfe-f-phase-nav__toggle-icon" xmlns="http://www.w3.org/2000/svg" viewBox="0 0 12 8" fill="none" aria-hidden="true"><path stroke="currentColor" stroke-width="2" stroke-linecap="round" d="M1 1l5 5 5-5"/></svg>';

    document.querySelectorAll('.dfe-f-phase-nav').forEach(function (nav) {
      var inner = nav.querySelector('.dfe-f-phase-nav__inner');
      var items = nav.querySelector('.dfe-f-phase-nav__items');
      if (!inner || !items) return;

      if (!items.id) {
        items.id = 'dfe-f-phase-nav-items-' + Math.random().toString(36).slice(2, 9);
      }

      if (!nav.querySelector('.dfe-f-phase-nav__toggle')) {
        var toggle = document.createElement('button');
        toggle.type = 'button';
        toggle.className = 'dfe-f-phase-nav__toggle';
        toggle.setAttribute('aria-expanded', 'false');
        toggle.setAttribute('aria-controls', items.id);
        toggle.setAttribute('aria-label', 'Show delivery phases');
        toggle.innerHTML = getCurrentPhaseNavLabel(nav) + chevronSvg;
        inner.insertBefore(toggle, inner.firstChild);

        toggle.addEventListener('click', function () {
          var open = nav.classList.toggle('dfe-f-phase-nav--open');
          toggle.setAttribute('aria-expanded', open ? 'true' : 'false');
          toggle.setAttribute('aria-label', open ? 'Hide delivery phases' : 'Show delivery phases');
        });
      }
    });
  }

  function getCurrentPhaseNavLabel(nav) {
    var current = nav.querySelector('.dfe-f-phase-nav__item--current');
    if (current) {
      var text = current.textContent.replace(/\s+/g, ' ').trim();
      return text || 'Phases';
    }
    return 'Phases';
  }

  /**
   * Task panel – expand/collapse .dfe-f-task-panel on trigger click.
   * Call window.dfeTaskPanelInit() after adding panels dynamically.
   */
  function initTaskPanels() {
    document.querySelectorAll('.dfe-f-task-panel').forEach(function (panel) {
      var trigger = panel.querySelector('.dfe-f-task-panel__trigger');
      if (!trigger || trigger.dataset.dfeTaskPanelInit) return;
      trigger.dataset.dfeTaskPanelInit = 'true';
      trigger.addEventListener('click', function () {
        var isOpen = panel.classList.toggle('is-open');
        trigger.setAttribute('aria-expanded', isOpen ? 'true' : 'false');
      });
    });
  }

  function initPhaseComponents() {
    initPhasePicker();
    initPhaseNavCollapsible();
    initTaskPanels();
  }

  function runWhenReady(fn) {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', fn);
    } else {
      fn();
    }
  }

  runWhenReady(initPhaseComponents);

  window.dfePhasePickerInit = initPhasePicker;
  window.dfePhaseNavCollapsibleInit = initPhaseNavCollapsible;
  window.dfeTaskPanelInit = initTaskPanels;
  window.dfePhaseComponentsInit = initPhaseComponents;
}());
