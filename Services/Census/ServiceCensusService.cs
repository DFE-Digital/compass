using Compass.Data;
using Compass.Models;
using Compass.Models.Census;
using Compass.Models.Fips;
using Compass.Models.ServiceSchema;
using Compass.Services.Aiss;
using Compass.Services.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Census;

public sealed class ServiceCensusService : IServiceCensusService
{
    private readonly CompassDbContext _db;
    private readonly IAissProductAccessibilityService _aiss;
    private readonly IFipsBusinessAreaLookupSyncService _businessAreaSync;
    private readonly IFipsDirectorateLookupSyncService _directorateSync;
    private readonly IPermissionService _permissions;

    public ServiceCensusService(
        CompassDbContext db,
        IAissProductAccessibilityService aiss,
        IFipsBusinessAreaLookupSyncService businessAreaSync,
        IFipsDirectorateLookupSyncService directorateSync,
        IPermissionService permissions)
    {
        _db = db;
        _aiss = aiss;
        _businessAreaSync = businessAreaSync;
        _directorateSync = directorateSync;
        _permissions = permissions;
    }

    public async Task<ServiceCensusProductListViewModel> ListProductsAsync(
        string userEmail, bool showAll, string? search, string? contact, CancellationToken ct)
    {
        var products = await VisibleProductsQuery()
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.Status,
                Areas = p.BusinessAreas
                    .Where(b => b.FipsBusinessArea != null && b.FipsBusinessArea.Name != "")
                    .Select(b => b.FipsBusinessArea.Name)
                    .ToList(),
                IsNamed = p.Contacts.Any(c =>
                    c.UserEmail != null && c.UserEmail != "" && c.UserEmail.ToLower() == userEmail.ToLower())
            })
            .ToListAsync(ct);

        var productIds = products.Select(p => p.Id).ToList();
        var actorEmail = (userEmail ?? "").Trim();
        var censusContacts = productIds.Count == 0
            ? []
            : await _db.ServiceCensusContactReviews.AsNoTracking()
                .Where(r => productIds.Contains(r.Response.ProductId)
                    && ((r.CmdbName != null && r.CmdbName != "")
                        || (r.CmdbEmail != null && r.CmdbEmail != "")))
                .Select(r => new
                {
                    r.Response.ProductId,
                    r.RoleKey,
                    r.CmdbName,
                    r.CmdbEmail,
                    r.IncorrectReason
                })
                .ToListAsync(ct);
        var censusNamedIds = censusContacts
            .Where(c => actorEmail.Length > 0
                && !string.IsNullOrWhiteSpace(c.CmdbEmail)
                && string.Equals(c.CmdbEmail.Trim(), actorEmail, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.ProductId)
            .ToHashSet();
        var responses = await _db.ServiceCensusResponses.AsNoTracking()
            .Where(r => productIds.Contains(r.ProductId))
            .Select(r => new
            {
                r.ProductId,
                r.Status,
                r.SubmittedAt,
                CompleteKeys = r.Tasks
                    .Where(t => t.Status == ServiceCensusStatuses.Complete)
                    .Select(t => t.TaskKey)
            })
            .ToListAsync(ct);
        var responseByProduct = responses
            .GroupBy(r => r.ProductId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.SubmittedAt).First());
        var submittedFromRegister = await CensusSubmissionDeclarationProductIdsAsync(productIds, ct);

        var taskKeys = CensusDefinition.Tasks.Select(t => t.Key).ToList();
        var taskCount = taskKeys.Count;
        var rows = products.Select(p =>
        {
            responseByProduct.TryGetValue(p.Id, out var response);
            var submitted = IsCensusSubmitted(response?.Status, response?.SubmittedAt)
                || submittedFromRegister.Contains(p.Id);
            var complete = response == null
                ? 0
                : taskKeys.Count(key =>
                    response.CompleteKeys.Any(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase)));
            var areaNames = (p.Areas ?? [])
                .Select(a => a.Trim())
                .Where(a => a.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(a => a, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new ServiceCensusProductRowViewModel
            {
                Id = p.Id,
                Title = p.Title,
                Status = submitted
                    ? "Submitted"
                    : complete == 0
                        ? "Not started"
                        : "In progress",
                AreaNames = areaNames.Count == 0 ? ["No business area"] : areaNames,
                Areas = areaNames.Count == 0 ? "No business area" : string.Join(", ", areaNames),
                TasksComplete = submitted ? taskCount : complete,
                TaskCount = taskCount,
                Submitted = submitted,
                SubmittedAt = response?.SubmittedAt,
                UserIsNamedContact = p.IsNamed || censusNamedIds.Contains(p.Id)
            };
        }).ToList();

        var needle = (search ?? "").Trim();
        if (!showAll && !string.IsNullOrEmpty(needle))
            rows = rows.Where(r => r.Title.Contains(needle, StringComparison.OrdinalIgnoreCase)).ToList();

        var contactRoles = await _db.FipsContactRoles.AsNoTracking()
            .Where(r => r.Active && r.Name != "Reporting user")
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .Select(r => r.Name)
            .ToListAsync(ct);
        foreach (var censusRole in CensusDefinition.ContactRoles)
        {
            if (!contactRoles.Any(name => string.Equals(name, censusRole.Name, StringComparison.OrdinalIgnoreCase)))
                contactRoles.Add(censusRole.Name);
        }
        var offeringContacts = await _db.CMDBProductContacts.AsNoTracking()
            .Where(c => productIds.Contains(c.CMDBProductId))
            .Select(c => new { c.CMDBProductId, Role = c.FipsContactRole.Name, c.UserName, c.UserEmail })
            .ToListAsync(ct);
        var peopleByProduct = new Dictionary<Guid, Dictionary<string, List<string>>>();
        foreach (var offering in offeringContacts)
        {
            AddRolePerson(peopleByProduct, offering.CMDBProductId, offering.Role, offering.UserName);
        }
        foreach (var review in censusContacts)
        {
            var roleName = CensusContactRoleName(review.RoleKey, review.IncorrectReason);
            if (!contactRoles.Any(name => string.Equals(name, roleName, StringComparison.OrdinalIgnoreCase)))
                contactRoles.Add(roleName);
            AddRolePerson(peopleByProduct, review.ProductId, roleName, FirstNonBlank(review.CmdbName, review.CmdbEmail));
        }
        foreach (var row in rows)
        {
            if (!peopleByProduct.TryGetValue(row.Id, out var roles)) continue;
            row.RoleContacts = roles.ToDictionary(
                r => r.Key,
                r => string.Join(", ", r.Value.OrderBy(n => n, StringComparer.OrdinalIgnoreCase)),
                StringComparer.OrdinalIgnoreCase);
            row.ContactNames = roles.Values
                .SelectMany(names => names)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        var contactOptions = peopleByProduct.Values
            .SelectMany(roles => roles.Values)
            .SelectMany(names => names)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var areaOptions = rows
            .SelectMany(r => r.AreaNames)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var contactNeedle = (contact ?? "").Trim();
        if (!showAll && !string.IsNullOrEmpty(contactNeedle))
        {
            var matchingIds = offeringContacts
                .Where(c =>
                    string.Equals((c.UserName ?? "").Trim(), contactNeedle, StringComparison.OrdinalIgnoreCase)
                    || string.Equals((c.UserEmail ?? "").Trim(), contactNeedle, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.CMDBProductId)
                .Concat(censusContacts
                    .Where(c =>
                        string.Equals((c.CmdbName ?? "").Trim(), contactNeedle, StringComparison.OrdinalIgnoreCase)
                        || string.Equals((c.CmdbEmail ?? "").Trim(), contactNeedle, StringComparison.OrdinalIgnoreCase))
                    .Select(c => c.ProductId))
                .ToHashSet();
            rows = rows.Where(r => matchingIds.Contains(r.Id)).ToList();
        }

        var yours = rows.Where(r => r.UserIsNamedContact).ToList();
        var shown = showAll ? rows : yours;

        return new ServiceCensusProductListViewModel
        {
            ShowAll = showAll,
            Search = search,
            ContactFilter = string.IsNullOrEmpty(contactNeedle) ? null : contactNeedle,
            ContactOptions = contactOptions,
            BusinessAreaOptions = areaOptions,
            ContactRoles = contactRoles,
            YourProductCount = yours.Count,
            AllProductCount = rows.Count,
            Products = shown
                .OrderBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
                .ToList()
        };
    }

    public async Task<ServiceCensusTaskListViewModel?> GetTaskListAsync(
        Guid productId, string userEmail, bool showAll, CancellationToken ct)
    {
        var product = await LoadProductAsync(productId, ct);
        if (product == null) return null;

        var response = await EnsureResponseAsync(product, userEmail, ct);
        if (await ProductIsAissEnrolledAsync(product, ct))
            await MarkAccessibilityEnrolledCompleteAsync(response, userEmail, ct);
        var named = NamedSubmitters(product, response);
        var userIsNamed = UserIsNamedSubmitter(named, userEmail);
        var opsCanSubmit = await UserIsOpsSubmitterAsync(userEmail);
        var allComplete = CensusDefinition.Tasks.All(t =>
            StatusOf(response, t.Key) == ServiceCensusStatuses.Complete);
        var submitted = await EnsureSubmittedStateAsync(response, product.Id, userEmail, ct);
        var canSubmit = CanUserSubmitReturn(userIsNamed, opsCanSubmit, allComplete, submitted);

        string blocked = SubmitBlockedReason(submitted, allComplete, userIsNamed, opsCanSubmit, named.Count);

        return new ServiceCensusTaskListViewModel
        {
            ProductId = product.Id,
            ProductTitle = product.Title,
            ShowAll = showAll,
            CanSubmit = canSubmit,
            AllComplete = allComplete,
            UserIsNamedSubmitter = userIsNamed,
            UserIsNamedOrAdmin = userIsNamed || opsCanSubmit,
            UserIsOpsAdmin = opsCanSubmit,
            Submitted = submitted,
            SubmittedAt = response.SubmittedAt,
            SubmittedBy = response.SubmittedBy,
            SubmitBlockedReason = blocked,
            NamedSubmitters = named,
            TasksComplete = CensusDefinition.Tasks.Count(t => StatusOf(response, t.Key) == ServiceCensusStatuses.Complete),
            TaskCount = CensusDefinition.Tasks.Count,
            Nav = NavFor(response, null),
            Groups = CensusDefinition.Groups().Select(g => new ServiceCensusTaskGroupViewModel
            {
                Key = g.Key,
                Name = g.First().GroupName,
                Tasks = g.Select(t => Summary(t, StatusOf(response, t.Key))).ToList()
            }).ToList()
        };
    }

    public async Task<ServiceCensusQuestionViewModel?> GetQuestionAsync(
        Guid productId, string taskKey, string userEmail, bool showAll, CancellationToken ct)
    {
        var task = CensusDefinition.Find(taskKey);
        if (task == null) return null;
        var product = await LoadProductAsync(productId, ct);
        if (product == null) return null;

        var response = await EnsureResponseAsync(product, userEmail, ct);
        var state = StateOf(response, task.Key);
        var vm = BaseQuestion(product, task, response, state, showAll);

        switch (task.Kind)
        {
            case CensusTaskKind.LongText:
                vm.LongText = task.Key == "service"
                    ? response.ServiceDescription
                    : task.Key == "service-line"
                        ? response.ServiceLineDescription
                        : state?.Answers.OrderBy(a => a.SortOrder).FirstOrDefault()?.Narrative;
                if (task.Key == "aim" && string.IsNullOrWhiteSpace(vm.LongText))
                    vm.LongText = FirstNonBlank(product.UserDescription, product.CMDBDescription);
                break;
            case CensusTaskKind.YesChoice:
                var yesAnswers = (state?.Answers ?? []).OrderBy(a => a.SortOrder).ToList();
                vm.YesNo = yesAnswers.FirstOrDefault()?.Title;
                vm.ChoiceValues = yesAnswers
                    .Select(a => a.LookupCode)
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .SelectMany(code => code!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                vm.ChoiceValue = vm.ChoiceValues.FirstOrDefault();
                vm.ChoiceNarrative = yesAnswers.Select(a => a.Narrative).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n));
                vm.ChoiceOptions = task.ChoiceOptions.ToList();
                break;
            case CensusTaskKind.Contacts:
                vm.Contacts = BuildContacts(product, response);
                break;
            case CensusTaskKind.Accessibility:
                await PopulateAccessibilityAsync(vm, product, response, ct);
                if (vm.AccessibilityEnrolled)
                {
                    await MarkAccessibilityEnrolledCompleteAsync(response, userEmail, ct);
                    vm.Status = ServiceCensusStatuses.Complete;
                    vm.CannotAnswer = false;
                    vm.CannotAnswerReason = null;
                    vm.Nav = NavFor(response, task.Key);
                }
                break;
            case CensusTaskKind.Lookup:
                await PopulateLookupAsync(vm, task, product, state, ct);
                break;
            case CensusTaskKind.Form:
                vm.FormFields = task.FormFields.ToList();
                var formKeys = task.FormFields.Select(f => f.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
                foreach (var answer in (state?.Answers ?? []).OrderBy(a => a.SortOrder))
                {
                    var key = answer.Title ?? "";
                    if (string.IsNullOrWhiteSpace(key)) continue;
                    if (!formKeys.Contains(key))
                    {
                        if (IsConditionalChoiceAnswer(task, key) && !string.IsNullOrWhiteSpace(answer.LookupCode))
                        {
                            vm.ChoiceValues.Add(answer.LookupCode);
                            continue;
                        }
                        var url = string.IsNullOrWhiteSpace(answer.ExternalUrl) ? key : answer.ExternalUrl;
                        if (!string.IsNullOrWhiteSpace(url))
                            vm.Items.Add(new ServiceCensusAnswerRowViewModel { Title = url, ExternalUrl = url });
                        continue;
                    }
                    if (!string.IsNullOrWhiteSpace(answer.LookupCode))
                        vm.FormValues[key] = answer.LookupCode;
                    else if (!string.IsNullOrWhiteSpace(answer.Narrative) && !vm.FormValues.ContainsKey(key))
                        vm.FormValues[key] = answer.Narrative;
                    if (!string.IsNullOrWhiteSpace(answer.Narrative))
                        vm.FormNarratives[key] = answer.Narrative;
                }
                break;
            default:
                vm.Items = (state?.Answers ?? [])
                    .OrderBy(a => a.SortOrder)
                    .Select(a => new ServiceCensusAnswerRowViewModel
                    {
                        Title = string.IsNullOrWhiteSpace(a.Title) ? a.Narrative : a.Title,
                        Narrative = a.Narrative,
                        ExternalUrl = a.ExternalUrl,
                        LookupCode = a.LookupCode
                    })
                    .Where(a => !string.IsNullOrWhiteSpace(a.Title))
                    .ToList();
                break;
        }

        if (task.Kind == CensusTaskKind.Form)
            HydrateNamedFormFields(vm, task, response, state, product);

        var named = NamedSubmitters(product, response);
        var submitted = await EnsureSubmittedStateAsync(response, product.Id, userEmail, ct);
        var allComplete = CensusDefinition.Tasks.All(t =>
            StatusOf(response, t.Key) == ServiceCensusStatuses.Complete);
        var userIsNamed = UserIsNamedSubmitter(named, userEmail);
        var opsCanSubmit = await UserIsOpsSubmitterAsync(userEmail);
        vm.Submitted = submitted;
        vm.AllComplete = allComplete;
        vm.UserIsNamedSubmitter = userIsNamed;
        vm.CanSubmit = CanUserSubmitReturn(userIsNamed, opsCanSubmit, allComplete, submitted);
        vm.NamedSubmitters = named;

        return vm;
    }

    public async Task<(bool Ok, string? Error, string? RedirectTaskKey)> SaveQuestionAsync(
        Guid productId, string taskKey, string userEmail, ServiceCensusSaveRequest request, CancellationToken ct)
    {
        var task = CensusDefinition.Find(taskKey);
        if (task == null) return (false, "That question was not found.", null);
        var product = await LoadProductAsync(productId, ct, tracking: false);
        if (product == null) return (false, "That product was not found.", null);

        var response = await EnsureResponseAsync(product, userEmail, ct);
        if (await EnsureSubmittedStateAsync(response, product.Id, userEmail, ct))
            return (false, "This census has been submitted and cannot be changed.", null);

        var intent = (request.Intent ?? "save").Trim().ToLowerInvariant();
        var markComplete = intent is "complete";
        if (task.Kind == CensusTaskKind.Accessibility)
        {
            request.CannotAnswer = false;
            request.CannotAnswerReason = null;
        }
        var error = Validate(task, request, intent);
        if (error != null) return (false, error, null);

        if (intent is not "previous" and not "next")
        {
            var state = StateOf(response, task.Key);
            if (state == null)
            {
                state = new ServiceCensusTaskState { ResponseId = response.Id, TaskKey = task.Key };
                response.Tasks.Add(state);
                _db.ServiceCensusTaskStates.Add(state);
            }

            state.CannotAnswer = request.CannotAnswer;
            state.CannotAnswerReason = request.CannotAnswer ? Trim(request.CannotAnswerReason, 4000) : null;
            state.UpdatedAt = DateTime.UtcNow;
            state.UpdatedBy = userEmail;
            response.UpdatedAt = DateTime.UtcNow;
            response.UpdatedBy = userEmail;

            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(ct);
                try
                {
                    await ApplyAnswersAsync(task, response, state, request, ct);
                    if (task.Kind == CensusTaskKind.Lookup)
                        await FillLookupTitlesAsync(task, product, state, ct);

                    if (task.Kind == CensusTaskKind.Accessibility
                        && string.Equals(request.YesNo, "enrolled", StringComparison.OrdinalIgnoreCase))
                        state.Status = ServiceCensusStatuses.Complete;
                    else if (markComplete)
                        state.Status = ServiceCensusStatuses.Complete;
                    else if (state.Status != ServiceCensusStatuses.Complete)
                        state.Status = HasContent(task, request, state) || request.CannotAnswer
                            ? ServiceCensusStatuses.InProgress
                            : ServiceCensusStatuses.NotStarted;

                    await SaveCensusAsync(ct);
                    await tx.CommitAsync(ct);
                }
                catch
                {
                    await tx.RollbackAsync(ct);
                    throw;
                }
            });
        }

        var redirect = intent switch
        {
            "next" or "complete" => CensusDefinition.Next(task.Key)?.Key,
            "previous" => CensusDefinition.Previous(task.Key)?.Key,
            _ => task.Key
        };
        return (true, null, redirect);
    }

    public async Task<ServiceCensusSubmitViewModel?> GetSubmitAsync(
        Guid productId, string userEmail, bool showAll, CancellationToken ct)
    {
        var product = await LoadProductAsync(productId, ct);
        if (product == null) return null;
        var response = await EnsureResponseAsync(product, userEmail, ct);
        if (await ProductIsAissEnrolledAsync(product, ct))
            await MarkAccessibilityEnrolledCompleteAsync(response, userEmail, ct);
        var named = NamedSubmitters(product, response);
        var userIsNamed = UserIsNamedSubmitter(named, userEmail);
        var opsCanSubmit = await UserIsOpsSubmitterAsync(userEmail);
        var incomplete = CensusDefinition.Tasks
            .Where(t => StatusOf(response, t.Key) != ServiceCensusStatuses.Complete)
            .Select(t => Summary(t, StatusOf(response, t.Key)))
            .ToList();
        var submitted = await EnsureSubmittedStateAsync(response, product.Id, userEmail, ct);
        var allComplete = incomplete.Count == 0;
        var canSubmit = CanUserSubmitReturn(userIsNamed, opsCanSubmit, allComplete, submitted);

        return new ServiceCensusSubmitViewModel
        {
            ProductId = product.Id,
            ProductTitle = product.Title,
            ShowAll = showAll,
            CanSubmit = canSubmit,
            UserIsNamedOrAdmin = userIsNamed || opsCanSubmit,
            UserIsOpsAdmin = opsCanSubmit,
            AllComplete = allComplete,
            Submitted = submitted,
            BlockedReason = SubmitBlockedReason(submitted, allComplete, userIsNamed, opsCanSubmit, named.Count),
            NamedSubmitters = named,
            Incomplete = incomplete,
            Nav = NavFor(response, null)
        };
    }

    public async Task<(bool Ok, string? Error)> SubmitAsync(Guid productId, string userEmail, CancellationToken ct)
    {
        var product = await LoadProductAsync(productId, ct);
        if (product == null) return (false, "That product was not found.");
        var response = await EnsureResponseAsync(product, userEmail, ct);
        if (await EnsureSubmittedStateAsync(response, product.Id, userEmail, ct))
            return (false, "This census has already been submitted.");
        if (await ProductIsAissEnrolledAsync(product, ct))
            await MarkAccessibilityEnrolledCompleteAsync(response, userEmail, ct);
        var named = NamedSubmitters(product, response);
        var userIsNamed = UserIsNamedSubmitter(named, userEmail);
        var opsCanSubmit = await UserIsOpsSubmitterAsync(userEmail);
        if (!userIsNamed && !opsCanSubmit)
            return (false, "Only a person named on the contacts page, or an admin, can submit this census.");
        // Named contacts must finish every section; ops/admin may submit incomplete returns.
        if (!opsCanSubmit
            && CensusDefinition.Tasks.Any(t => StatusOf(response, t.Key) != ServiceCensusStatuses.Complete))
            return (false, "Every question must be marked complete before you can submit.");

        await PersistSubmittedStatusAsync(response, userEmail, ct);

        try
        {
            await WriteToRegisterAsync(product, response, userEmail, ct);
            await _db.SaveChangesAsync(ct);
        }
        catch
        {
            // Census is already marked submitted; register copy can be retried after reopen.
        }

        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ReopenAsync(Guid productId, string userEmail, CancellationToken ct)
    {
        var product = await LoadProductAsync(productId, ct);
        if (product == null) return (false, "That product was not found.");
        var response = await EnsureResponseAsync(product, userEmail, ct);
        if (response.Status != ServiceCensusStatuses.Submitted)
            return (true, null);

        response.Status = ServiceCensusStatuses.Draft;
        response.SubmittedAt = null;
        response.SubmittedBy = null;
        response.UpdatedAt = DateTime.UtcNow;
        response.UpdatedBy = userEmail;
        await ClearCensusSubmissionDeclarationsAsync(product.Id, ct);
        await _db.SaveChangesAsync(ct);
        return (true, null);
    }

    public async Task<(bool Ok, string? Error)> ResetAsync(Guid productId, string userEmail, CancellationToken ct)
    {
        var product = await LoadProductAsync(productId, ct);
        if (product == null) return (false, "That product was not found.");
        var response = await EnsureResponseAsync(product, userEmail, ct);
        if (!IsCensusSubmitted(response.Status, response.SubmittedAt)
            && !await HasCensusSubmissionDeclarationAsync(product.Id, ct))
            return (false, "Only a submitted census can be reset.");

        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var responseId = response.Id;
                await _db.ServiceCensusAnswers
                    .Where(a => a.TaskState.ResponseId == responseId)
                    .ExecuteDeleteAsync(ct);
                await _db.ServiceCensusTaskStates
                    .Where(t => t.ResponseId == responseId)
                    .ExecuteDeleteAsync(ct);
                await _db.ServiceCensusContactReviews
                    .Where(r => r.ResponseId == responseId)
                    .ExecuteDeleteAsync(ct);

                foreach (var task in response.Tasks.ToList())
                {
                    foreach (var answer in task.Answers.ToList())
                        _db.Entry(answer).State = EntityState.Detached;
                    task.Answers.Clear();
                    _db.Entry(task).State = EntityState.Detached;
                    response.Tasks.Remove(task);
                }

                foreach (var review in response.ContactReviews.ToList())
                {
                    _db.Entry(review).State = EntityState.Detached;
                    response.ContactReviews.Remove(review);
                }

                response.Status = ServiceCensusStatuses.Draft;
                response.SubmittedAt = null;
                response.SubmittedBy = null;
                response.ServiceDescription = null;
                response.ServiceLineDescription = null;
                response.AccessibilityNotEnrolledReason = null;
                response.UpdatedAt = DateTime.UtcNow;
                response.UpdatedBy = userEmail;
                await ClearCensusSubmissionDeclarationsAsync(product.Id, ct);
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });

        return (true, null);
    }

    public Task<ServiceCensusReportViewModel> GetReportAsync(
        string? businessArea, CancellationToken ct, bool includeSubmissions = false) =>
        ServiceCensusReportBuilder.LoadAsync(_db, businessArea, ct, includeSubmissions);

    public async Task<ServiceCensusAnalysisViewModel> GetAnalysisAsync(
        string? businessArea,
        string? view,
        string? questionKey,
        Guid? productId,
        bool submittedOnly,
        CancellationToken ct,
        bool? groupByAnswer = null)
    {
        var report = await GetReportAsync(businessArea, ct, includeSubmissions: true);
        return ServiceCensusAnalysisBuilder.Build(
            report, view, questionKey, productId, submittedOnly, groupByAnswer);
    }

    public async Task<IReadOnlyList<ServiceCensusProductRowViewModel>> SuggestProductsAsync(
        string userEmail, bool showAll, string? search, int limit, CancellationToken ct)
    {
        var take = Math.Clamp(limit, 1, 25);
        var list = await ListProductsAsync(userEmail, showAll, search, null, ct);
        var needle = (search ?? "").Trim();
        IEnumerable<ServiceCensusProductRowViewModel> ranked = list.Products;
        if (!string.IsNullOrEmpty(needle))
            ranked = list.Products
                .OrderBy(r => r.Title.StartsWith(needle, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase);
        return ranked.Take(take).ToList();
    }

    public async Task<IReadOnlyList<CensusDependencySuggestion>> SuggestDependenciesAsync(
        string? search, Guid? excludeProductId, int limit, CancellationToken ct)
    {
        var take = Math.Clamp(limit, 1, 20);
        var needle = (search ?? "").Trim().ToLowerInvariant();
        if (needle.Length == 0)
            return [];

        var products = await _db.CMDBProducts.AsNoTracking()
            .Where(p => (p.Status == CMDBProductStatus.Active || p.Status == CMDBProductStatus.New)
                && (excludeProductId == null || p.Id != excludeProductId)
                && p.Title.ToLower().Contains(needle))
            .OrderBy(p => p.Title)
            .Select(p => new CensusDependencySuggestion
            {
                Code = "product:" + p.Id,
                Title = p.Title,
                Kind = "Product"
            })
            .Take(take)
            .ToListAsync(ct);

        var services = await _db.CensusServices.AsNoTracking()
            .Where(s => s.RemovedAt == null && s.Name.ToLower().Contains(needle))
            .OrderBy(s => s.Name)
            .Select(s => new CensusDependencySuggestion
            {
                Code = "service:" + s.Id,
                Title = s.Name,
                Kind = "Service"
            })
            .Take(take)
            .ToListAsync(ct);

        return products.Concat(services)
            .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Kind)
            .Take(take)
            .ToList();
    }

    public async Task<ServiceCensusPromptViewModel?> GetPromptAsync(string userEmail, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return null;

        var email = userEmail.Trim().ToLowerInvariant();
        var named = await _db.CMDBProductContacts.AsNoTracking()
            .Where(c => c.UserEmail != null && c.UserEmail.ToLower() == email
                && c.CMDBProduct.Status == CMDBProductStatus.Active
                && !c.CMDBProduct.IsEnterpriseService)
            .Select(c => new { c.CMDBProductId, c.CMDBProduct.Title })
            .ToListAsync(ct);
        var censusNamed = await _db.ServiceCensusContactReviews.AsNoTracking()
            .Where(r => r.CmdbEmail != null && r.CmdbEmail.ToLower() == email
                && r.Response.Product.Status == CMDBProductStatus.Active
                && !r.Response.Product.IsEnterpriseService)
            .Select(r => new { CMDBProductId = r.Response.ProductId, r.Response.Product.Title })
            .ToListAsync(ct);
        named = named.Concat(censusNamed).ToList();
        if (named.Count == 0)
            return null;

        var productIds = named.Select(p => p.CMDBProductId).Distinct().ToList();
        var submitted = await _db.ServiceCensusResponses.AsNoTracking()
            .Where(r => productIds.Contains(r.ProductId) && r.Status == ServiceCensusStatuses.Submitted)
            .Select(r => r.ProductId)
            .ToListAsync(ct);
        var outstanding = named
            .GroupBy(p => p.CMDBProductId)
            .Where(g => !submitted.Contains(g.Key))
            .Select(g => g.First())
            .OrderBy(p => p.Title)
            .ToList();
        if (outstanding.Count == 0)
            return null;

        return new ServiceCensusPromptViewModel
        {
            OutstandingCount = outstanding.Count,
            SingleProductId = outstanding.Count == 1 ? outstanding[0].CMDBProductId : null,
            SingleProductTitle = outstanding.Count == 1 ? outstanding[0].Title : null
        };
    }

    private IQueryable<CMDBProduct> VisibleProductsQuery() =>
        _db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active && !p.IsEnterpriseService);

    private Task<CMDBProduct?> LoadProductAsync(Guid id, CancellationToken ct, bool tracking = true)
    {
        IQueryable<CMDBProduct> query = _db.CMDBProducts
            .Include(p => p.Phase)
            .Include(p => p.BusinessAreas).ThenInclude(ba => ba.FipsBusinessArea)
            .Include(p => p.Directorates).ThenInclude(d => d.FipsDirectorate).ThenInclude(fd => fd.DirectorateLookup)
            .Include(p => p.Channels).ThenInclude(c => c.FipsChannel)
            .Include(p => p.UserGroups).ThenInclude(ug => ug.FipsUserGroup)
            .Include(p => p.Types).ThenInclude(t => t.FipsType)
            .Include(p => p.Objectives).ThenInclude(o => o.Objective)
            .Include(p => p.Missions).ThenInclude(m => m.Mission)
            .Include(p => p.RiskAppetiteLookup)
            .Include(p => p.Contacts).ThenInclude(c => c.FipsContactRole);
        if (!tracking)
            query = query.AsNoTracking();
        return query.FirstOrDefaultAsync(
            p => p.Id == id && p.Status == CMDBProductStatus.Active && !p.IsEnterpriseService,
            ct);
    }

    private async Task<ServiceCensusResponse> EnsureResponseAsync(CMDBProduct product, string userEmail, CancellationToken ct)
    {
        var response = await _db.ServiceCensusResponses
            .Include(r => r.Tasks).ThenInclude(t => t.Answers)
            .Include(r => r.ContactReviews)
            .FirstOrDefaultAsync(r => r.ProductId == product.Id, ct);
        if (response != null) return response;

        response = new ServiceCensusResponse
        {
            ProductId = product.Id,
            CreatedBy = userEmail,
            UpdatedBy = userEmail
        };
        _db.ServiceCensusResponses.Add(response);
        await SeedFromRegisterAsync(response, product.Id, userEmail, ct);
        await SaveCensusAsync(ct);
        return response;
    }

    private async Task SeedFromRegisterAsync(ServiceCensusResponse response, Guid productId, string userEmail, CancellationToken ct)
    {
        var entries = await _db.CensusEntries.AsNoTracking()
            .Where(e => e.ProductId == productId && e.RemovedAt == null)
            .OrderBy(e => e.SortOrder)
            .ToListAsync(ct);

        foreach (var task in CensusDefinition.Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.RegisterDomainKey)) continue;
            var matches = entries
                .Where(e => string.Equals(e.DomainKey, task.RegisterDomainKey, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (matches.Count == 0) continue;

            var state = new ServiceCensusTaskState
            {
                Response = response,
                TaskKey = task.Key,
                Status = ServiceCensusStatuses.InProgress,
                UpdatedBy = userEmail
            };
            response.Tasks.Add(state);

            if (task.Kind == CensusTaskKind.LongText
                || ((task.Key == "service" || task.Key == "service-line") && task.Kind == CensusTaskKind.Form))
            {
                var text = matches[0].Narrative ?? matches[0].Title;
                if (task.Key == "service")
                {
                    response.ServiceDescription = text;
                    state.Answers.Add(new ServiceCensusAnswer { Title = "name", Narrative = text });
                }
                else if (task.Key == "service-line")
                {
                    var name = matches[0].Title ?? text;
                    response.ServiceLineDescription = name;
                    state.Answers.Add(new ServiceCensusAnswer { Title = "name", Narrative = name });
                    if (!string.IsNullOrWhiteSpace(matches[0].Narrative)
                        && !string.Equals(matches[0].Narrative, name, StringComparison.Ordinal))
                        state.Answers.Add(new ServiceCensusAnswer { Title = "description", Narrative = matches[0].Narrative });
                }
                else
                {
                    state.Answers.Add(new ServiceCensusAnswer { Narrative = text });
                }
            }
            else if (task.Kind == CensusTaskKind.Accessibility)
            {
                response.AccessibilityNotEnrolledReason = matches[0].Narrative;
            }
            else
            {
                var order = 0;
                foreach (var match in matches)
                {
                    state.Answers.Add(new ServiceCensusAnswer
                    {
                        Title = match.Title,
                        Narrative = match.Narrative,
                        LookupCode = match.LookupCode,
                        SecondaryLookupCode = match.SecondaryLookupCode,
                        ExternalUrl = match.ExternalUrl,
                        SortOrder = order++
                    });
                }
            }
        }
    }

    private static string CensusContactRoleName(string roleKey, string? extraName)
    {
        var defined = CensusDefinition.ContactRoles.FirstOrDefault(r =>
            string.Equals(r.Key, roleKey, StringComparison.OrdinalIgnoreCase));
        if (defined != null) return defined.Name;
        var extra = (extraName ?? "").Trim();
        return extra.Length > 0 ? extra : "Additional contact";
    }

    private static void AddRolePerson(
        Dictionary<Guid, Dictionary<string, List<string>>> peopleByProduct,
        Guid productId,
        string? role,
        string? person)
    {
        var roleName = (role ?? "").Trim();
        var name = (person ?? "").Trim();
        if (roleName.Length == 0 || name.Length == 0) return;
        if (!peopleByProduct.TryGetValue(productId, out var roles))
        {
            roles = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            peopleByProduct[productId] = roles;
        }
        if (!roles.TryGetValue(roleName, out var names))
        {
            names = [];
            roles[roleName] = names;
        }
        if (!names.Any(existing => string.Equals(existing, name, StringComparison.OrdinalIgnoreCase)))
            names.Add(name);
    }

    private static List<ServiceCensusNamedPersonViewModel> NamedSubmitters(
        CMDBProduct product, ServiceCensusResponse response)
    {
        var people = new List<ServiceCensusNamedPersonViewModel>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in BuildContacts(product, response))
        {
            var name = (row.DisplayName ?? "").Trim();
            var email = (row.DisplayEmail ?? "").Trim();
            if (name.Length == 0 && email.Length == 0) continue;
            var key = email.Length > 0 ? email : name + "|" + row.RoleKey;
            if (!seen.Add(key)) continue;
            people.Add(new ServiceCensusNamedPersonViewModel
            {
                Name = name,
                Email = email,
                RoleName = row.RoleName
            });
        }
        return people;
    }

    private static bool UserIsNamedSubmitter(IReadOnlyList<ServiceCensusNamedPersonViewModel> people, string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        var actor = email.Trim();
        return people.Any(p =>
            !string.IsNullOrWhiteSpace(p.Email)
            && string.Equals(p.Email.Trim(), actor, StringComparison.OrdinalIgnoreCase));
    }

    private Task<bool> UserIsOpsSubmitterAsync(string email) =>
        _permissions.IsOperationConsoleUserAsync(email);

    /// <summary>
    /// Named contacts may submit only when every section is complete.
    /// Ops/admin (operation console) may submit even when some sections are incomplete.
    /// </summary>
    private static bool CanUserSubmitReturn(bool userIsNamed, bool opsCanSubmit, bool allComplete, bool submitted) =>
        !submitted && (opsCanSubmit || (userIsNamed && allComplete));

    private static string SubmitBlockedReason(
        bool submitted, bool allComplete, bool userIsNamed, bool opsCanSubmit, int namedCount)
    {
        if (submitted) return "This census has already been submitted.";
        if (opsCanSubmit) return "";
        var parts = new List<string>();
        if (!allComplete) parts.Add("All sections must be complete.");
        if (namedCount == 0) parts.Add("Name people on the contacts page before this census can be submitted.");
        else if (!userIsNamed) parts.Add("Only one of the people listed can submit this census.");
        return string.Join(" ", parts);
    }

    private static bool IsCensusSubmitted(string? status, DateTime? submittedAt) =>
        submittedAt.HasValue
        || string.Equals((status ?? "").Trim(), ServiceCensusStatuses.Submitted, StringComparison.OrdinalIgnoreCase);

    private async Task PersistSubmittedStatusAsync(
        ServiceCensusResponse response, string userEmail, CancellationToken ct)
    {
        var at = DateTime.UtcNow;
        response.Status = ServiceCensusStatuses.Submitted;
        response.SubmittedAt = at;
        response.SubmittedBy = userEmail;
        response.UpdatedAt = at;
        response.UpdatedBy = userEmail;

        var updated = await _db.ServiceCensusResponses
            .Where(r => r.Id == response.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(r => r.Status, ServiceCensusStatuses.Submitted)
                .SetProperty(r => r.SubmittedAt, at)
                .SetProperty(r => r.SubmittedBy, userEmail)
                .SetProperty(r => r.UpdatedAt, at)
                .SetProperty(r => r.UpdatedBy, userEmail), ct);

        if (updated == 0)
            await SaveCensusAsync(ct);
    }

    private async Task<HashSet<Guid>> CensusSubmissionDeclarationProductIdsAsync(
        IReadOnlyCollection<Guid> productIds, CancellationToken ct)
    {
        if (productIds.Count == 0)
            return [];

        var ids = await _db.CensusSectionDeclarations.AsNoTracking()
            .Where(d => productIds.Contains(d.ProductId)
                && d.SectionKey.StartsWith("census-")
                && d.StatusCode == "COMPLETE")
            .Select(d => d.ProductId)
            .Distinct()
            .ToListAsync(ct);
        return ids.ToHashSet();
    }

    private async Task<bool> HasCensusSubmissionDeclarationAsync(Guid productId, CancellationToken ct) =>
        await _db.CensusSectionDeclarations.AsNoTracking()
            .AnyAsync(d => d.ProductId == productId
                && d.SectionKey.StartsWith("census-")
                && d.StatusCode == "COMPLETE", ct);

    private async Task<bool> EnsureSubmittedStateAsync(
        ServiceCensusResponse response, Guid productId, string userEmail, CancellationToken ct)
    {
        if (IsCensusSubmitted(response.Status, response.SubmittedAt))
            return true;
        if (!await HasCensusSubmissionDeclarationAsync(productId, ct))
            return false;
        await PersistSubmittedStatusAsync(response, userEmail, ct);
        return true;
    }

    private async Task ClearCensusSubmissionDeclarationsAsync(Guid productId, CancellationToken ct) =>
        await _db.CensusSectionDeclarations
            .Where(d => d.ProductId == productId && d.SectionKey.StartsWith("census-"))
            .ExecuteDeleteAsync(ct);

    private static string StatusOf(ServiceCensusResponse response, string taskKey) =>
        StateOf(response, taskKey)?.Status ?? ServiceCensusStatuses.NotStarted;

    private static ServiceCensusTaskState? StateOf(ServiceCensusResponse response, string taskKey) =>
        response.Tasks.FirstOrDefault(t => string.Equals(t.TaskKey, taskKey, StringComparison.OrdinalIgnoreCase));

    private static ServiceCensusTaskSummaryViewModel Summary(CensusTask task, string status) => new()
    {
        Key = task.Key,
        Heading = task.Heading,
        Help = task.Help,
        Status = status,
        StatusLabel = StatusLabel(status)
    };

    private static string StatusLabel(string status) => status switch
    {
        ServiceCensusStatuses.Complete => "Completed",
        ServiceCensusStatuses.InProgress => "In progress",
        _ => "Not started"
    };

    private static List<ServiceCensusNavItemViewModel> NavFor(ServiceCensusResponse response, string? current) =>
        CensusDefinition.Tasks.Select(t => new ServiceCensusNavItemViewModel
        {
            Key = t.Key,
            GroupName = t.GroupName,
            Heading = t.NavLabel,
            Status = StatusOf(response, t.Key),
            StatusLabel = StatusLabel(StatusOf(response, t.Key)),
            Current = string.Equals(t.Key, current, StringComparison.OrdinalIgnoreCase)
        }).ToList();

    private static ServiceCensusQuestionViewModel BaseQuestion(
        CMDBProduct product, CensusTask task, ServiceCensusResponse response, ServiceCensusTaskState? state, bool showAll)
    {
        var status = state?.Status ?? ServiceCensusStatuses.NotStarted;
        return new ServiceCensusQuestionViewModel
        {
            ProductId = product.Id,
            ProductTitle = product.Title,
            ShowAll = showAll,
            TaskKey = task.Key,
            GroupName = task.GroupName,
            Heading = task.Heading,
            Help = task.Help,
            Kind = task.Kind,
            LookupControl = task.LookupControl,
            TitleLabel = task.TitleLabel,
            NarrativeLabel = task.NarrativeLabel,
            ItemNoun = task.ItemNoun,
            AddLabel = task.AddLabel,
            EmptyLabel = task.EmptyLabel,
            CaptureUrl = task.CaptureUrl,
            UrlLabel = task.UrlLabel,
            UrlHint = task.UrlHint,
            PickFromRegister = task.PickFromRegister,
            FormFields = task.FormFields.ToList(),
            Status = status,
            CannotAnswer = state?.CannotAnswer == true,
            CannotAnswerReason = state?.CannotAnswerReason,
            PreviousTaskKey = CensusDefinition.Previous(task.Key)?.Key,
            NextTaskKey = CensusDefinition.Next(task.Key)?.Key,
            Submitted = response.Status == ServiceCensusStatuses.Submitted,
            Nav = NavFor(response, task.Key)
        };
    }

    private static List<ServiceCensusContactRowViewModel> BuildContacts(CMDBProduct product, ServiceCensusResponse response)
    {
        var reviews = response.ContactReviews.ToDictionary(r => r.RoleKey, StringComparer.OrdinalIgnoreCase);
        var usedCmdb = new HashSet<int>();
        var usedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<ServiceCensusContactRowViewModel>();

        foreach (var role in CensusDefinition.ContactRoles)
        {
            var cmdb = MatchContact(product, role);
            if (cmdb != null) usedCmdb.Add(cmdb.Id);
            reviews.TryGetValue(role.Key, out var review);
            rows.Add(ContactRow(role.Key, role.Name, role.Hint, cmdb, review, isAdded: false));
            usedKeys.Add(role.Key);
        }

        foreach (var contact in product.Contacts ?? [])
        {
            if (usedCmdb.Contains(contact.Id)) continue;
            var key = "cmdb-" + contact.Id;
            reviews.TryGetValue(key, out var review);
            rows.Add(ContactRow(
                key,
                contact.FipsContactRole?.Name ?? "Contact",
                "",
                contact,
                review,
                isAdded: true));
            usedKeys.Add(key);
        }

        foreach (var review in response.ContactReviews)
        {
            if (usedKeys.Contains(review.RoleKey)) continue;
            rows.Add(ContactRow(
                review.RoleKey,
                string.IsNullOrWhiteSpace(review.IncorrectReason) ? "Additional contact" : review.IncorrectReason,
                "",
                null,
                review,
                isAdded: true));
        }

        return rows;
    }

    private static ServiceCensusContactRowViewModel ContactRow(
        string roleKey, string roleName, string roleHint, CMDBProductContact? cmdb, ServiceCensusContactReview? review, bool isAdded)
    {
        var reviewHasPerson = review != null
            && (!string.IsNullOrWhiteSpace(review.CmdbName) || !string.IsNullOrWhiteSpace(review.CmdbEmail));
        var selectedName = reviewHasPerson ? review!.CmdbName : cmdb?.UserName;
        var selectedEmail = reviewHasPerson ? review!.CmdbEmail : cmdb?.UserEmail;
        return new ServiceCensusContactRowViewModel
        {
            RoleKey = roleKey,
            RoleName = roleName,
            RoleHint = roleHint,
            CmdbName = cmdb?.UserName,
            CmdbEmail = cmdb?.UserEmail,
            OriginalName = cmdb?.UserName,
            OriginalEmail = cmdb?.UserEmail,
            SelectedName = selectedName,
            SelectedEmail = selectedEmail,
            IsAdded = isAdded,
            IsIncorrect = review?.IsIncorrect == true
        };
    }

    private static CMDBProductContact? MatchContact(CMDBProduct product, CensusContactRole role)
    {
        var contacts = product.Contacts ?? [];
        foreach (var contact in contacts)
        {
            var name = (contact.FipsContactRole?.Name ?? "").Trim().ToLowerInvariant();
            if (role.NameAliases.Any(alias => name.Contains(alias, StringComparison.OrdinalIgnoreCase)))
                return contact;
        }
        return null;
    }

    private async Task PopulateAccessibilityAsync(
        ServiceCensusQuestionViewModel vm, CMDBProduct product, ServiceCensusResponse response, CancellationToken ct)
    {
        try
        {
            var aiss = await _aiss.LoadForProductAsync(product.UniqueID, product.Id, 5, ct);
            vm.AccessibilityEnrolled = aiss.IsOnboarded;
            vm.AccessibilityPending = !aiss.NotFound && !aiss.IsOnboarded;
            vm.AccessibilityStatusLabel = aiss.IsOnboarded
                ? (aiss.OpenIssueCount > 0
                    ? $"Enrolled — {aiss.OpenIssueCount} open issue{(aiss.OpenIssueCount == 1 ? "" : "s")}"
                    : "Enrolled — no open issues")
                : aiss.NotFound
                    ? "Not enrolled"
                    : "Pending enrolment";
            vm.AccessibilityOpenIssues = aiss.OpenIssueCount;
        }
        catch
        {
            vm.AccessibilityEnrolled = false;
            vm.AccessibilityStatusLabel = "Enrolment could not be checked";
        }

        vm.AccessibilityNotEnrolledReason = response.AccessibilityNotEnrolledReason;
        vm.AccessibilityEnrolUrl = CensusDefinition.AccessibilityEnrolUrl;
    }

    private async Task<bool> ProductIsAissEnrolledAsync(CMDBProduct product, CancellationToken ct)
    {
        try
        {
            var aiss = await _aiss.LoadForProductAsync(product.UniqueID, product.Id, 5, ct);
            return aiss.IsOnboarded;
        }
        catch
        {
            return false;
        }
    }

    private async Task MarkAccessibilityEnrolledCompleteAsync(
        ServiceCensusResponse response, string userEmail, CancellationToken ct)
    {
        var state = StateOf(response, "accessibility");
        if (state == null)
        {
            state = await _db.ServiceCensusTaskStates
                .Include(t => t.Answers)
                .FirstOrDefaultAsync(
                    t => t.ResponseId == response.Id && t.TaskKey == "accessibility",
                    ct);
            if (state != null)
                response.Tasks.Add(state);
        }

        if (state == null)
        {
            state = new ServiceCensusTaskState
            {
                ResponseId = response.Id,
                TaskKey = "accessibility",
                Status = ServiceCensusStatuses.Complete,
                UpdatedAt = DateTime.UtcNow,
                UpdatedBy = userEmail
            };
            response.Tasks.Add(state);
            _db.ServiceCensusTaskStates.Add(state);
            state.Answers.Add(new ServiceCensusAnswer { Title = "enrolled" });
        }
        else
        {
            state.CannotAnswer = false;
            state.CannotAnswerReason = null;
            state.Status = ServiceCensusStatuses.Complete;
            state.UpdatedAt = DateTime.UtcNow;
            state.UpdatedBy = userEmail;
            if (state.Answers.Count == 0)
                state.Answers.Add(new ServiceCensusAnswer { Title = "enrolled" });
        }

        response.AccessibilityNotEnrolledReason = null;
        try
        {
            await SaveCensusAsync(ct);
        }
        catch
        {
            // Keep the in-memory complete status for this request even if persist fails.
        }
    }

    private static bool IsCensusEntity(object entity) =>
        entity is ServiceCensusResponse
            or ServiceCensusTaskState
            or ServiceCensusAnswer
            or ServiceCensusContactReview;

    private void DetachNonCensus()
    {
        foreach (var entry in _db.ChangeTracker.Entries().Where(e => !IsCensusEntity(e.Entity)).ToList())
            entry.State = EntityState.Detached;
    }

    private async Task SaveCensusAsync(CancellationToken ct)
    {
        DetachNonCensus();
        for (var attempt = 0; attempt < 4; attempt++)
        {
            try
            {
                await _db.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException ex)
            {
                foreach (var entry in ex.Entries.ToList())
                {
                    if (entry.State == EntityState.Added)
                        continue;
                    try
                    {
                        var database = await entry.GetDatabaseValuesAsync(ct);
                        if (database == null || entry.State == EntityState.Deleted)
                        {
                            entry.State = EntityState.Detached;
                            continue;
                        }

                        entry.OriginalValues.SetValues(database);
                    }
                    catch
                    {
                        entry.State = EntityState.Detached;
                    }
                }

                DetachNonCensus();
            }
        }

        foreach (var entry in _db.ChangeTracker.Entries().ToList())
        {
            if (entry.State is EntityState.Deleted or EntityState.Modified
                && entry.Entity is not ServiceCensusAnswer
                && entry.Entity is not ServiceCensusResponse
                && entry.Entity is not ServiceCensusTaskState)
                entry.State = EntityState.Detached;
        }

        DetachNonCensus();
        await _db.SaveChangesAsync(ct);
    }

    private static bool HasItemList(CensusTask task) =>
        task.Kind == CensusTaskKind.ItemList;

    private static string ItemListRequiredMessage(CensusTask task)
    {
        var noun = string.IsNullOrWhiteSpace(task.ItemNoun) ? "item" : task.ItemNoun;
        return "Add at least one " + noun + ", or say you cannot answer this.";
    }

    private static string? Validate(CensusTask task, ServiceCensusSaveRequest request, string intent)
    {
        var saveOrContinue = intent is "save" or "complete";
        if (saveOrContinue && request.CannotAnswer)
            return string.IsNullOrWhiteSpace(request.CannotAnswerReason)
                ? "Explain why you cannot answer this question."
                : null;

        if (saveOrContinue && HasItemList(task) && !request.Items.Any(HasItem))
            return ItemListRequiredMessage(task);

        if (intent is not "complete") return null;

        return task.Kind switch
        {
            CensusTaskKind.LongText when string.IsNullOrWhiteSpace(request.LongText)
                => "Enter a description, or say you cannot answer this.",
            CensusTaskKind.YesChoice when string.IsNullOrWhiteSpace(request.YesNo)
                => "Choose yes or no, or say you cannot answer this.",
            CensusTaskKind.YesChoice when Yes(request.YesNo) && PostedChoiceValues(request).Count == 0
                => "Choose the kind of enabling functionality.",
            CensusTaskKind.Lookup when !HasLookupSelection(task, request)
                => task.LookupControl == CensusLookupControl.Checkbox
                    ? "Select at least one option, or say you cannot answer this."
                    : "Choose an option, or say you cannot answer this.",
            CensusTaskKind.Accessibility when !string.Equals(request.YesNo, "enrolled", StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(request.AccessibilityNotEnrolledReason)
                => "Give a reason why this product is not enrolled.",
            CensusTaskKind.Contacts when request.Contacts.Any(c =>
                    c.IsAdded && HasSelectedPerson(c) && string.IsNullOrWhiteSpace(c.RoleName))
                => "Give a role for each extra contact you add.",
            CensusTaskKind.Form => ValidateForm(task, request),
            _ => null
        };
    }

    private static bool HasContent(CensusTask task, ServiceCensusSaveRequest request, ServiceCensusTaskState state)
    {
        if (request.CannotAnswer) return true;
        return task.Kind switch
        {
            CensusTaskKind.LongText => !string.IsNullOrWhiteSpace(request.LongText),
            CensusTaskKind.YesChoice => !string.IsNullOrWhiteSpace(request.YesNo),
            CensusTaskKind.Lookup => HasLookupSelection(task, request) || !string.IsNullOrWhiteSpace(request.LongText),
            CensusTaskKind.ItemList => request.Items.Any(HasItem)
                || (!request.ItemListCleared && state.Answers.Any(HasStoredAnswer)),
            CensusTaskKind.Accessibility => !string.IsNullOrWhiteSpace(request.AccessibilityNotEnrolledReason) || !string.IsNullOrWhiteSpace(request.YesNo),
            CensusTaskKind.Contacts => request.Contacts.Any(HasSelectedPerson),
            CensusTaskKind.Form => task.FormFields.Any(f =>
                !string.IsNullOrWhiteSpace(FormValue(request, f.Key))
                || !string.IsNullOrWhiteSpace(FormNarrative(request, f.Key)))
                || request.Items.Any(HasItem)
                || (!request.ItemListCleared && state.Answers.Any(HasStoredAnswer)),
            _ => false
        };
    }

    private static bool HasItem(ServiceCensusAnswerRowViewModel item) =>
        !string.IsNullOrWhiteSpace(item.Title) || !string.IsNullOrWhiteSpace(item.Narrative)
        || !string.IsNullOrWhiteSpace(item.ExternalUrl) || !string.IsNullOrWhiteSpace(item.LookupCode);

    private static bool HasStoredAnswer(ServiceCensusAnswer answer) =>
        !string.IsNullOrWhiteSpace(answer.Title) || !string.IsNullOrWhiteSpace(answer.Narrative) || !string.IsNullOrWhiteSpace(answer.ExternalUrl);

    private static bool Yes(string? value) =>
        string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);

    private async Task ApplyAnswersAsync(
        CensusTask task, ServiceCensusResponse response, ServiceCensusTaskState state, ServiceCensusSaveRequest request, CancellationToken ct)
    {
        if (request.CannotAnswer)
        {
            await PersistTaskAnswersAsync(state, [], ct);
            return;
        }

        switch (task.Kind)
        {
            case CensusTaskKind.LongText:
                var text = Trim(request.LongText, task.Key == "aim" ? 10000 : 4000);
                if (task.Key == "service") response.ServiceDescription = text;
                if (task.Key == "service-line") response.ServiceLineDescription = text;
                await PersistTaskAnswersAsync(state, string.IsNullOrWhiteSpace(text) ? [] : [
                    new ServiceCensusAnswer { Title = task.Heading, Narrative = text }
                ], ct);
                if (task.Key == "aim" && !string.IsNullOrWhiteSpace(text))
                {
                    await _db.CMDBProducts
                        .Where(p => p.Id == response.ProductId)
                        .ExecuteUpdateAsync(s => s
                            .SetProperty(p => p.UserDescription, text)
                            .SetProperty(p => p.UpdatedAt, DateTime.UtcNow), ct);
                }
                break;
            case CensusTaskKind.YesChoice:
                await PersistTaskAnswersAsync(state, BuildYesChoiceAnswers(task, request), ct);
                break;
            case CensusTaskKind.Accessibility:
                if (string.Equals(request.YesNo, "enrolled", StringComparison.OrdinalIgnoreCase))
                    break;
                response.AccessibilityNotEnrolledReason = Trim(request.AccessibilityNotEnrolledReason, 4000);
                await PersistTaskAnswersAsync(state, [
                    new ServiceCensusAnswer
                    {
                        Title = string.IsNullOrWhiteSpace(request.YesNo) ? "Not enrolled" : request.YesNo,
                        Narrative = response.AccessibilityNotEnrolledReason
                    }
                ], ct);
                break;
            case CensusTaskKind.Contacts:
                await ApplyContactsAsync(response, request.Contacts, ct);
                break;
            case CensusTaskKind.Lookup:
                await PersistTaskAnswersAsync(state, BuildLookupAnswers(task, request), ct);
                break;
            case CensusTaskKind.Form:
                var keptFormItems = request.Items.Any(HasItem)
                    ? request.Items.Where(HasItem).ToList()
                    : request.ItemListCleared
                        ? []
                        : state.Answers
                            .Where(a => !task.FormFields.Any(f =>
                                string.Equals(f.Key, a.Title, StringComparison.OrdinalIgnoreCase)))
                            .Select(a => new ServiceCensusAnswerRowViewModel
                            {
                                Title = a.Title,
                                ExternalUrl = a.ExternalUrl
                            })
                            .ToList();
                await PersistTaskAnswersAsync(state, BuildFormAnswers(task, request, keptFormItems), ct);
                if (task.Key == "service")
                    response.ServiceDescription = Trim(FormValue(request, "name"), 4000);
                if (task.Key == "service-line")
                    response.ServiceLineDescription = Trim(FormValue(request, "name"), 4000);
                break;
            default:
                if (request.Items.Any(HasItem) || request.ItemListCleared)
                    await PersistTaskAnswersAsync(state, BuildItemAnswers(request.Items), ct);
                break;
        }
    }

    private static string? FormValue(ServiceCensusSaveRequest request, string key) =>
        request.FormValues.TryGetValue(key, out var value) ? value : null;

    private static string? FormNarrative(ServiceCensusSaveRequest request, string key) =>
        request.FormNarratives.TryGetValue(key, out var value) ? value : null;

    private static string? ValidateForm(CensusTask task, ServiceCensusSaveRequest request)
    {
        foreach (var field in task.FormFields)
        {
            var value = FormValue(request, field.Key)?.Trim();
            var narrative = FormNarrative(request, field.Key)?.Trim();
            if (field.Kind == CensusFormFieldKind.Radio && string.IsNullOrWhiteSpace(value))
                return "Answer: " + field.Label;
            if (field.Kind == CensusFormFieldKind.Radio
                && !string.IsNullOrWhiteSpace(field.RevealNarrativeFor)
                && string.Equals(value, field.RevealNarrativeFor, StringComparison.OrdinalIgnoreCase)
                && string.IsNullOrWhiteSpace(narrative))
                return field.NarrativeLabel ?? "Explain where you record them.";
            if ((field.Kind == CensusFormFieldKind.LongText || field.Kind == CensusFormFieldKind.Text)
                && string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(narrative))
                return "Answer: " + field.Label;
            if (field.Kind == CensusFormFieldKind.Radio)
            {
                foreach (var revealed in field.ConditionalChoices)
                {
                    if (!string.Equals(value, revealed.WhenValue, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var allowed = revealed.Choices.Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var selected = PostedChoiceValues(request)
                        .Where(v => allowed.Contains(v))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .ToList();
                    if (selected.Count < revealed.Minimum)
                        return revealed.Hint ?? "Select the options that apply.";
                }
            }
        }
        return null;
    }

    private static List<ServiceCensusAnswer> BuildFormAnswers(
        CensusTask task,
        ServiceCensusSaveRequest request,
        List<ServiceCensusAnswerRowViewModel> items)
    {
        var answers = new List<ServiceCensusAnswer>();
        var order = 0;
        foreach (var field in task.FormFields)
        {
            // LongText / Text post into FormValues; do not apply the radio LookupCode length (300) to them.
            var valueMax = field.Kind switch
            {
                CensusFormFieldKind.LongText => 10000,
                CensusFormFieldKind.Text => 400,
                _ => 300
            };
            var value = Trim(FormValue(request, field.Key), valueMax);
            var narrative = Trim(FormNarrative(request, field.Key), 10000);
            if (field.Kind == CensusFormFieldKind.LongText || field.Kind == CensusFormFieldKind.Text)
            {
                narrative = Trim(value ?? narrative, valueMax);
                value = field.Key;
            }
            if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(narrative))
                continue;
            answers.Add(new ServiceCensusAnswer
            {
                Title = field.Key,
                LookupCode = field.Kind == CensusFormFieldKind.Radio ? value : null,
                Narrative = field.Kind is CensusFormFieldKind.LongText or CensusFormFieldKind.Text
                    || string.Equals(value, field.RevealNarrativeFor, StringComparison.OrdinalIgnoreCase)
                    ? narrative
                    : field.Options.FirstOrDefault(o =>
                        string.Equals(o.Value, value, StringComparison.OrdinalIgnoreCase))?.Label,
                SortOrder = order++
            });
            if (field.Kind == CensusFormFieldKind.Radio)
            {
                foreach (var revealed in field.ConditionalChoices)
                {
                    if (!string.Equals(value, revealed.WhenValue, StringComparison.OrdinalIgnoreCase))
                        continue;
                    var allowed = revealed.Choices.ToDictionary(c => c.Value, StringComparer.OrdinalIgnoreCase);
                    foreach (var choice in PostedChoiceValues(request))
                    {
                        if (!allowed.TryGetValue(choice, out var option)) continue;
                        answers.Add(new ServiceCensusAnswer
                        {
                            Title = revealed.AnswerTitle,
                            LookupCode = option.Value,
                            Narrative = option.Label,
                            SortOrder = order++
                        });
                    }
                }
            }
        }

        foreach (var item in items.Where(HasItem))
        {
            var url = Trim(item.Title, 500) ?? Trim(item.ExternalUrl, 500);
            if (string.IsNullOrWhiteSpace(url)) continue;
            answers.Add(new ServiceCensusAnswer
            {
                Title = url,
                ExternalUrl = url,
                SortOrder = order++
            });
        }
        return answers;
    }

    private static List<ServiceCensusAnswer> BuildYesChoiceAnswers(CensusTask task, ServiceCensusSaveRequest request)
    {
        var answers = new List<ServiceCensusAnswer>();
        if (string.IsNullOrWhiteSpace(request.YesNo))
            return answers;

        var yes = Yes(request.YesNo);
        var order = 0;
        answers.Add(new ServiceCensusAnswer
        {
            Title = yes ? "Yes" : "No",
            Narrative = yes ? Trim(request.ChoiceNarrative, 10000) : null,
            SortOrder = order++
        });
        if (!yes) return answers;

        foreach (var kind in PostedChoiceValues(request))
        {
            var option = task.ChoiceOptions.FirstOrDefault(o =>
                string.Equals(o.Value, kind, StringComparison.OrdinalIgnoreCase));
            answers.Add(new ServiceCensusAnswer
            {
                Title = option?.Label ?? kind,
                LookupCode = kind,
                SortOrder = order++
            });
        }
        return answers;
    }

    private static List<ServiceCensusAnswer> BuildLookupAnswers(CensusTask task, ServiceCensusSaveRequest request)
    {
        var answers = new List<ServiceCensusAnswer>();
        var order = 0;
        foreach (var id in PostedLookupIds(task, request))
        {
            var code = Trim(id, 80) ?? id;
            answers.Add(new ServiceCensusAnswer
            {
                LookupCode = code,
                Title = id,
                SortOrder = order++
            });
        }

        var detail = Trim(request.LongText, 10000);
        if (!string.IsNullOrWhiteSpace(detail))
        {
            answers.Add(new ServiceCensusAnswer
            {
                Title = "stack-info",
                Narrative = detail,
                SortOrder = order
            });
        }
        return answers;
    }

    private static List<ServiceCensusAnswer> BuildItemAnswers(List<ServiceCensusAnswerRowViewModel> items)
    {
        var answers = new List<ServiceCensusAnswer>();
        var order = 0;
        foreach (var item in items.Where(HasItem))
        {
            var title = Trim(item.Title, 300) ?? Trim(item.Narrative, 300);
            if (string.IsNullOrWhiteSpace(title)) continue;
            answers.Add(new ServiceCensusAnswer
            {
                Title = title,
                Narrative = Trim(item.Narrative, 10000) ?? title,
                ExternalUrl = Trim(item.ExternalUrl, 500),
                LookupCode = Trim(item.LookupCode, 80),
                SortOrder = order++
            });
        }
        return answers;
    }

    private async Task PersistTaskAnswersAsync(
        ServiceCensusTaskState state,
        IReadOnlyList<ServiceCensusAnswer> next,
        CancellationToken ct)
    {
        var stateEntry = _db.Entry(state);
        if (stateEntry.State == EntityState.Detached)
            _db.Attach(state);
        var responseAdded = state.Response != null && _db.Entry(state.Response).State == EntityState.Added;
        if (stateEntry.State == EntityState.Added || responseAdded)
            await _db.SaveChangesAsync(ct);

        var existing = state.Answers.OrderBy(a => a.SortOrder).ToList();
        for (var i = 0; i < next.Count; i++)
        {
            var title = Trim(next[i].Title, 300);
            var narrative = next[i].Narrative;
            var lookup = Trim(next[i].LookupCode, 80);
            var secondary = Trim(next[i].SecondaryLookupCode, 80);
            var url = Trim(next[i].ExternalUrl, 500);
            if (i < existing.Count)
            {
                var row = existing[i];
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE ServiceCensusAnswers
                    SET Title = {title}, Narrative = {narrative}, LookupCode = {lookup},
                        SecondaryLookupCode = {secondary}, ExternalUrl = {url}, SortOrder = {i}
                    WHERE Id = {row.Id}
                    """, ct);
                row.Title = title;
                row.Narrative = narrative;
                row.LookupCode = lookup;
                row.SecondaryLookupCode = secondary;
                row.ExternalUrl = url;
                row.SortOrder = i;
                _db.Entry(row).State = EntityState.Unchanged;
            }
            else
            {
                var id = Guid.NewGuid();
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO ServiceCensusAnswers (Id, TaskStateId, Title, Narrative, LookupCode, SecondaryLookupCode, ExternalUrl, SortOrder)
                    VALUES ({id}, {state.Id}, {title}, {narrative}, {lookup}, {secondary}, {url}, {i})
                    """, ct);
                var saved = new ServiceCensusAnswer
                {
                    Id = id,
                    TaskStateId = state.Id,
                    Title = title,
                    Narrative = narrative,
                    LookupCode = lookup,
                    SecondaryLookupCode = secondary,
                    ExternalUrl = url,
                    SortOrder = i
                };
                state.Answers.Add(saved);
                _db.Entry(saved).State = EntityState.Unchanged;
            }
        }

        var surplus = existing.Skip(next.Count).ToList();
        if (surplus.Count > 0)
        {
            var ids = surplus.Select(a => a.Id).ToList();
            foreach (var row in surplus)
            {
                _db.Entry(row).State = EntityState.Detached;
                state.Answers.Remove(row);
            }
            await _db.ServiceCensusAnswers.Where(a => ids.Contains(a.Id)).ExecuteDeleteAsync(ct);
        }
    }

    private static bool HasSelectedPerson(ServiceCensusContactRowViewModel contact) =>
        !string.IsNullOrWhiteSpace(contact.SelectedName) || !string.IsNullOrWhiteSpace(contact.SelectedEmail);

    private async Task ApplyContactsAsync(ServiceCensusResponse response, List<ServiceCensusContactRowViewModel> contacts, CancellationToken ct)
    {
        var defined = CensusDefinition.ContactRoles.Select(r => r.Key).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var keepKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var posted in contacts)
        {
            var roleKey = Trim(posted.RoleKey, 80);
            if (string.IsNullOrEmpty(roleKey)) continue;
            var isDefined = defined.Contains(roleKey);
            var isAdded = posted.IsAdded || !isDefined;
            if (isAdded && !HasSelectedPerson(posted) && string.IsNullOrWhiteSpace(posted.RoleName))
                continue;

            var selectedName = Trim(posted.SelectedName, 200);
            var selectedEmail = Trim(posted.SelectedEmail, 320);
            var originalEmail = Trim(posted.OriginalEmail, 320);
            var changed = !string.Equals(selectedEmail ?? "", originalEmail ?? "", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Trim(posted.SelectedName, 200) ?? "", Trim(posted.OriginalName, 200) ?? "", StringComparison.OrdinalIgnoreCase);
            var isIncorrect = !isAdded && changed && (HasSelectedPerson(posted) || !string.IsNullOrWhiteSpace(originalEmail));
            var incorrectReason = isAdded ? Trim(posted.RoleName, 200) : null;

            var existing = response.ContactReviews.FirstOrDefault(r =>
                string.Equals(r.RoleKey, roleKey, StringComparison.OrdinalIgnoreCase));
            if (existing == null)
            {
                var id = Guid.NewGuid();
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO ServiceCensusContactReviews (Id, ResponseId, RoleKey, CmdbName, CmdbEmail, IsIncorrect, IncorrectReason)
                    VALUES ({id}, {response.Id}, {roleKey}, {selectedName}, {selectedEmail}, {isIncorrect}, {incorrectReason})
                    """, ct);
                var saved = new ServiceCensusContactReview
                {
                    Id = id,
                    ResponseId = response.Id,
                    RoleKey = roleKey,
                    CmdbName = selectedName,
                    CmdbEmail = selectedEmail,
                    IsIncorrect = isIncorrect,
                    IncorrectReason = incorrectReason
                };
                response.ContactReviews.Add(saved);
                _db.Entry(saved).State = EntityState.Unchanged;
            }
            else
            {
                await _db.Database.ExecuteSqlInterpolatedAsync($"""
                    UPDATE ServiceCensusContactReviews
                    SET CmdbName = {selectedName}, CmdbEmail = {selectedEmail}, IsIncorrect = {isIncorrect}, IncorrectReason = {incorrectReason}
                    WHERE Id = {existing.Id}
                    """, ct);
                existing.CmdbName = selectedName;
                existing.CmdbEmail = selectedEmail;
                existing.IsIncorrect = isIncorrect;
                existing.IncorrectReason = incorrectReason;
                _db.Entry(existing).State = EntityState.Unchanged;
            }
            keepKeys.Add(roleKey);
        }

        var surplus = response.ContactReviews
            .Where(r => !keepKeys.Contains(r.RoleKey) && !defined.Contains(r.RoleKey))
            .ToList();
        if (surplus.Count > 0)
        {
            var ids = surplus.Select(r => r.Id).ToList();
            foreach (var row in surplus)
            {
                _db.Entry(row).State = EntityState.Detached;
                response.ContactReviews.Remove(row);
            }
            await _db.ServiceCensusContactReviews.Where(r => ids.Contains(r.Id)).ExecuteDeleteAsync(ct);
        }
    }

    private async Task WriteToRegisterAsync(
        CMDBProduct product, ServiceCensusResponse response, string userEmail, CancellationToken ct)
    {
        var productId = product.Id;
        var domainKeys = CensusDefinition.Tasks
            .Select(t => t.RegisterDomainKey)
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Select(k => k!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existing = await _db.CensusEntries
            .Where(e => e.ProductId == productId && e.RemovedAt == null && domainKeys.Contains(e.DomainKey))
            .ToListAsync(ct);
        foreach (var entry in existing)
        {
            entry.RemovedAt = DateTime.UtcNow;
            entry.RemovedBy = userEmail;
            entry.RemovalReason = "Replaced by service census submission";
        }

        foreach (var task in CensusDefinition.Tasks)
        {
            if (string.IsNullOrWhiteSpace(task.RegisterDomainKey)) continue;
            var state = StateOf(response, task.Key);
            if (state == null) continue;

            if (state.CannotAnswer)
            {
                _db.CensusEntries.Add(new CensusEntry
                {
                    ProductId = productId,
                    DomainKey = task.RegisterDomainKey,
                    Title = "Cannot answer",
                    Narrative = state.CannotAnswerReason,
                    VerificationStatusCode = "UNVERIFIED",
                    CreatedBy = userEmail,
                    UpdatedBy = userEmail
                });
                continue;
            }

            if (task.Kind == CensusTaskKind.Contacts)
            {
                var order = 0;
                foreach (var review in response.ContactReviews)
                {
                    var role = CensusDefinition.ContactRoles.FirstOrDefault(r =>
                        string.Equals(r.Key, review.RoleKey, StringComparison.OrdinalIgnoreCase));
                    var title = role?.Name ?? review.IncorrectReason ?? review.RoleKey;
                    var added = role == null;
                    _db.CensusEntries.Add(new CensusEntry
                    {
                        ProductId = productId,
                        DomainKey = task.RegisterDomainKey,
                        Title = title,
                        PersonName = review.CmdbName,
                        PersonEmail = review.CmdbEmail,
                        LookupCode = added ? "ADDED" : review.IsIncorrect ? "CHANGED" : "CONFIRMED",
                        Narrative = added ? review.IncorrectReason : null,
                        SortOrder = order++,
                        CreatedBy = userEmail,
                        UpdatedBy = userEmail
                    });
                }
                continue;
            }

            var answers = (state.Answers ?? []).OrderBy(a => a.SortOrder).ToList();
            if (answers.Count == 0 && (task.Kind == CensusTaskKind.LongText || task.Key is "service" or "service-line"))
            {
                var text = task.Key == "service" ? response.ServiceDescription
                    : task.Key == "service-line" ? response.ServiceLineDescription
                    : null;
                if (!string.IsNullOrWhiteSpace(text))
                    answers.Add(new ServiceCensusAnswer { Title = task.Heading, Narrative = text });
            }

            var i = 0;
            foreach (var answer in answers)
            {
                _db.CensusEntries.Add(new CensusEntry
                {
                    ProductId = productId,
                    DomainKey = task.RegisterDomainKey,
                    Title = answer.Title,
                    Narrative = answer.Narrative,
                    LookupCode = answer.LookupCode,
                    SecondaryLookupCode = answer.SecondaryLookupCode,
                    ExternalUrl = answer.ExternalUrl,
                    SortOrder = i++,
                    CreatedBy = userEmail,
                    UpdatedBy = userEmail
                });
            }
        }

        foreach (var group in CensusDefinition.Groups())
        {
            var key = "census-" + group.Key;
            var declaration = await _db.CensusSectionDeclarations
                .FirstOrDefaultAsync(d => d.ProductId == productId && d.SectionKey == key, ct);
            if (declaration == null)
            {
                declaration = new CensusSectionDeclaration { ProductId = productId, SectionKey = key };
                _db.CensusSectionDeclarations.Add(declaration);
            }
            declaration.StatusCode = "COMPLETE";
            declaration.UpdatedAt = DateTime.UtcNow;
            declaration.UpdatedBy = userEmail;
        }

        await ApplyLookupsToProductAsync(product, response, ct);
    }

    private static string? Trim(string? value, int max)
    {
        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return null;
        return text.Length <= max ? text : text[..max];
    }

    private static string? FirstNonBlank(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
                return value.Trim();
        }
        return null;
    }

    private static List<string> PostedChoiceValues(ServiceCensusSaveRequest request)
    {
        var values = request.ChoiceValues
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .ToList();
        if (values.Count == 0 && !string.IsNullOrWhiteSpace(request.ChoiceValue))
            values.Add(request.ChoiceValue.Trim());
        return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool HasLookupSelection(CensusTask task, ServiceCensusSaveRequest request) =>
        PostedLookupIds(task, request).Count > 0;

    private static List<string> PostedLookupIds(CensusTask task, ServiceCensusSaveRequest request)
    {
        if (task.LookupControl == CensusLookupControl.Checkbox)
            return request.LookupIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        return string.IsNullOrWhiteSpace(request.LookupId) ? [] : [request.LookupId.Trim()];
    }

    private async Task PopulateLookupAsync(
        ServiceCensusQuestionViewModel vm, CensusTask task, CMDBProduct product, ServiceCensusTaskState? state, CancellationToken ct)
    {
        if (task.ChoiceOptions.Count > 0)
            vm.LookupOptions = task.ChoiceOptions.ToList();
        else
            vm.LookupOptions = await LoadLookupOptionsAsync(task.LookupSource, product, ct);
        var fromAnswers = (state?.Answers ?? [])
            .Select(a => a.LookupCode)
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Select(code => code!)
            .ToList();
        List<string> selected;
        if (fromAnswers.Count > 0)
            selected = fromAnswers;
        else if (task.ChoiceOptions.Count > 0)
            selected = [];
        else if (task.LookupSource == CensusLookupSource.BusinessArea)
        {
            var lookups = await FipsBusinessAreaLookupUiHelper.LoadBusinessAreaLookupOptionsForEditAsync(_db, product, ct);
            selected = FipsBusinessAreaLookupUiHelper.GetSelectedBusinessAreaLookupIds(product, lookups)
                .Select(id => id.ToString()).ToList();
        }
        else if (task.LookupSource == CensusLookupSource.Directorate)
        {
            var lookups = await FipsDirectorateLookupUiHelper.LoadDirectorateLookupOptionsForEditAsync(_db, product, ct);
            selected = FipsDirectorateLookupUiHelper.GetSelectedDirectorateLookupIds(product, lookups)
                .Select(id => id.ToString()).ToList();
        }
        else
            selected = CurrentProductLookupIds(task.LookupSource, product).Select(id => id.ToString()).ToList();

        if (task.LookupSource == CensusLookupSource.UserGroup)
            selected = await MapToLevel1UserGroupIdsAsync(selected, ct);

        vm.SelectedLookupIds = selected;
        vm.LookupId = selected.FirstOrDefault();
        vm.LongText = (state?.Answers ?? [])
            .FirstOrDefault(a => string.IsNullOrWhiteSpace(a.LookupCode) && !string.IsNullOrWhiteSpace(a.Narrative))
            ?.Narrative;
        foreach (var answer in state?.Answers ?? [])
        {
            if (string.IsNullOrWhiteSpace(answer.LookupCode)) continue;
            var option = vm.LookupOptions.FirstOrDefault(o =>
                string.Equals(o.Value, answer.LookupCode, StringComparison.OrdinalIgnoreCase));
            if (option != null && string.IsNullOrWhiteSpace(answer.Title))
                answer.Title = option.Label;
        }
    }

    private static bool IsDiscoveryPhase(string? name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Contains("discovery", StringComparison.OrdinalIgnoreCase);

    private async Task<List<CensusChoice>> LoadLookupOptionsAsync(
        CensusLookupSource source, CMDBProduct product, CancellationToken ct)
    {
        switch (source)
        {
            case CensusLookupSource.Phase:
            {
                var phases = await _db.PhaseLookups.AsNoTracking()
                    .Where(x => x.IsActive || x.Id == product.PhaseId)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name, x.Description })
                    .ToListAsync(ct);
                var firstIsDiscovery = phases.Count > 0 && IsDiscoveryPhase(phases[0].Name);
                var splitAt = phases.FindIndex(x => IsDiscoveryPhase(x.Name) != firstIsDiscovery);
                return phases.Select((x, i) => new CensusChoice
                {
                    Value = x.Id.ToString(),
                    Label = x.Name,
                    Hint = x.Description,
                    DividerBefore = splitAt >= 0 && i == splitAt
                }).ToList();
            }
            case CensusLookupSource.BusinessArea:
            {
                var rows = await FipsBusinessAreaLookupUiHelper.LoadBusinessAreaLookupOptionsForEditAsync(_db, product, ct);
                return rows.Select(x => new CensusChoice { Value = x.Id.ToString(), Label = x.Name }).ToList();
            }
            case CensusLookupSource.Directorate:
            {
                var rows = await FipsDirectorateLookupUiHelper.LoadDirectorateLookupOptionsForEditAsync(_db, product, ct);
                return rows.Select(x => new CensusChoice { Value = x.Id.ToString(), Label = x.Name }).ToList();
            }
            case CensusLookupSource.Channel:
                return (await _db.FipsChannels.AsNoTracking()
                    .Where(x => x.Active)
                    .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name, x.Description })
                    .ToListAsync(ct))
                    .Select(x => new CensusChoice { Value = x.Id.ToString(), Label = x.Name, Hint = x.Description })
                    .ToList();
            case CensusLookupSource.Type:
                return (await _db.FipsTypes.AsNoTracking()
                    .Where(x => x.Active)
                    .OrderBy(x => x.DisplayOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name, x.Description })
                    .ToListAsync(ct))
                    .Select(x => new CensusChoice { Value = x.Id.ToString(), Label = x.Name, Hint = x.Description })
                    .ToList();
            case CensusLookupSource.UserGroup:
                return (await FipsUserGroupUiHelper.LoadActiveTreeAsync(_db, ct))
                    .Where(x => x.Depth == 0)
                    .Select(x => new CensusChoice
                    {
                        Value = x.Id.ToString(),
                        Label = x.Name,
                        Hint = x.Description
                    })
                    .ToList();
            case CensusLookupSource.MissionPillar:
                return (await _db.Missions.AsNoTracking()
                    .Where(m => !m.IsDeleted)
                    .OrderBy(m => m.Title)
                    .Select(m => new { m.Id, m.Title, m.Description })
                    .ToListAsync(ct))
                    .Select(m => new CensusChoice { Value = m.Id.ToString(), Label = m.Title, Hint = m.Description })
                    .ToList();
            case CensusLookupSource.StrategicObjective:
                return (await _db.Objectives.AsNoTracking()
                    .Where(o => !o.IsDeleted && o.Status == "active")
                    .OrderBy(o => o.Title)
                    .Select(o => new { o.Id, o.Title, o.Description })
                    .ToListAsync(ct))
                    .Select(o => new CensusChoice { Value = o.Id.ToString(), Label = o.Title, Hint = o.Description })
                    .ToList();
            case CensusLookupSource.Technology:
                return (await _db.CensusTechnologyLookups.AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name, x.Description })
                    .ToListAsync(ct))
                    .Select(x => new CensusChoice { Value = x.Id.ToString(), Label = x.Name, Hint = x.Description })
                    .ToList();
            case CensusLookupSource.Integration:
                return (await _db.CensusIntegrationLookups.AsNoTracking()
                    .Where(x => x.IsActive)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name, x.Description })
                    .ToListAsync(ct))
                    .Select(x => new CensusChoice { Value = x.Id.ToString(), Label = x.Name, Hint = x.Description })
                    .ToList();
            case CensusLookupSource.RiskAppetite:
                return (await _db.RiskAppetiteLookups.AsNoTracking()
                    .Where(x => x.IsActive || x.Id == product.RiskAppetiteLookupId)
                    .OrderBy(x => x.SortOrder).ThenBy(x => x.Name)
                    .Select(x => new { x.Id, x.Name, x.Description })
                    .ToListAsync(ct))
                    .Select(x => new CensusChoice { Value = x.Id.ToString(), Label = x.Name, Hint = x.Description })
                    .ToList();
            default:
                return [];
        }
    }

    private static List<int> CurrentProductLookupIds(CensusLookupSource source, CMDBProduct product) =>
        source switch
        {
            CensusLookupSource.Phase when product.PhaseId is int phaseId => [phaseId],
            CensusLookupSource.BusinessArea => product.BusinessAreas
                .Select(b => b.FipsBusinessArea?.BusinessAreaLookupId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList(),
            CensusLookupSource.Directorate => product.Directorates
                .Select(d => d.FipsDirectorate?.DirectorateLookupId)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList(),
            CensusLookupSource.Channel => product.Channels.Select(c => c.FipsChannelId).ToList(),
            CensusLookupSource.Type => product.Types.Select(t => t.FipsTypeId).ToList(),
            CensusLookupSource.UserGroup => product.UserGroups.Select(u => u.FipsUserGroupId).ToList(),
            CensusLookupSource.MissionPillar => product.Missions.Select(m => m.MissionId).ToList(),
            CensusLookupSource.StrategicObjective => product.Objectives.Select(o => o.ObjectiveId).ToList(),
            CensusLookupSource.RiskAppetite when product.RiskAppetiteLookupId is int riskId => [riskId],
            CensusLookupSource.Technology => [],
            CensusLookupSource.Integration => [],
            _ => []
        };

    private async Task FillLookupTitlesAsync(
        CensusTask task, CMDBProduct product, ServiceCensusTaskState state, CancellationToken ct)
    {
        var options = await LoadLookupOptionsAsync(task.LookupSource, product, ct);
        foreach (var answer in state.Answers)
        {
            var option = options.FirstOrDefault(o =>
                string.Equals(o.Value, answer.LookupCode, StringComparison.OrdinalIgnoreCase));
            if (option != null)
                answer.Title = option.Label;
        }
    }

    private async Task ApplyLookupsToProductAsync(CMDBProduct product, ServiceCensusResponse response, CancellationToken ct)
    {
        foreach (var task in CensusDefinition.Tasks.Where(t => t.Kind == CensusTaskKind.Lookup))
        {
            var state = StateOf(response, task.Key);
            if (state == null || state.CannotAnswer) continue;
            if (task.LookupSource == CensusLookupSource.Technology
                || task.LookupSource == CensusLookupSource.Integration) continue;

            var ids = (state.Answers ?? [])
                .Select(a => int.TryParse(a.LookupCode, out var id) ? id : 0)
                .Where(id => id > 0)
                .Distinct()
                .ToArray();

            switch (task.LookupSource)
            {
                case CensusLookupSource.Phase:
                    product.PhaseId = ids.FirstOrDefault() is int phase && phase > 0 ? phase : null;
                    break;
                case CensusLookupSource.BusinessArea:
                    var baIds = await _businessAreaSync.ResolveToFipsBusinessAreaIdsAsync(ids, ct);
                    _db.CMDBProductBusinessAreas.RemoveRange(product.BusinessAreas);
                    foreach (var id in baIds.Distinct())
                        product.BusinessAreas.Add(new CMDBProductBusinessArea { CMDBProductId = product.Id, FipsBusinessAreaId = id });
                    break;
                case CensusLookupSource.Directorate:
                    var dirIds = await _directorateSync.ResolveToFipsDirectorateIdsAsync(ids, ct);
                    _db.CMDBProductDirectorates.RemoveRange(product.Directorates);
                    foreach (var id in dirIds.Distinct())
                        product.Directorates.Add(new CMDBProductDirectorate { CMDBProductId = product.Id, FipsDirectorateId = id });
                    break;
                case CensusLookupSource.Channel:
                    _db.CMDBProductChannels.RemoveRange(product.Channels);
                    foreach (var id in ids)
                        product.Channels.Add(new CMDBProductChannel { CMDBProductId = product.Id, FipsChannelId = id });
                    break;
                case CensusLookupSource.Type:
                    _db.CMDBProductTypes.RemoveRange(product.Types);
                    foreach (var id in ids)
                        product.Types.Add(new CMDBProductType { CMDBProductId = product.Id, FipsTypeId = id });
                    break;
                case CensusLookupSource.UserGroup:
                    var level1Ids = (await MapToLevel1UserGroupIdsAsync(ids.Select(id => id.ToString()), ct))
                        .Select(v => int.Parse(v))
                        .ToArray();
                    _db.CMDBProductUserGroups.RemoveRange(product.UserGroups);
                    foreach (var id in level1Ids)
                        product.UserGroups.Add(new CMDBProductUserGroup { CMDBProductId = product.Id, FipsUserGroupId = id });
                    break;
                case CensusLookupSource.MissionPillar:
                    _db.CMDBProductMissions.RemoveRange(product.Missions);
                    foreach (var id in ids)
                        product.Missions.Add(new CMDBProductMission { CMDBProductId = product.Id, MissionId = id, CreatedAt = DateTime.UtcNow });
                    break;
                case CensusLookupSource.StrategicObjective:
                    _db.CMDBProductObjectives.RemoveRange(product.Objectives);
                    foreach (var id in ids)
                        product.Objectives.Add(new CMDBProductObjective { CMDBProductId = product.Id, ObjectiveId = id, CreatedAt = DateTime.UtcNow });
                    break;
                case CensusLookupSource.RiskAppetite:
                    product.RiskAppetiteLookupId = ids.FirstOrDefault() is int risk && risk > 0 ? risk : null;
                    break;
            }
        }

        ApplyProductUrlFromCensus(product, response);
        product.UpdatedAt = DateTime.UtcNow;
    }

    private static void ApplyProductUrlFromCensus(CMDBProduct product, ServiceCensusResponse response)
    {
        var task = CensusDefinition.Find("product-url");
        if (task == null) return;
        var state = StateOf(response, task.Key);
        if (state == null || state.CannotAnswer) return;

        var access = (state.Answers ?? []).FirstOrDefault(a =>
            string.Equals(a.Title, "access", StringComparison.OrdinalIgnoreCase));
        if (access == null) return;

        var hasUrl = string.Equals(access.LookupCode, "has-url", StringComparison.OrdinalIgnoreCase);
        product.ProductURL = hasUrl ? Trim(access.Narrative, 2000) : null;
    }

    private static void HydrateNamedFormFields(
        ServiceCensusQuestionViewModel vm,
        CensusTask task,
        ServiceCensusResponse response,
        ServiceCensusTaskState? state,
        CMDBProduct product)
    {
        if (task.Key == "product-url")
        {
            vm.RegisterProductUrl = string.IsNullOrWhiteSpace(product.ProductURL) ? null : product.ProductURL.Trim();
            if (!HasFormValue(vm, "access") && !string.IsNullOrWhiteSpace(vm.RegisterProductUrl))
            {
                vm.FormValues["access"] = "has-url";
                if (!vm.FormNarratives.TryGetValue("access", out var url) || string.IsNullOrWhiteSpace(url))
                    vm.FormNarratives["access"] = vm.RegisterProductUrl;
            }
        }

        if (task.Key is "service" or "service-line")
        {
            var stored = task.Key == "service" ? response.ServiceDescription : response.ServiceLineDescription;
            SetFormValueIfMissing(vm, "name", stored);
            if (!HasFormValue(vm, "name"))
            {
                var leftover = (state?.Answers ?? []).FirstOrDefault(a =>
                    !string.Equals(a.Title, "name", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(a.Title, "description", StringComparison.OrdinalIgnoreCase)
                    && !string.IsNullOrWhiteSpace(a.Narrative));
                if (leftover != null)
                    vm.FormValues["name"] = leftover.Narrative;
            }
            vm.Items.Clear();
        }

        if (task.Key == "group"
            && string.Equals(vm.FormValues.GetValueOrDefault("group"), CensusDefinition.GroupFunding, StringComparison.OrdinalIgnoreCase))
        {
            vm.FormValues["group"] = CensusDefinition.GroupCrossCutting;
            if (!vm.ChoiceValues.Any(v => string.Equals(v, CensusDefinition.GroupFunding, StringComparison.OrdinalIgnoreCase)))
                vm.ChoiceValues.Add(CensusDefinition.GroupFunding);
        }

        if (task.Key == "group"
            && string.Equals(vm.FormValues.GetValueOrDefault("group"), CensusDefinition.GroupEnabling, StringComparison.OrdinalIgnoreCase))
        {
            var enabling = StateOf(response, CensusDefinition.EnablingProductTaskKey);
            foreach (var kind in (enabling?.Answers ?? [])
                .Select(a => a.LookupCode)
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .SelectMany(code => code!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
            {
                if (!vm.ChoiceValues.Any(v => string.Equals(v, kind, StringComparison.OrdinalIgnoreCase)))
                    vm.ChoiceValues.Add(kind);
            }
        }
    }

    private static bool IsConditionalChoiceAnswer(CensusTask task, string title) =>
        task.FormFields.Any(field =>
            field.ConditionalChoices.Any(choice =>
                string.Equals(choice.AnswerTitle, title, StringComparison.OrdinalIgnoreCase)));

    private static bool HasFormValue(ServiceCensusQuestionViewModel vm, string key) =>
        vm.FormValues.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value);

    private static void SetFormValueIfMissing(ServiceCensusQuestionViewModel vm, string key, string? value)
    {
        if (HasFormValue(vm, key) || string.IsNullOrWhiteSpace(value))
            return;
        vm.FormValues[key] = value;
    }

    private async Task<List<string>> MapToLevel1UserGroupIdsAsync(IEnumerable<string> values, CancellationToken ct)
    {
        var requested = values
            .Select(v => int.TryParse(v, out var id) ? id : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (requested.Count == 0)
            return [];

        var groups = await _db.FipsUserGroups.AsNoTracking()
            .Select(g => new { g.Id, g.ParentId })
            .ToListAsync(ct);
        var parentById = groups.ToDictionary(g => g.Id, g => g.ParentId);
        var roots = new HashSet<int>();
        foreach (var id in requested)
        {
            var current = id;
            var seen = new HashSet<int>();
            while (parentById.TryGetValue(current, out var parent) && parent.HasValue && seen.Add(current))
                current = parent.Value;
            if (parentById.ContainsKey(current))
                roots.Add(current);
        }

        return roots.Select(id => id.ToString()).ToList();
    }
}
