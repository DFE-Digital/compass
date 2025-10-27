using Microsoft.AspNetCore.Mvc;
using Compass.Services;

namespace Compass.ViewComponents;

public class BusinessAreasNavigationViewComponent : ViewComponent
{
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<BusinessAreasNavigationViewComponent> _logger;

    public BusinessAreasNavigationViewComponent(
        IProductsApiService productsApiService,
        ILogger<BusinessAreasNavigationViewComponent> logger)
    {
        _productsApiService = productsApiService;
        _logger = logger;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var businessAreas = await _productsApiService.GetBusinessAreasAsync();
            return View(businessAreas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading business areas for navigation");
            // Return empty list on error to prevent navigation from breaking
            return View(new List<string>());
        }
    }
}

