using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Compass.Services;

namespace Compass.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ApiController : ControllerBase
{
    private readonly ILogger<ApiController> _logger;
    private readonly ICmsApiService _cmsApiService;

    public ApiController(ILogger<ApiController> logger, ICmsApiService cmsApiService)
    {
        _logger = logger;
        _cmsApiService = cmsApiService;
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }

    // Add your API endpoints here as you build out the application
    // Example:
    // [HttpGet("products")]
    // public async Task<IActionResult> GetProducts()
    // {
    //     var products = await _cmsApiService.GetAsync<ApiCollectionResponse<Product>>("products");
    //     return Ok(products);
    // }
}

