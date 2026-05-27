(function () {
  'use strict';

  var list = document.getElementById('raid-admin-col-order-list');
  if (!list) return;

  function moveItem(item, direction) {
    if (!item) return;
    if (direction < 0 && item.previousElementSibling) {
      list.insertBefore(item, item.previousElementSibling);
    } else if (direction > 0 && item.nextElementSibling) {
      list.insertBefore(item.nextElementSibling, item);
    }
  }

  list.addEventListener('click', function (e) {
    var up = e.target.closest('.raid-admin-col-move-up');
    var down = e.target.closest('.raid-admin-col-move-down');
    if (!up && !down) return;
    e.preventDefault();
    var item = e.target.closest('.raid-admin-col-order-item');
    moveItem(item, up ? -1 : 1);
  });
})();
