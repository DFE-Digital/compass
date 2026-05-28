(function () {
  'use strict';

  var list = document.getElementById('raid-admin-col-order-list');
  if (!list) return;

  var dragSource = null;

  function moveItem(item, direction) {
    if (!item) return;
    if (direction < 0 && item.previousElementSibling) {
      list.insertBefore(item, item.previousElementSibling);
    } else if (direction > 0 && item.nextElementSibling) {
      list.insertBefore(item.nextElementSibling, item);
    }
  }

  function colKey(item) {
    return item && item.getAttribute('data-col-key');
  }

  list.addEventListener('click', function (e) {
    var up = e.target.closest('.raid-admin-col-move-up');
    var down = e.target.closest('.raid-admin-col-move-down');
    if (!up && !down) return;
    e.preventDefault();
    var item = e.target.closest('.raid-admin-col-order-item');
    moveItem(item, up ? -1 : 1);
  });

  list.addEventListener('dragstart', function (e) {
    if (e.target.closest('.raid-admin-col-move-up, .raid-admin-col-move-down')) {
      e.preventDefault();
      return;
    }
    var item = e.target.closest('.raid-admin-col-order-item');
    if (!item) return;
    dragSource = item;
    item.classList.add('raid-admin-col-order-item--dragging');
    e.dataTransfer.setData('text/plain', colKey(item) || '');
    e.dataTransfer.effectAllowed = 'move';
  });

  list.addEventListener('dragend', function () {
    dragSource = null;
    list.querySelectorAll('.raid-admin-col-order-item--dragging').forEach(function (el) {
      el.classList.remove('raid-admin-col-order-item--dragging');
    });
    list.querySelectorAll('.raid-admin-col-order-item--drag-over').forEach(function (el) {
      el.classList.remove('raid-admin-col-order-item--drag-over');
    });
  });

  list.addEventListener('dragover', function (e) {
    var item = e.target.closest('.raid-admin-col-order-item');
    if (!item || item === dragSource) return;
    e.preventDefault();
    e.dataTransfer.dropEffect = 'move';
    list.querySelectorAll('.raid-admin-col-order-item--drag-over').forEach(function (el) {
      el.classList.remove('raid-admin-col-order-item--drag-over');
    });
    item.classList.add('raid-admin-col-order-item--drag-over');
  });

  list.addEventListener('dragleave', function (e) {
    var item = e.target.closest('.raid-admin-col-order-item');
    if (item) item.classList.remove('raid-admin-col-order-item--drag-over');
  });

  list.addEventListener('drop', function (e) {
    e.preventDefault();
    var target = e.target.closest('.raid-admin-col-order-item');
    if (!target || !dragSource || target === dragSource) return;
    list.insertBefore(dragSource, target);
    target.classList.remove('raid-admin-col-order-item--drag-over');
  });
})();
