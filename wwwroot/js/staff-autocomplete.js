console.log('Staff autocomplete JS file loaded directly');

// Wait for jQuery to be available
(function() {
    function initStaffAutocomplete() {
        if (typeof jQuery === 'undefined') {
            setTimeout(initStaffAutocomplete, 50);
            return;
        }
        
        var $ = jQuery;
        
        $(document).ready(function() {
            console.log('Staff autocomplete: jQuery ready');
            console.log('Found ' + $('.staff-autocomplete').length + ' autocomplete fields');
            
            // Initialize autocomplete for staff input fields
            $('.staff-autocomplete').each(function() {
                var $input = $(this);
                var $hiddenInput = $input.siblings('input[type="hidden"]');
                var timeoutId;
                var $resultsContainer = $('<div class="autocomplete-results"></div>');
                $input.after($resultsContainer);
                
                console.log('Initialized autocomplete for field:', $input.attr('id'));
                
                // Handle input with debounce
                $input.on('input', function() {
                    clearTimeout(timeoutId);
                    var searchTerm = $(this).val();
                    
                    console.log('Input detected:', searchTerm);
                    
                    if (searchTerm.length < 2) {
                        $resultsContainer.hide().empty();
                        return;
                    }
                    
                    console.log('Searching for:', searchTerm);
                    
                    timeoutId = setTimeout(function() {
                        $.ajax({
                            url: '/api/staff/search',
                            data: { q: searchTerm },
                            dataType: 'json',
                            success: function(data) {
                                console.log('API response:', data);
                                displayResults(data.results);
                            },
                            error: function(xhr, status, error) {
                                console.error('Failed to search staff:', status, error);
                            }
                        });
                    }, 250);
                });
                
                function displayResults(results) {
                    $resultsContainer.empty();
                    
                    if (!results || results.length === 0) {
                        $resultsContainer.html('<div class="autocomplete-item no-results">No staff found</div>');
                        $resultsContainer.show();
                        return;
                    }
                    
                    results.forEach(function(staff) {
                        var $item = $('<div class="autocomplete-item"></div>');
                        $item.html(
                            '<div class="autocomplete-name">' + staff.displayName + '</div>' +
                            '<div class="autocomplete-details">' + staff.email + 
                            (staff.jobTitle ? ' • ' + staff.jobTitle : '') + '</div>'
                        );
                        
                        $item.on('click', function() {
                            $input.val(staff.displayName);
                            $hiddenInput.val(staff.id);
                            $resultsContainer.hide().empty();
                        });
                        
                        $resultsContainer.append($item);
                    });
                    
                    $resultsContainer.show();
                }
                
                // Hide results when clicking outside
                $(document).on('click', function(e) {
                    if (!$(e.target).closest('.staff-autocomplete').length && 
                        !$(e.target).closest('.autocomplete-results').length) {
                        $resultsContainer.hide();
                    }
                });
                
                // Keyboard navigation
                $input.on('keydown', function(e) {
                    var $items = $resultsContainer.find('.autocomplete-item');
                    var $selected = $items.filter('.selected');
                    
                    if (e.key === 'ArrowDown') {
                        e.preventDefault();
                        if ($selected.length === 0) {
                            $items.first().addClass('selected');
                        } else {
                            $selected.removeClass('selected');
                            var $next = $selected.next('.autocomplete-item');
                            if ($next.length) {
                                $next.addClass('selected');
                            } else {
                                $items.first().addClass('selected');
                            }
                        }
                    } else if (e.key === 'ArrowUp') {
                        e.preventDefault();
                        if ($selected.length === 0) {
                            $items.last().addClass('selected');
                        } else {
                            $selected.removeClass('selected');
                            var $prev = $selected.prev('.autocomplete-item');
                            if ($prev.length) {
                                $prev.addClass('selected');
                            } else {
                                $items.last().addClass('selected');
                            }
                        }
                    } else if (e.key === 'Enter') {
                        if ($selected.length) {
                            e.preventDefault();
                            $selected.click();
                        }
                    } else if (e.key === 'Escape') {
                        $resultsContainer.hide();
                    }
                });
            });
        });
    }
    
    initStaffAutocomplete();
})();
