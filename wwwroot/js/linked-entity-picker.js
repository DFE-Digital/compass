(() => {
    function escapeHtml(value) {
        if (typeof value !== 'string') {
            return '';
        }

        return value.replace(/[&<>"']/g, match => ({
            '&': '&amp;',
            '<': '&lt;',
            '>': '&gt;',
            '"': '&quot;',
            "'": '&#39;'
        }[match]));
    }

    function formatResult(item) {
        if (!item.id) {
            return escapeHtml(item.text || '');
        }

        const badge = item.isCurrentProject
            ? '<span class="badge badge-primary badge-pill ml-2">This project</span>'
            : '';

        const title = `<div class="linked-entity-result__title">${escapeHtml(item.text)}${badge}</div>`;
        const description = item.description
            ? `<div class="linked-entity-result__meta">${escapeHtml(item.description)}</div>`
            : '';

        return `<div class="linked-entity-result">${title}${description}</div>`;
    }

    function formatSelection(item) {
        return item.text || item.id || '';
    }

    function initLinkedEntitySelect(select) {
        if (!window.$ || !window.$.fn.select2) {
            return;
        }

        const searchUrl = select.dataset.searchUrl;
        if (!searchUrl) {
            return;
        }

        const projectId = select.dataset.projectId;
        const placeholder = select.dataset.placeholder || 'Search';
        const $select = window.$(select);

        $select.select2({
            theme: 'bootstrap4',
            placeholder,
            minimumInputLength: 0,
            ajax: {
                delay: 250,
                transport: function(params, success, failure) {
                    const request = window.$.ajax(params);
                    request.then(success);
                    request.fail(failure);
                    return request;
                },
                url: searchUrl,
                data: params => ({
                    projectId,
                    term: params.term || '',
                    limit: 15
                }),
                processResults: data => ({
                    results: (data?.results || []).map(item => ({
                        id: item.id,
                        text: item.text,
                        description: item.description,
                        isCurrentProject: item.isCurrentProject
                    }))
                })
            },
            templateResult: formatResult,
            templateSelection: formatSelection,
            escapeMarkup: markup => markup
        });
    }

    document.addEventListener('DOMContentLoaded', () => {
        document
            .querySelectorAll('.js-linked-entity-select')
            .forEach(initLinkedEntitySelect);
    });
})();

