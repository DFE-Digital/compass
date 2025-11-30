// Custom COMPASS JavaScript for AdminLTE

$(document).ready(function() {
    // Smooth page loading
    handleSmoothLoading();
    
    // Initialize AdminLTE components
    initializeAdminLTE();
    
    // Initialize custom components
    initializeCustomComponents();
    
    // Setup event handlers
    setupEventHandlers();
});

function handleSmoothLoading() {
    // Wait for all resources to load
    $(window).on('load', function() {
        // Hide preloader smoothly
        $('.preloader').addClass('fade-out');
        
        // Show body content
        $('body').addClass('loaded');
        
        // Remove preloader from DOM after transition
        setTimeout(function() {
            $('.preloader').remove();
        }, 300);
    });
}

function initializeAdminLTE() {
    // Initialize tooltips
    $('[data-toggle="tooltip"]').tooltip();
    
    // Initialize popovers
    $('[data-toggle="popover"]').popover();
    
    // Initialize smooth dropdown transitions for topnav
    initializeSmoothDropdowns();
    
    // DataTable initialization moved to individual pages
}

function initializeSmoothDropdowns() {
    // Handle smooth dropdown open transitions in navbar-gov
    $('.navbar-gov .dropdown').on('show.bs.dropdown', function() {
        const $menu = $(this).find('.dropdown-menu');
        const $dropdown = $(this);
        
        // Ensure menu is visible for transition
        $menu.css('display', 'block');
        
        // Set initial hidden state
        $menu.css({
            'opacity': '0',
            'transform': 'translateY(-8px) scale(0.98)',
            'visibility': 'visible'
        });
        
        // Force reflow to ensure initial state is rendered
        $menu[0].offsetHeight;
        
        // Trigger transition to visible state
        requestAnimationFrame(function() {
            $menu.css({
                'opacity': '1',
                'transform': 'translateY(0) scale(1)'
            });
        });
    });
    
    // Handle smooth dropdown close transitions
    $('.navbar-gov .dropdown').on('hide.bs.dropdown', function() {
        const $menu = $(this).find('.dropdown-menu');
        
        // Start closing transition
        $menu.css({
            'opacity': '0',
            'transform': 'translateY(-8px) scale(0.98)'
        });
    });
}

function initializeCustomComponents() {
    // Initialize dashboard components
    initializeDashboard();
    
    // Initialize form components
    initializeForms();
    
    // Initialize notification system
    initializeNotifications();
}

function initializeDashboard() {
    // Add click handlers for dashboard cards
    $('.dashboard-card').on('click', function() {
        const url = $(this).data('url');
        if (url) {
            window.location.href = url;
        }
    });
    
    // Initialize progress bars animation
    $('.progress-bar').each(function() {
        const $this = $(this);
        const width = $this.data('width') || $this.attr('aria-valuenow');
        $this.css('width', width + '%');
    });
    
    // Initialize counters
    $('.counter').each(function() {
        const $this = $(this);
        const target = parseInt($this.text());
        $this.text('0');
        $this.prop('Counter', 0).animate({
            Counter: target
        }, {
            duration: 2000,
            easing: 'swing',
            step: function(now) {
                $this.text(Math.ceil(now));
            }
        });
    });
}

function initializeForms() {
    // Form validation
    $('.needs-validation').on('submit', function(e) {
        if (!this.checkValidity()) {
            e.preventDefault();
            e.stopPropagation();
        }
        $(this).addClass('was-validated');
    });
    
    // Auto-save functionality for forms
    $('.auto-save').on('change', function() {
        const form = $(this).closest('form');
        if (form.length) {
            saveFormData(form);
        }
    });
    
    // File upload preview
    $('.file-input').on('change', function() {
        const file = this.files[0];
        if (file) {
            const reader = new FileReader();
            reader.onload = function(e) {
                $('.file-preview').attr('src', e.target.result).show();
            };
            reader.readAsDataURL(file);
        }
    });
}

function initializeNotifications() {
    // Alerts removed - no auto-hide functionality
    
    // Notification bell click handler
    $('.notification-bell').on('click', function() {
        loadNotifications();
    });
}

function setupEventHandlers() {
    // Sidebar toggle
    $('.sidebar-toggle').on('click', function() {
        $('body').toggleClass('sidebar-collapse');
    });
    
    // Fullscreen toggle
    $('.fullscreen-toggle').on('click', function() {
        toggleFullscreen();
    });
    
    // Print functionality
    $('.print-btn').on('click', function() {
        window.print();
    });
    
    // Export functionality
    $('.export-btn').on('click', function() {
        const format = $(this).data('format');
        exportData(format);
    });
    
    // Refresh data
    $('.refresh-btn').on('click', function() {
        const url = $(this).data('url');
        if (url) {
            refreshData(url);
        }
    });
}

function saveFormData(form) {
    const formData = new FormData(form[0]);
    const url = form.attr('action') || window.location.href;
    
    $.ajax({
        url: url,
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function(response) {
            showNotification('Form data saved successfully', 'success');
        },
        error: function() {
            showNotification('Error saving form data', 'error');
        }
    });
}

function loadNotifications() {
    // Load notifications via AJAX
    $.get('/api/notifications', function(data) {
        updateNotificationBadge(data.count);
        updateNotificationList(data.notifications);
    });
}

function updateNotificationBadge(count) {
    $('.notification-badge').text(count);
    if (count > 0) {
        $('.notification-badge').show();
    } else {
        $('.notification-badge').hide();
    }
}

function updateNotificationList(notifications) {
    const $list = $('.notification-list');
    $list.empty();
    
    notifications.forEach(function(notification) {
        const $item = $('<li class="dropdown-item">')
            .html('<i class="fas fa-' + notification.icon + ' mr-2"></i>' + notification.message)
            .append('<span class="float-right text-muted text-sm">' + notification.time + '</span>');
        $list.append($item);
    });
}

function showNotification(message, type) {
    // Alerts removed - no notification functionality
}

function toggleFullscreen() {
    if (!document.fullscreenElement) {
        document.documentElement.requestFullscreen();
    } else {
        if (document.exitFullscreen) {
            document.exitFullscreen();
        }
    }
}

function exportData(format) {
    const url = window.location.href + '/export?format=' + format;
    window.open(url, '_blank');
}

function refreshData(url) {
    $('.content').addClass('loading');
    
    $.get(url, function(data) {
        $('.content').html(data);
        $('.content').removeClass('loading');
        initializeCustomComponents();
    }).fail(function() {
        $('.content').removeClass('loading');
        showNotification('Error refreshing data', 'error');
    });
}

// Utility functions
function formatDate(date) {
    return new Date(date).toLocaleDateString('en-GB');
}

function formatCurrency(amount) {
    return new Intl.NumberFormat('en-GB', {
        style: 'currency',
        currency: 'GBP'
    }).format(amount);
}

function formatNumber(number) {
    return new Intl.NumberFormat('en-GB').format(number);
}

// Global error handler
window.addEventListener('error', function(e) {
    console.error('Global error:', e.error);
    showNotification('An unexpected error occurred', 'error');
});

// AJAX error handler
$(document).ajaxError(function(event, xhr, settings, thrownError) {
    if (xhr.status === 401) {
        window.location.href = '/Account/SignIn';
    } else if (xhr.status === 403) {
        showNotification('Access denied', 'error');
    } else if (xhr.status >= 500) {
        showNotification('Server error occurred', 'error');
    }
});
