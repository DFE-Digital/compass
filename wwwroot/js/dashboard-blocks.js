(() => {
    const initCharts = (root = document) => {
        if (typeof Chart === 'undefined' || !root) {
            return;
        }

        root.querySelectorAll('canvas[data-chart-config]').forEach(canvas => {
            try {
                const ctx = canvas.getContext('2d');
                const config = JSON.parse(canvas.dataset.chartConfig || '{}');
                if (canvas._chartInstance) {
                    canvas._chartInstance.destroy();
                }
                canvas._chartInstance = new Chart(ctx, config);
            } catch (err) {
                console.error('Failed to initialise chart', err);
            }
        });
    };

    window.DashboardBlocks = window.DashboardBlocks || {};
    window.DashboardBlocks.initCharts = initCharts;

    if (document.readyState !== 'loading') {
        initCharts();
    } else {
        document.addEventListener('DOMContentLoaded', () => initCharts());
    }
})();

