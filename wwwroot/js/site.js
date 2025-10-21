//
// For guidance on how to add JavaScript see:
// https://prototype-kit.service.gov.uk/docs/adding-css-javascript-and-images
//

// Application Insights helper functions
function trackEvent(eventName, properties) {
    if (window.appInsights) {
        window.appInsights.trackEvent(eventName, properties);
    }
}

function trackPageView(pageName, url) {
    if (window.appInsights) {
        window.appInsights.trackPageView(pageName, url);
    }
}

// Track page load
document.addEventListener('DOMContentLoaded', function() {
    // Track page view
    trackPageView(document.title, window.location.href);
    
    // Track user interactions
    trackEvent('PageLoaded', {
        page: window.location.pathname,
        referrer: document.referrer,
        userAgent: navigator.userAgent,
        timestamp: new Date().toISOString()
    });
});

// Track search interactions
document.addEventListener('DOMContentLoaded', function() {
    const searchForms = document.querySelectorAll('form[action*="search"], form[action*="Search"]');
    searchForms.forEach(function(form) {
        form.addEventListener('submit', function(e) {
            const searchInput = form.querySelector('input[type="search"], input[name*="search"], input[name*="Search"]');
            if (searchInput && searchInput.value.trim()) {
                trackEvent('SearchPerformed', {
                    searchTerm: searchInput.value.trim(),
                    page: window.location.pathname,
                    timestamp: new Date().toISOString()
                });
            }
        });
    });
});

// Track product clicks
document.addEventListener('DOMContentLoaded', function() {
    const productLinks = document.querySelectorAll('a[href*="/products/"], a[href*="/product/"]');
    productLinks.forEach(function(link) {
        link.addEventListener('click', function(e) {
            trackEvent('ProductClicked', {
                productUrl: link.href,
                productText: link.textContent.trim(),
                page: window.location.pathname,
                timestamp: new Date().toISOString()
            });
        });
    });
});

// Track errors
window.addEventListener('error', function(e) {
    trackEvent('JavaScriptError', {
        errorMessage: e.message,
        errorSource: e.filename,
        errorLine: e.lineno,
        errorColumn: e.colno,
        page: window.location.pathname,
        timestamp: new Date().toISOString()
    });
});

// Track unhandled promise rejections
window.addEventListener('unhandledrejection', function(e) {
    trackEvent('UnhandledPromiseRejection', {
        errorMessage: e.reason ? e.reason.toString() : 'Unknown error',
        page: window.location.pathname,
        timestamp: new Date().toISOString()
    });
});
