using Compass.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Compass.Services;

namespace Compass.Controllers;

public class UserSatisfactionSurveysController : Controller
{
    private readonly CompassDbContext _db;
    private readonly IProductsApiService _productsApiService;

    public UserSatisfactionSurveysController(CompassDbContext db, IProductsApiService productsApiService)
	{
		_db = db;
        _productsApiService = productsApiService;
	}

    public async Task<IActionResult> Index(string? search = null, int? minScore = null, int? maxScore = null)
	{
		ViewData["Title"] = "User satisfaction surveys";

        // Base sets from Products API (consistent with Accessibility)
        var cmsProducts = await _productsApiService.GetProductsAsync() ?? new List<Compass.Models.ProductDto>();
        var allProducts = cmsProducts
            .Where(p => !string.IsNullOrEmpty(p.FipsId))
            .Select(p => new { ProductFipsId = p.FipsId!, ProductTitle = p.Title })
            .ToList();

		var enrolledFips = await _db.Services.Select(s => s.FipsId).ToListAsync();

		// Compute enrolled with score aggregates
		var enrolled = await _db.Services
			.Select(s => new {
				FipsId = s.FipsId,
				DisplayName = s.DisplayName,
				Avg = _db.SurveyResponses
					.Where(r => r.SurveyInstance!.ServiceId == s.ServiceId)
					.Select(r => r.UssComputed)
					.DefaultIfEmpty()
					.Average(),
				Count = _db.SurveyResponses.Count(r => r.SurveyInstance!.ServiceId == s.ServiceId)
			})
			.ToListAsync();

		// Filter by search
		if (!string.IsNullOrWhiteSpace(search))
		{
			var term = search.Trim();
			enrolled = enrolled.Where(e => (e.DisplayName ?? "").Contains(term, StringComparison.OrdinalIgnoreCase)
				|| e.FipsId.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
			allProducts = allProducts.Where(p => p.ProductTitle.Contains(term, StringComparison.OrdinalIgnoreCase)
				|| p.ProductFipsId.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();
		}

		// Score range filter
		if (minScore.HasValue)
			enrolled = enrolled.Where(e => (double)e.Avg >= minScore.Value).ToList();
		if (maxScore.HasValue)
			enrolled = enrolled.Where(e => (double)e.Avg <= maxScore.Value).ToList();

		var nonEnrolled = allProducts
			.Where(p => !enrolledFips.Contains(p.ProductFipsId))
			.Select(p => new { FipsId = p.ProductFipsId, ProductName = p.ProductTitle })
			.ToList();

        // Summary stats (for cards)
        var totalResponses = await _db.SurveyResponses.CountAsync();
        var overallAvg = totalResponses > 0
            ? Math.Round(await _db.SurveyResponses.AverageAsync(r => (double)r.UssComputed), 1)
            : 0;
        ViewBag.SummaryStats = new {
            TotalEnrolledProducts = enrolled.Count,
            TotalProducts = allProducts.Count,
            TotalResponses = totalResponses,
            OverallAvg = overallAvg
        };

		ViewBag.Enrolled = enrolled;
		ViewBag.NonEnrolled = nonEnrolled;
		ViewBag.CurrentSearch = search;
		ViewBag.MinScore = minScore;
		ViewBag.MaxScore = maxScore;

		return View("~/Views/Apps/UserSatisfactionSurveys/Index.cshtml");
	}

	[HttpPost]
	[ValidateAntiForgeryToken]
	public async Task<IActionResult> Onboard(string fipsId, string? displayName)
	{
		if (string.IsNullOrWhiteSpace(fipsId))
		{
			TempData["ErrorMessage"] = "FIPS ID is required.";
			return RedirectToAction(nameof(Index));
		}
		var svc = await _db.Services.FirstOrDefaultAsync(s => s.FipsId == fipsId);
		if (svc == null)
		{
			_db.Services.Add(new Compass.Models.FipsService { FipsId = fipsId, DisplayName = displayName, IsActive = true, CreatedUtc = DateTime.UtcNow });
		}
		else
		{
			svc.DisplayName = displayName ?? svc.DisplayName;
			svc.IsActive = true;
			svc.UpdatedUtc = DateTime.UtcNow;
		}
		await _db.SaveChangesAsync();
		TempData["SuccessMessage"] = "Service onboarded.";
		return RedirectToAction(nameof(Index));
	}

    public async Task<IActionResult> Enroll(string? fipsId = null)
	{
		ViewData["Title"] = "Enrol Product";
		ViewBag.FipsId = fipsId;
        if (!string.IsNullOrWhiteSpace(fipsId))
        {
            var products = await _productsApiService.GetProductsAsync();
            var product = products?.FirstOrDefault(p => p.FipsId == fipsId);
            if (product != null)
            {
                var businessArea = product.CategoryValues?
                    .FirstOrDefault(cv => cv.CategoryType != null && cv.CategoryType.Name == "Business Area")?.Name;
                ViewBag.Product = product;
                ViewBag.BusinessArea = businessArea;
            }
        }
		return View("~/Views/Apps/UserSatisfactionSurveys/Enroll.cshtml");
	}

    [HttpPost]
    [ActionName("Enroll")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ConfirmEnroll(string fipsId)
	{
		if (string.IsNullOrWhiteSpace(fipsId))
		{
			TempData["ErrorMessage"] = "FIPS ID is required.";
			return RedirectToAction(nameof(Enroll));
		}
		var svc = await _db.Services.FirstOrDefaultAsync(s => s.FipsId == fipsId);
		if (svc == null)
		{
            _db.Services.Add(new Compass.Models.FipsService { FipsId = fipsId, IsActive = true, CreatedUtc = DateTime.UtcNow });
		}
		else
		{
			svc.IsActive = true;
			svc.UpdatedUtc = DateTime.UtcNow;
		}
		await _db.SaveChangesAsync();
		TempData["SuccessMessage"] = "Product enrolled in User satisfaction service.";
		return RedirectToAction(nameof(Details), new { fipsId });
	}

	public async Task<IActionResult> Details(string fipsId, string? tab = null)
	{
		var service = await _db.Services.FirstOrDefaultAsync(s => s.FipsId == fipsId);
		if (service == null)
		{
			return RedirectToAction(nameof(Enroll), new { fipsId });
		}

        // Get product title from CMS
        string? productTitle = null;
        var products = await _productsApiService.GetProductsAsync();
        var product = products?.FirstOrDefault(p => p.FipsId == fipsId);
        if (product != null)
        {
            productTitle = product.Title;
        }

		var statsQuery = _db.SurveyResponses
			.Where(r => r.SurveyInstance!.ServiceId == service.ServiceId);
		var count = await statsQuery.CountAsync();
		double avg = 0;
		if (count > 0)
		{
			avg = await statsQuery.AverageAsync(r => (double)r.UssComputed);
		}
		var recent = await statsQuery
			.OrderByDescending(r => r.SubmittedUtc)
			.Take(25)
			.Select(r => new { r.SubmittedUtc, r.Channel, r.GeoRegion, r.UssComputed, r.Band, r.FreeText })
			.ToListAsync();

		ViewBag.CurrentTab = string.IsNullOrWhiteSpace(tab) ? "overview" : tab;
		ViewBag.FipsId = fipsId;
        ViewBag.DisplayName = productTitle ?? service.DisplayName ?? fipsId;
        ViewBag.ProductTitle = productTitle;
		ViewBag.UssAvg = Math.Round(avg, 1);
		ViewBag.UssCount = count;
		ViewBag.Recent = recent;

		return View("~/Views/Apps/UserSatisfactionSurveys/Details.cshtml");
	}

}


