(() => {
    const ready = (fn) => {
        if (document.readyState !== 'loading') {
            fn();
        } else {
            document.addEventListener('DOMContentLoaded', fn);
        }
    };

    ready(() => {
        const gridElement = document.getElementById('dashboardGrid');
        if (!gridElement || typeof GridStack === 'undefined') {
            return;
        }

        const tokenField = document.querySelector('input[name="__RequestVerificationToken"]');
        const antiForgeryToken = tokenField ? tokenField.value : '';

        let blockDefinitions = [];
        try {
            blockDefinitions = JSON.parse(gridElement.dataset.blockDefinitions || '[]');
        } catch {
            blockDefinitions = [];
        }

        const grid = GridStack.init({
            column: 12,
            cellHeight: 140,
            float: false,
            disableOneColumnMode: true,
            acceptWidgets: true,
            dragIn: '.dashboard-block-card',
            dragInOptions: {
                revert: 'invalid',
                appendTo: 'body',
                helper: 'clone',
                scroll: false
            },
            resizable: {
                handles: 'se,sw'
            }
        }, gridElement);

        let isEditMode = false;
        let isProgrammaticAdd = false;

        const generateId = () => {
            if (typeof crypto !== 'undefined' && crypto.randomUUID) {
                return crypto.randomUUID();
            }
            return 'block-' + Math.random().toString(36).substring(2, 9);
        };

        const collectLayout = () => {
            const nodes = (grid.engine && grid.engine.nodes) ? grid.engine.nodes : [];
            return nodes
                .map(node => {
                    const el = node.el;
                    const blockId = el?.dataset.blockId || generateId();
                    if (el) {
                        el.dataset.blockId = blockId;
                    }
                    return {
                        id: blockId,
                        type: el?.dataset.blockType || '',
                        x: node.x,
                        y: node.y,
                        width: node.w,
                        height: node.h,
                        settings: {}
                    };
                })
                .filter(block => block.type);
        };

        let saveTimeout;
        const persistLayout = () => {
            clearTimeout(saveTimeout);
            saveTimeout = window.setTimeout(() => {
                const blocks = collectLayout();
                fetch('/Home/SaveDashboardLayout', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'RequestVerificationToken': antiForgeryToken
                    },
                    body: JSON.stringify({ blocks })
                }).then(response => {
                    if (!response.ok) {
                        throw new Error('Failed to save layout');
                    }
                }).catch(err => {
                    console.error(err);
                });
            }, 300);
        };

        const toggleBtn = document.getElementById('toggleLayoutMode');
        const addBtn = document.getElementById('addBlockBtn');
        const palette = document.getElementById('blockPalette');
        const emptyStateAddBtn = document.getElementById('emptyStateAddBlock');
        const modeStatus = document.getElementById('dashboardModeStatus');
        const emptyState = document.getElementById('dashboardEmptyState');
        const gridShell = document.getElementById('dashboardGridShell');

        const setEditMode = (state) => {
            isEditMode = state;
            grid.setStatic(!state);
            grid.enableMove(state);
            grid.enableResize(state);
            gridElement.classList.toggle('is-editing', state);
            gridElement.dataset.editing = state ? 'true' : 'false';
            gridShell?.classList.toggle('is-edit-mode', state);
            if (toggleBtn) {
                toggleBtn.innerHTML = state
                    ? '<i class="fas fa-check"></i> Done editing'
                    : '<i class="fas fa-arrows-alt"></i> Enter edit mode';
                toggleBtn.classList.toggle('btn-primary', state);
                toggleBtn.classList.toggle('btn-outline-secondary', !state);
            }
            if (modeStatus) {
                modeStatus.innerHTML = state
                    ? '<i class="fas fa-pen mr-1"></i>Edit mode'
                    : '<i class="fas fa-lock mr-1"></i>View only';
                modeStatus.classList.toggle('status-pill--active', state);
            }
            if (!state && palette) {
                palette.classList.remove('show');
            }
            if (!state) {
                persistLayout();
            }
        };

        const updateEmptyStateVisibility = () => {
            if (!emptyState) {
                return;
            }
            const hasBlocks = gridElement.querySelectorAll('.grid-stack-item').length > 0;
            emptyState.classList.toggle('d-none', hasBlocks);
        };

        setEditMode(false);
        updateEmptyStateVisibility();

        toggleBtn?.addEventListener('click', () => {
            setEditMode(!isEditMode);
        });

        addBtn?.addEventListener('click', () => {
            if (!isEditMode) {
                setEditMode(true);
            }
            if (!palette) {
                return;
            }
            palette.classList.toggle('show');
        });

        emptyStateAddBtn?.addEventListener('click', () => {
            if (!isEditMode) {
                setEditMode(true);
            }
            palette?.classList.add('show');
        });

        const loadBlockContent = (blockId, blockType, target) => {
            if (!target) {
                return;
            }

            target.innerHTML = `
                <div class="dashboard-block placeholder d-flex flex-column align-items-center justify-content-center h-100 text-muted">
                    <p class="mb-1">Loading block…</p>
                </div>`;

            fetch('/Home/RenderDashboardBlock', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': antiForgeryToken
                },
                body: JSON.stringify({ blockId, blockType })
            })
                .then(response => {
                    if (!response.ok) {
                        throw new Error('Failed to render block');
                    }
                    return response.text();
                })
                .then(html => {
                    target.innerHTML = html;
                    if (window.DashboardBlocks && typeof window.DashboardBlocks.initCharts === 'function') {
                        window.DashboardBlocks.initCharts(target);
                    }
                })
                .catch(err => {
                    console.error(err);
                    target.innerHTML = `
                        <div class="alert alert-warning mb-0">
                            Could not load this block. Please try again.
                        </div>`;
                });
        };

        const createWidgetElement = (definition, blockId) => {
            const wrapper = document.createElement('div');
            wrapper.classList.add('grid-stack-item');
            wrapper.dataset.blockId = blockId;
            wrapper.dataset.blockType = definition.type;

            const content = document.createElement('div');
            content.classList.add('grid-stack-item-content');

            const removeButton = document.createElement('button');
            removeButton.type = 'button';
            removeButton.className = 'btn btn-link text-danger btn-sm remove-block';
            removeButton.setAttribute('data-remove-block', blockId);
            removeButton.title = 'Remove block';
            removeButton.innerHTML = '<i class="fas fa-times"></i>';

            const blockHost = document.createElement('div');

            content.appendChild(removeButton);
            content.appendChild(blockHost);
            wrapper.appendChild(content);

            return { wrapper, blockHost };
        };

        const addBlockOfType = (type, coords) => {
            const definition = blockDefinitions.find(d => d.type === type);
            if (!definition) {
                return;
            }

            const blockId = generateId();
            const { wrapper, blockHost } = createWidgetElement(definition, blockId);

            const widgetOptions = {
                width: definition.defaultWidth,
                height: definition.defaultHeight,
                minWidth: definition.minWidth,
                minHeight: definition.minHeight,
                x: 0,
                y: 0
            };

            if (coords && Number.isFinite(coords.x) && Number.isFinite(coords.y)) {
                widgetOptions.x = Math.max(0, Math.min(12 - widgetOptions.width, coords.x));
                widgetOptions.y = Math.max(0, coords.y);
            }

            isProgrammaticAdd = true;
            grid.addWidget(wrapper, widgetOptions);
            isProgrammaticAdd = false;
            loadBlockContent(blockId, type, blockHost);
            updateEmptyStateVisibility();
            persistLayout();
        };

        const handleAddedItems = (items) => {
            if (!items || isProgrammaticAdd) {
                return;
            }
            items.forEach(item => {
                const el = item.el;
                if (!el) {
                    return;
                }
                const paletteType = el.dataset.blockType;
                const fromPalette = el.classList.contains('dashboard-block-card');
                if (!fromPalette || !paletteType) {
                    return;
                }
                grid.removeWidget(el);
                addBlockOfType(paletteType, { x: item.x ?? 0, y: item.y ?? 0 });
            });
        };

        const queuePersist = () => {
            if (!isEditMode) {
                return;
            }
            persistLayout();
        };

        grid.on('change', () => {
            updateEmptyStateVisibility();
            queuePersist();
        });

        grid.on('added', (event, items) => {
            handleAddedItems(items);
            updateEmptyStateVisibility();
            queuePersist();
        });

        grid.on('removed', () => {
            updateEmptyStateVisibility();
            queuePersist();
        });

        gridElement.addEventListener('click', (event) => {
            const removeBtn = event.target.closest('[data-remove-block]');
            if (!removeBtn || !isEditMode) {
                return;
            }
            event.preventDefault();
            const id = removeBtn.getAttribute('data-remove-block');
            const widget = gridElement.querySelector(`.grid-stack-item[data-block-id="${id}"]`);
            if (widget) {
                grid.removeWidget(widget);
                updateEmptyStateVisibility();
                persistLayout();
            }
        });

        const bindAddButtons = () => {
            document.querySelectorAll('[data-add-block]').forEach(button => {
                button.addEventListener('dragstart', (event) => {
                    event.preventDefault();
                });

                button.addEventListener('click', () => {
                    if (!isEditMode) {
                        setEditMode(true);
                    }

                    const type = button.getAttribute('data-add-block');
                    addBlockOfType(type);
                    palette?.classList.remove('show');
                });
            });
        };

        const bindPaletteDrag = () => {
            document.querySelectorAll('.dashboard-block-card').forEach(card => {
                card.addEventListener('pointerdown', () => {
                    if (!isEditMode) {
                        setEditMode(true);
                    }
                    card.classList.add('is-dragging');
                });

                card.addEventListener('pointerup', () => {
                    card.classList.remove('is-dragging');
                });

                card.addEventListener('pointerleave', () => {
                    card.classList.remove('is-dragging');
                });
            });
        };

        bindAddButtons();
        bindPaletteDrag();

        gridElement.querySelectorAll('.grid-stack-item').forEach(item => {
            const blockHost = item.querySelector('.grid-stack-item-content > div:not(.remove-block)');
            const blockId = item.dataset.blockId;
            const blockType = item.dataset.blockType;
            if (blockHost && blockId && blockType && blockHost.children.length === 0) {
                loadBlockContent(blockId, blockType, blockHost);
            }
        });
    });
})();

