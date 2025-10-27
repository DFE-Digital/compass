using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Compass.Services;

namespace Compass.Controllers.Api;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductsApiService _productsApiService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductsApiService productsApiService, ILogger<ProductsController> logger)
    {
        _productsApiService = productsApiService;
        _logger = logger;
    }

    // GET: api/products/search?q=searchterm
    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        try
        {
            var searchTerm = (q ?? "").Trim();
            _logger.LogInformation("Searching products with term: {SearchTerm}", searchTerm);

            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return Ok(new { results = new List<object>() });
            }

            // Get all products from CMS
            var allProducts = await _productsApiService.GetProductsAsync(null);
            
            // Filter products based on search term (search in title, FIPS ID, and phase)
            var filteredProducts = allProducts
                .Where(p => !string.IsNullOrEmpty(p.FipsId) &&
                           (p.Title?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true ||
                            p.FipsId.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                            p.Phase?.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) == true))
                .Take(20)
                .Select(p => new
                {
                    fipsId = p.FipsId,
                    title = p.Title,
                    phase = p.Phase ?? "Not specified",
                    text = $"{p.Title} ({p.FipsId})"
                })
                .ToList();

            _logger.LogInformation("Found {Count} matching products", filteredProducts.Count);

            return Ok(new { results = filteredProducts });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products");
            return StatusCode(500, new { error = "An error occurred while searching products" });
        }
    }
}

