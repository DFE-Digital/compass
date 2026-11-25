using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Compass.Services;
using Compass.Services.Census;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Compass.Controllers.Modern;

[Authorize]
[Route("census")]
[Route("modern/census")]
public sealed class ModernCensusController : Controller
{
    private readonly IServiceCensusService _census;
    private readonly IGlobalFeatureToggleService _features;
    private readonly IPermissionService _permission;

    public ModernCensusController(
        IServiceCensusService census,
        IGlobalFeatureToggleService features,
        IPermissionService permission)
    {
        _census = census;
        _features = features;
        _permission = permission;
    }

    private string CurrentUserEmail =>
        User.Identity?.Name
        ?? User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("preferred_username")?.Value
        ?? User.FindFirst("email")?.Value
        ?? "";

    private void SetNav(string sub)
    {
        ViewBag.MainNavSection = "census";
        ViewBag.SubNavItem = sub;
        ViewBag.UseCensusServiceNav = true;
    }

    private async Task<IActionResult?> RequireRegisterAsync()
    {
        if (await _features.IsFeatureEnabledForPrincipalAsync(Models.FeatureCodes.Fips, User))
            return null;
        TempData["ErrorMessage"] = "The service register is turned off, so the service census is not available.";
        return RedirectToAction("Dashboard", "ModernWork");
    }

    [HttpGet("")]
    public Task<IActionResult> Index(string? q, CancellationToken cancellationToken) =>
        Products(showAll: false, q, null, cancellationToken);

    [HttpGet("all")]
    public Task<IActionResult> All(string? q, string? contact, CancellationToken cancellationToken) =>
        Products(showAll: true, q, contact, cancellationToken);

    [HttpGet("suggest")]
    public async Task<IActionResult> Suggest(string? q, bool all = false, CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        var needle = (q ?? "").Trim();
        var matches = await _census.SuggestProductsAsync(CurrentUserEmail, all, needle, 15, cancellationToken);
        return Json(new
        {
            results = matches.Select(p => new
            {
                id = p.Id,
                title = p.Title,
                href = Url.Action(nameof(TaskList), new { id = p.Id, all }),
                hint = p.Submitted ? "Submitted" : $"{p.Percent}% complete"
            })
        });
    }

    [HttpGet("dependencies/suggest")]
    public async Task<IActionResult> SuggestDependencies(string? q, Guid? exclude, CancellationToken cancellationToken)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        var matches = await _census.SuggestDependenciesAsync(q, exclude, 12, cancellationToken);
        return Json(new
        {
            results = matches.Select(p => new { code = p.Code, title = p.Title, hint = p.Kind })
        });
    }

    [HttpGet("report")]
    [HttpGet("/ModernCensus/Report")]
    public async Task<IActionResult> Report(string? area, CancellationToken cancellationToken)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        SetNav("census-report");
        var vm = await _census.GetReportAsync(area, cancellationToken);
        vm.CanExport = await CanExportReportAsync();
        ViewData["Title"] = "Census report";
        return View("~/Views/Modern/Census/Report.cshtml", vm);
    }

    [HttpGet("report/export")]
    public async Task<IActionResult> ExportReport(string? area, CancellationToken cancellationToken)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        if (!await CanExportReportAsync())
        {
            TempData["ErrorMessage"] = "Only admin and Central Operations users can export this report.";
            return RedirectToAction(nameof(Report), new { area });
        }

        var vm = await _census.GetReportAsync(area, cancellationToken, includeSubmissions: true);
        var bytes = ServiceCensusReportExcelExport.BuildWorkbook(vm);
        var scope = "all";
        if (!string.IsNullOrWhiteSpace(vm.BusinessAreaFilter))
        {
            var slug = new string(vm.BusinessAreaFilter.ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) ? c : '-')
                .ToArray())
                .Trim('-');
            while (slug.Contains("--", StringComparison.Ordinal))
                slug = slug.Replace("--", "-", StringComparison.Ordinal);
            if (!string.IsNullOrEmpty(slug))
                scope = slug;
        }
        var fileName = $"census-report-{scope}-{DateTime.UtcNow:yyyyMMdd-HHmm}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    [HttpGet("report/responses")]
    [HttpGet("/ModernCensus/Responses")]
    public async Task<IActionResult> Responses(
        string? dim,
        string? question,
        Guid? product,
        string? area,
        bool? submittedOnly,
        string? applied,
        CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        if (!await CanExportReportAsync())
        {
            TempData["ErrorMessage"] = "Only admin and Central Operations users can view submitted census data.";
            return RedirectToAction(nameof(Report), new { area });
        }

        SetNav("census-explore");
        var report = await _census.GetReportAsync(area, cancellationToken, includeSubmissions: true);
        var dimension = string.Equals(dim, "product", StringComparison.OrdinalIgnoreCase) ? "product" : "question";
        var submittedOnlyFlag = submittedOnly ?? string.IsNullOrWhiteSpace(applied);
        var products = submittedOnlyFlag
            ? report.Products.Where(p => p.Submitted).ToList()
            : report.Products;
        if (products.Count == 0)
            products = report.Products;

        var questionKey = question;
        if (string.IsNullOrWhiteSpace(questionKey) || CensusDefinition.Find(questionKey) == null)
            questionKey = CensusDefinition.Tasks.FirstOrDefault()?.Key;

        var productId = product;
        if (productId == null || products.All(p => p.Id != productId))
            productId = products.FirstOrDefault()?.Id;

        var answers = report.SubmissionAnswers.AsEnumerable();
        if (submittedOnlyFlag)
            answers = answers.Where(a => string.Equals(a.CensusStatus, "Submitted", StringComparison.OrdinalIgnoreCase));

        List<ServiceCensusReportAnswerRow> rows;
        string? selectedQuestion = null;
        string? selectedProduct = null;
        var valueCounts = new List<ServiceCensusExploreValueCount>();
        if (dimension == "product")
        {
            rows = answers
                .Where(a => a.ProductId == productId)
                .OrderBy(a => a.Section, StringComparer.OrdinalIgnoreCase)
                .ThenBy(a => a.Question, StringComparer.OrdinalIgnoreCase)
                .ToList();
            selectedProduct = products.FirstOrDefault(p => p.Id == productId)?.Title;
        }
        else
        {
            rows = answers
                .Where(a => string.Equals(a.QuestionKey, questionKey, StringComparison.OrdinalIgnoreCase))
                .OrderBy(a => a.Product, StringComparer.OrdinalIgnoreCase)
                .ToList();
            selectedQuestion = CensusDefinition.Find(questionKey ?? "")?.NavLabel ?? questionKey;
            valueCounts = rows
                .Where(a => !string.IsNullOrWhiteSpace(a.Item))
                .GroupBy(a => a.Item!, StringComparer.OrdinalIgnoreCase)
                .Select(g => new ServiceCensusExploreValueCount
                {
                    Value = g.First().Item ?? g.Key,
                    ProductCount = g.Select(x => x.ProductId).Distinct().Count()
                })
                .OrderByDescending(v => v.ProductCount)
                .ThenBy(v => v.Value, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        ViewData["Title"] = "Census submitted data";
        return View("~/Views/Modern/Census/Responses.cshtml", new ServiceCensusExploreViewModel
        {
            Dimension = dimension,
            QuestionKey = questionKey,
            ProductId = productId,
            BusinessAreaFilter = report.BusinessAreaFilter,
            SubmittedOnly = submittedOnlyFlag,
            BusinessAreaOptions = report.BusinessAreaOptions,
            Questions = report.Questions,
            Products = products.OrderBy(p => p.Title, StringComparer.OrdinalIgnoreCase).ToList(),
            Rows = rows,
            ValueCounts = valueCounts,
            SelectedQuestion = selectedQuestion,
            SelectedProduct = selectedProduct
        });
    }

    [HttpGet("analysis")]
    [HttpGet("report/analysis")]
    [HttpGet("/ModernCensus/Analysis")]
    public async Task<IActionResult> Analysis(
        string? view,
        string? area,
        string? question,
        Guid? product,
        bool? submittedOnly,
        bool? groupBy,
        string? applied,
        CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        if (!await CanExportReportAsync())
        {
            TempData["ErrorMessage"] = "Only admin and Central Operations users can view census analysis.";
            return RedirectToAction(nameof(Report), new { area });
        }

        SetNav("census-analysis");
        var submittedOnlyFlag = submittedOnly ?? string.IsNullOrWhiteSpace(applied);
        var vm = await _census.GetAnalysisAsync(
            area, view, question, product, submittedOnlyFlag, cancellationToken, groupBy);
        ViewData["Title"] = "Census analysis";
        return View("~/Views/Modern/Census/Analysis.cshtml", vm);
    }

    private Task<bool> CanExportReportAsync() =>
        _permission.IsOperationConsoleUserAsync(CurrentUserEmail);

    private async Task<IActionResult> Products(bool showAll, string? q, string? contact, CancellationToken ct)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        SetNav(showAll ? "census-all" : "census-yours");
        var vm = await _census.ListProductsAsync(CurrentUserEmail, showAll, q, contact, ct);
        ViewData["Title"] = showAll ? "All products" : "Your products";
        return View("~/Views/Modern/Census/Index.cshtml", vm);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> TaskList(Guid id, bool all = false, CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        SetNav(all ? "census-all" : "census-yours");
        var vm = await _census.GetTaskListAsync(id, CurrentUserEmail, all, cancellationToken);
        if (vm == null) return NotFound();
        vm.CanAdminReset = await UserCanResetCensusAsync();
        ViewData["Title"] = vm.ProductTitle + " — Service census";
        return View("~/Views/Modern/Census/TaskList.cshtml", vm);
    }

    [HttpGet("{id:guid}/questions/{taskKey}")]
    public async Task<IActionResult> Question(Guid id, string taskKey, bool all = false, CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        SetNav(all ? "census-all" : "census-yours");
        var vm = await _census.GetQuestionAsync(id, taskKey, CurrentUserEmail, all, cancellationToken);
        if (vm == null) return NotFound();
        vm.CanAdminReset = await UserCanResetCensusAsync();
        if (vm.PickFromRegister)
            vm.DependencySuggestUrl = Url.Action(nameof(SuggestDependencies), new { exclude = id });
        ViewData["Title"] = vm.Heading + " — " + vm.ProductTitle;
        return View("~/Views/Modern/Census/Question.cshtml", vm);
    }

    [HttpPost("{id:guid}/questions/{taskKey}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Question(
        Guid id,
        string taskKey,
        ServiceCensusSaveRequest model,
        bool all = false,
        CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        SetNav(all ? "census-all" : "census-yours");
        BindPostedItems(model, Request.Form);
        BindPostedContacts(model, Request.Form);

        var (ok, error, redirectKey) = await _census.SaveQuestionAsync(id, taskKey, CurrentUserEmail, model, cancellationToken);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, error ?? "The answer could not be saved.");
            if (model.CannotAnswer)
                ModelState.AddModelError("CannotAnswerReason", error ?? "");
            else if (model.Items.Count == 0)
                ModelState.AddModelError("Items", error ?? "");
            var vm = await _census.GetQuestionAsync(id, taskKey, CurrentUserEmail, all, cancellationToken);
            if (vm == null) return NotFound();
            vm.CanAdminReset = await UserCanResetCensusAsync();
            ApplyPosted(vm, model);
            if (vm.PickFromRegister)
                vm.DependencySuggestUrl = Url.Action(nameof(SuggestDependencies), new { exclude = id });
            ViewData["Title"] = vm.Heading + " — " + vm.ProductTitle;
            return View("~/Views/Modern/Census/Question.cshtml", vm);
        }

        TempData["SuccessMessage"] = "Saved.";
        var intent = (model.Intent ?? "save").Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(redirectKey) && intent is "complete" or "next")
            return RedirectToAction(nameof(Submit), new { id, all });
        return RedirectToAction(nameof(Question), new { id, taskKey = redirectKey ?? taskKey, all });
    }

    [HttpGet("{id:guid}/submit")]
    public async Task<IActionResult> Submit(Guid id, bool all = false, CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        SetNav(all ? "census-all" : "census-yours");
        var vm = await _census.GetSubmitAsync(id, CurrentUserEmail, all, cancellationToken);
        if (vm == null) return NotFound();
        vm.CanAdminReset = await UserCanResetCensusAsync();
        ViewData["Title"] = "Submit census — " + vm.ProductTitle;
        return View("~/Views/Modern/Census/Submit.cshtml", vm);
    }

    [HttpPost("{id:guid}/submit")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SubmitPost(Guid id, bool all = false, CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        var (ok, error) = await _census.SubmitAsync(id, CurrentUserEmail, cancellationToken);
        if (!ok)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(Submit), new { id, all });
        }

        TempData["SuccessMessage"] = "Census submitted. The service register has been updated.";
        return RedirectToAction(nameof(TaskList), new { id, all });
    }

    [HttpPost("{id:guid}/reopen")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reopen(Guid id, bool all = false, string? taskKey = null, CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        var (ok, error) = await _census.ReopenAsync(id, CurrentUserEmail, cancellationToken);
        if (!ok)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(TaskList), new { id, all });
        }

        TempData["SuccessMessage"] = "Census reopened. A named contact must submit it again when the changes are complete.";
        if (!string.IsNullOrWhiteSpace(taskKey) && CensusDefinition.Find(taskKey) != null)
            return RedirectToAction(nameof(Question), new { id, taskKey, all });
        return RedirectToAction(nameof(TaskList), new { id, all });
    }

    [HttpPost("{id:guid}/reset")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reset(Guid id, bool all = false, CancellationToken cancellationToken = default)
    {
        var blocked = await RequireRegisterAsync();
        if (blocked != null) return blocked;
        if (!await UserCanResetCensusAsync())
            return Forbid();

        var (ok, error) = await _census.ResetAsync(id, CurrentUserEmail, cancellationToken);
        if (!ok)
        {
            TempData["ErrorMessage"] = error;
            return RedirectToAction(nameof(TaskList), new { id, all });
        }

        TempData["SuccessMessage"] = "Census reset. All answers have been cleared. A named contact must complete and submit it again.";
        return RedirectToAction(nameof(TaskList), new { id, all });
    }

    private Task<bool> UserCanResetCensusAsync() =>
        _permission.IsOperationConsoleUserAsync(CurrentUserEmail);

    private static void BindPostedItems(ServiceCensusSaveRequest model, IFormCollection form)
    {
        var byIndex = new SortedDictionary<int, ServiceCensusAnswerRowViewModel>();
        foreach (var key in form.Keys)
        {
            var name = key;
            const string prefix = "model.";
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                name = name[prefix.Length..];
            if (!name.StartsWith("Items[", StringComparison.OrdinalIgnoreCase))
                continue;
            var close = name.IndexOf(']');
            if (close < 6 || !int.TryParse(name.AsSpan(6, close - 6), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                continue;
            var prop = name[(close + 1)..].TrimStart('.');
            if (!byIndex.TryGetValue(index, out var row))
            {
                row = new ServiceCensusAnswerRowViewModel();
                byIndex[index] = row;
            }
            var value = form[key].ToString();
            if (prop.Equals("Title", StringComparison.OrdinalIgnoreCase))
                row.Title = value;
            else if (prop.Equals("Narrative", StringComparison.OrdinalIgnoreCase))
                row.Narrative = value;
            else if (prop.Equals("ExternalUrl", StringComparison.OrdinalIgnoreCase))
                row.ExternalUrl = value;
            else if (prop.Equals("LookupCode", StringComparison.OrdinalIgnoreCase))
                row.LookupCode = value;
        }

        var parsed = byIndex.Values
            .Where(item =>
                !string.IsNullOrWhiteSpace(item.Title)
                || !string.IsNullOrWhiteSpace(item.Narrative)
                || !string.IsNullOrWhiteSpace(item.ExternalUrl)
                || !string.IsNullOrWhiteSpace(item.LookupCode))
            .ToList();
        if (parsed.Count == 0)
            parsed = ParseItemListJson(form);

        model.ItemListPosted = form.TryGetValue("ItemListPosted", out var posted)
            && posted.Any(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
        if (model.ItemListPosted || parsed.Count > 0)
            model.Items = parsed;

        model.ItemListCleared = form.TryGetValue("ItemListCleared", out var cleared)
            && cleared.Any(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
        model.CannotAnswer = form.TryGetValue("CannotAnswer", out var cannot)
            && cannot.Any(value => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase));
    }

    private static void BindPostedContacts(ServiceCensusSaveRequest model, IFormCollection form)
    {
        var byIndex = new SortedDictionary<int, ServiceCensusContactRowViewModel>();
        foreach (var key in form.Keys)
        {
            var name = key;
            const string prefix = "model.";
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                name = name[prefix.Length..];
            if (!name.StartsWith("Contacts[", StringComparison.OrdinalIgnoreCase))
                continue;
            var close = name.IndexOf(']');
            if (close < 9 || !int.TryParse(name.AsSpan(9, close - 9), NumberStyles.None, CultureInfo.InvariantCulture, out var index))
                continue;
            var prop = name[(close + 1)..].TrimStart('.');
            if (!byIndex.TryGetValue(index, out var row))
            {
                row = new ServiceCensusContactRowViewModel();
                byIndex[index] = row;
            }
            var value = form[key].ToString();
            switch (prop.ToLowerInvariant())
            {
                case "rolekey": row.RoleKey = value; break;
                case "rolename": row.RoleName = value; break;
                case "originalname": row.OriginalName = value; break;
                case "originalemail": row.OriginalEmail = value; break;
                case "selectedname": row.SelectedName = value; break;
                case "selectedemail": row.SelectedEmail = value; break;
                case "isadded":
                    row.IsAdded = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(value, "on", StringComparison.OrdinalIgnoreCase);
                    break;
            }
        }

        if (byIndex.Count > 0)
            model.Contacts = byIndex.Values.Where(c => !string.IsNullOrWhiteSpace(c.RoleKey)).ToList();
    }

    private static List<ServiceCensusAnswerRowViewModel> ParseItemListJson(IFormCollection form)
    {
        if (!form.TryGetValue("ItemListJson", out var raw))
            return [];
        var text = raw.ToString();
        if (string.IsNullOrWhiteSpace(text))
            return [];
        try
        {
            using var doc = JsonDocument.Parse(text);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return [];
            var rows = new List<ServiceCensusAnswerRowViewModel>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind != JsonValueKind.Object) continue;
                var row = new ServiceCensusAnswerRowViewModel
                {
                    Title = ReadJsonString(el, "Title") ?? ReadJsonString(el, "title"),
                    Narrative = ReadJsonString(el, "Narrative") ?? ReadJsonString(el, "narrative"),
                    ExternalUrl = ReadJsonString(el, "ExternalUrl") ?? ReadJsonString(el, "url") ?? ReadJsonString(el, "externalUrl"),
                    LookupCode = ReadJsonString(el, "LookupCode") ?? ReadJsonString(el, "code")
                };
                if (!string.IsNullOrWhiteSpace(row.Title)
                    || !string.IsNullOrWhiteSpace(row.Narrative)
                    || !string.IsNullOrWhiteSpace(row.ExternalUrl)
                    || !string.IsNullOrWhiteSpace(row.LookupCode))
                    rows.Add(row);
            }
            return rows;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? ReadJsonString(JsonElement el, string name)
    {
        if (!el.TryGetProperty(name, out var value))
            return null;
        return value.ValueKind == JsonValueKind.String ? value.GetString() : value.ToString();
    }

    private static void ApplyPosted(ServiceCensusQuestionViewModel vm, ServiceCensusSaveRequest model)
    {
        vm.CannotAnswer = model.CannotAnswer;
        vm.CannotAnswerReason = model.CannotAnswerReason;
        vm.LongText = model.LongText;
        vm.YesNo = model.YesNo;
        vm.ChoiceValue = model.ChoiceValue;
        vm.ChoiceValues = model.ChoiceValues.Count > 0
            ? model.ChoiceValues
            : string.IsNullOrWhiteSpace(model.ChoiceValue) ? vm.ChoiceValues : [model.ChoiceValue];
        vm.ChoiceNarrative = model.ChoiceNarrative;
        vm.AccessibilityNotEnrolledReason = model.AccessibilityNotEnrolledReason;
        vm.LookupId = model.LookupId;
        vm.SelectedLookupIds = model.LookupIds.Count > 0
            ? model.LookupIds
            : string.IsNullOrWhiteSpace(model.LookupId) ? vm.SelectedLookupIds : [model.LookupId];
        if (model.FormValues.Count > 0) vm.FormValues = model.FormValues;
        if (model.FormNarratives.Count > 0) vm.FormNarratives = model.FormNarratives;
        vm.Items = model.Items;
        if (model.Contacts.Count > 0)
            vm.Contacts = model.Contacts;
    }
}
