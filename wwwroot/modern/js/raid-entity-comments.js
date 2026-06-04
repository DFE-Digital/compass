(function () {
  'use strict';

  function readCsrfToken() {
    var tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
    return tokenEl ? tokenEl.value : '';
  }

  function formatWhen(iso) {
    if (!iso) return '';
    try {
      var d = new Date(iso);
      return d.toLocaleString('en-GB', {
        day: 'numeric',
        month: 'short',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch (e) {
      return iso;
    }
  }

  function authorName(comment) {
    if (comment.createdByUser && (comment.createdByUser.name || comment.createdByUser.email)) {
      return comment.createdByUser.name || comment.createdByUser.email;
    }
    return 'Unknown user';
  }

  function renderCommentItem(comment) {
    var item = document.createElement('div');
    item.className = 'raid-entity-comments-timeline__item';
    item.setAttribute('role', 'listitem');

    var meta = document.createElement('p');
    meta.className = 'raid-entity-comments-timeline__meta govuk-body-s govuk-!-margin-bottom-1';
    meta.textContent = authorName(comment) + ' · ' + formatWhen(comment.createdAt);

    var body = document.createElement('p');
    body.className = 'govuk-body govuk-!-margin-bottom-0';
    body.style.whiteSpace = 'pre-wrap';
    body.textContent = comment.commentText || '';

    item.appendChild(meta);
    item.appendChild(body);
    return item;
  }

  function setStatus(panel, message, isError) {
    var status = panel.querySelector('[data-raid-entity-comments-status]');
    if (!status) return;
    if (!message) {
      status.hidden = true;
      status.textContent = '';
      return;
    }
    status.hidden = false;
    status.textContent = message;
    status.className = isError
      ? 'govuk-body-s govuk-!-margin-top-2 govuk-!-margin-bottom-0 govuk-error-message'
      : 'govuk-body-s govuk-!-margin-top-2 govuk-!-margin-bottom-0';
  }

  function loadComments(panel) {
    var entityType = panel.getAttribute('data-entity-type');
    var entityId = panel.getAttribute('data-entity-id');
    var timeline = panel.querySelector('[id$="-timeline"]');
    if (!entityType || !entityId || !timeline) return;

    fetch('/api/comments/' + encodeURIComponent(entityType) + '/' + encodeURIComponent(entityId))
      .then(function (r) {
        if (!r.ok) throw new Error('Load failed');
        return r.json();
      })
      .then(function (comments) {
        timeline.innerHTML = '';
        if (!comments || comments.length === 0) {
          timeline.innerHTML = '<p class="govuk-body-s govuk-!-margin-bottom-0">No progress updates yet.</p>';
          return;
        }
        comments.forEach(function (c) {
          timeline.appendChild(renderCommentItem(c));
        });
      })
      .catch(function () {
        timeline.innerHTML = '<p class="govuk-body-s govuk-!-margin-bottom-0">Could not load progress updates.</p>';
      });
  }

  function addComment(panel) {
    var entityType = panel.getAttribute('data-entity-type');
    var entityId = parseInt(panel.getAttribute('data-entity-id') || '0', 10);
    var input = panel.querySelector('[data-raid-entity-comments-input]');
    var addBtn = panel.querySelector('[data-raid-entity-comments-add]');
    var timeline = panel.querySelector('[id$="-timeline"]');
    if (!entityType || !entityId || !input || !addBtn || !timeline) return;

    var text = input.value.trim();
    if (!text) {
      input.focus();
      setStatus(panel, 'Enter some text before adding an update.', true);
      return;
    }

    addBtn.disabled = true;
    setStatus(panel, '', false);

    fetch('/api/comments', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'RequestVerificationToken': readCsrfToken()
      },
      body: JSON.stringify({
        entityType: entityType,
        entityId: entityId,
        commentText: text
      })
    })
      .then(function (r) {
        if (!r.ok) throw new Error('Save failed');
        return r.json();
      })
      .then(function (comment) {
        input.value = '';
        var empty = timeline.querySelector('p');
        if (empty && !timeline.querySelector('.raid-entity-comments-timeline__item')) {
          timeline.innerHTML = '';
        }
        timeline.insertBefore(renderCommentItem(comment), timeline.firstChild);
        setStatus(panel, 'Update added.', false);
      })
      .catch(function () {
        setStatus(panel, 'Could not save update. Try again.', true);
      })
      .finally(function () {
        addBtn.disabled = false;
      });
  }

  function initPanel(panel) {
    loadComments(panel);
    var addBtn = panel.querySelector('[data-raid-entity-comments-add]');
    if (addBtn) {
      addBtn.addEventListener('click', function () {
        addComment(panel);
      });
    }
  }

  document.querySelectorAll('[data-raid-entity-comments]').forEach(initPanel);
})();
