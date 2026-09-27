using System.Text.Json;
using System.Text.RegularExpressions;
using Compass.Data;
using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;
using Compass.Services;
using Compass.Services.Fips;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceDataModels;

public sealed class ServiceDataModelCensusService : IServiceDataModelCensusService
{
    private readonly CompassDbContext _db;
    private readonly IServiceDataModelAccessService _access;
    private readonly ICoreCensusThemeService _coreThemes;
    private readonly IFipsProductWriteService _fipsProductWrite;
    private readonly IAuditLogger _auditLogger;

    public ServiceDataModelCensusService(
        CompassDbContext db,
        IServiceDataModelAccessService access,
        ICoreCensusThemeService coreThemes,
        IPermissionService permissions,
        IFipsProductWriteService fipsProductWrite,
        IAuditLogger auditLogger)
    {
        _db = db;
        _access = access;
        _coreThemes = coreThemes;
        _ = permissions; // retained for DI compatibility; work list scopes via contacts + access service
        _fipsProductWrite = fipsProductWrite;
        _auditLogger = auditLogger;
    }

    public async Task<CensusProductAssignmentsViewModel?> GetAssignmentsForProductAsync(
        Guid productId,
        string email)
    {
        // Listing is available to authenticated users who can open the Service Register
        // product page. Save/submit/review remain gated by CanEdit/CanReview.
        if (string.IsNullOrWhiteSpace(email) || productId == Guid.Empty)
            return null;

        var product = await _db.CMDBProducts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
            return null;

        // Lazy-create standing Service Census when the user can access this product.
        if (await _access.CanAccessProductAsync(_db, email, productId))
            await EnsureStandingServiceCensusAsync(productId, email);

        var assignments = await _db.ServiceDataModelAssignments
            .AsNoTracking()
            .Include(a => a.Version)
            .ThenInclude(v => v.Model)
            .Where(a => a.CMDBProductId == productId)
            .OrderByDescending(a => a.UpdatedUtc)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var items = assignments.Select(a => MapListItem(a, product.Title, now)).ToList();

        return new CensusProductAssignmentsViewModel
        {
            ProductId = productId,
            ProductTitle = product.Title,
            Assignments = items,
            CanEdit = await _access.CanEditCensusAsync(_db, email, productId),
            CanReview = await _access.CanReviewCensusAsync(_db, email, productId)
        };
    }

    public async Task<CensusWorkListViewModel> GetWorkListAsync(
        string email,
        string tab,
        string? statusFilter,
        string? search,
        int? businessAreaId = null,
        int? phaseId = null,
        int? typeId = null,
        int? channelId = null,
        int? userGroupId = null)
    {
        var normalizedTab = CensusWorkListBuilder.NormalizeTab(tab);
        var listingTab = CensusWorkListBuilder.ToListingHelperTab(normalizedTab);

        // Same populations / auth as Service Register: "your" → my contacts; "all" / "business-area" → active catalogue.
        var productsVm = await FipsProductListingHelper.BuildProductsViewModelAsync(
            _db,
            listingTab,
            email ?? string.Empty,
            search,
            businessAreaId,
            channelId,
            userGroupId,
            typeId,
            phaseId);

        var productRows = productsVm.Products.ToList();
        await EnrichServiceOwnersAsync(productRows);

        var publishedBaseline = await LoadPublishedServiceCensusBaselineAsync();
        var progressByProduct = await LoadPrimaryProgressSummariesAsync(
            productRows.Select(p => p.Id).ToList());

        var workRows = CensusWorkListBuilder.BuildRows(
            productRows,
            progressByProduct,
            publishedBaseline,
            statusFilter);

        productsVm.Products = workRows;
        productsVm.ActiveTab = listingTab;
        productsVm.Search = search;
        productsVm.BusinessAreaId = businessAreaId;
        productsVm.ChannelId = channelId;
        productsVm.UserGroupId = userGroupId;
        productsVm.TypeId = typeId;
        productsVm.PhaseId = phaseId;

        IReadOnlyList<CensusWorkListBusinessAreaGroupViewModel> baGroups =
            Array.Empty<CensusWorkListBusinessAreaGroupViewModel>();
        if (normalizedTab == CensusWorkListBuilder.TabBusinessArea)
        {
            var areasByProduct = await LoadBusinessAreaNamesByProductAsync(workRows.Select(p => p.Id).ToList());
            baGroups = CensusWorkListBuilder.GroupByBusinessArea(workRows, areasByProduct)
                .Select(g => new CensusWorkListBusinessAreaGroupViewModel
                {
                    Name = g.Name,
                    Products = g.Products
                })
                .ToList();
        }

        return new CensusWorkListViewModel
        {
            Tab = normalizedTab,
            StatusFilter = statusFilter,
            Search = search,
            BusinessAreaId = businessAreaId,
            ChannelId = channelId,
            UserGroupId = userGroupId,
            PhaseId = phaseId,
            TypeId = typeId,
            YourProductsCount = productsVm.MyProductsCount,
            AllProductsCount = productsVm.AllProductsCount,
            CensusPublishedAvailable = publishedBaseline != null,
            Products = productsVm,
            BusinessAreaGroups = baGroups,
            BusinessAreaOptions = productsVm.BusinessAreaOptions
                .Select(b => new CensusWorkListFilterOption { Value = b.Id.ToString(), Text = b.Name })
                .ToList(),
            ChannelOptions = productsVm.ChannelOptions
                .Select(c => new CensusWorkListFilterOption { Value = c.Id.ToString(), Text = c.Name })
                .ToList(),
            UserGroupOptions = productsVm.UserGroupOptions
                .Select(u => new CensusWorkListFilterOption { Value = u.Id.ToString(), Text = u.Name })
                .ToList(),
            PhaseOptions = productsVm.PhaseOptions
                .Select(p => new CensusWorkListFilterOption { Value = p.Id.ToString(), Text = p.Name })
                .ToList(),
            TypeOptions = productsVm.TypeOptions
                .Select(t => new CensusWorkListFilterOption { Value = t.Id.ToString(), Text = t.Name })
                .ToList()
        };
    }

    public async Task<Guid?> EnsureStandingServiceCensusAsync(Guid productId, string email)
    {
        if (string.IsNullOrWhiteSpace(email) || productId == Guid.Empty)
            return null;

        if (!await _access.CanAccessProductAsync(_db, email, productId))
            return null;

        var productExists = await _db.CMDBProducts.AsNoTracking()
            .AnyAsync(p => p.Id == productId);
        if (!productExists)
            return null;

        var published = await GetPublishedServiceCensusVersionAsync();
        if (published == null)
            return null;

        var existing = await _db.ServiceDataModelAssignments
            .Where(a =>
                a.CMDBProductId == productId &&
                a.Version.Model.StableKey == ServiceCensusDefaults.StableKey)
            .OrderByDescending(a => a.UpdatedUtc)
            .FirstOrDefaultAsync();

        if (existing != null)
            return existing.Id;

        var now = DateTime.UtcNow;
        var standing = new ServiceDataModelAssignment
        {
            ServiceDataModelVersionId = published.Id,
            CMDBProductId = productId,
            Status = ServiceDataModelAssignmentStatus.NotStarted,
            CreatedUtc = now,
            UpdatedUtc = now
        };
        _db.ServiceDataModelAssignments.Add(standing);
        WriteSdmAudit(
            "ServiceDataModelAssignment",
            standing.Id.ToString(),
            "EnsureStandingServiceCensus",
            email,
            JsonSerializer.Serialize(new { productId, versionId = published.Id }));
        await _db.SaveChangesAsync();
        return standing.Id;
    }

    private async Task EnrichServiceOwnersAsync(List<FipsProductRow> rows)
    {
        if (rows.Count == 0)
            return;

        var ids = rows.Select(r => r.Id).Distinct().ToList();
        var ownerRows = await _db.CMDBProductContacts.AsNoTracking()
            .Where(c =>
                ids.Contains(c.CMDBProductId) &&
                c.FipsContactRole != null &&
                c.FipsContactRole.Name == "Service Owner")
            .Select(c => new
            {
                c.CMDBProductId,
                Name = c.UserName ?? c.UserEmail ?? ""
            })
            .ToListAsync();

        var ownersByProduct = ownerRows
            .Where(o => !string.IsNullOrWhiteSpace(o.Name))
            .GroupBy(o => o.CMDBProductId)
            .ToDictionary(
                g => g.Key,
                g => string.Join(
                    ", ",
                    g.Select(x => x.Name.Trim())
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)));

        foreach (var row in rows)
        {
            if (ownersByProduct.TryGetValue(row.Id, out var joined) && !string.IsNullOrWhiteSpace(joined))
                row.ServiceOwner = joined;
        }
    }

    private async Task<Dictionary<Guid, IReadOnlyList<string>>> LoadBusinessAreaNamesByProductAsync(
        IReadOnlyList<Guid> productIds)
    {
        var result = new Dictionary<Guid, IReadOnlyList<string>>();
        if (productIds.Count == 0)
            return result;

        var rows = await _db.CMDBProductBusinessAreas.AsNoTracking()
            .Where(b => productIds.Contains(b.CMDBProductId))
            .Select(b => new { b.CMDBProductId, Name = b.FipsBusinessArea.Name })
            .ToListAsync();

        foreach (var group in rows.GroupBy(r => r.CMDBProductId))
        {
            result[group.Key] = group
                .Select(x => x.Name)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return result;
    }

    private async Task<ServiceDataModelVersion?> GetPublishedServiceCensusVersionAsync()
    {
        return await _db.ServiceDataModelVersions
            .Include(v => v.Model)
            .Where(v =>
                v.Status == ServiceDataModelLifecycleStatus.Published &&
                v.Model.StableKey == ServiceCensusDefaults.StableKey)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();
    }

    private async Task<CensusWorkListBuilder.PublishedCensusBaseline?> LoadPublishedServiceCensusBaselineAsync()
    {
        var published = await GetPublishedServiceCensusVersionAsync();
        if (published == null)
            return null;

        var themes = await _coreThemes.GetSharedStructureAsync();
        var fields = SharedCensusAnswerKeys.FlattenActiveFields(themes);
        var inputs = fields.Select(f => new ServiceDataModelFieldAnswerInput(
            f.Id,
            f.StableKey,
            f.FieldType,
            f.IsMandatory,
            f.CountsTowardsCompletion,
            f.VisibilityRuleJson,
            ValueJson: null,
            IsValid: true,
            IsDisabled: false)).ToList();

        var completion = ServiceDataModelCompletionCalculator.Calculate(inputs);
        return new CensusWorkListBuilder.PublishedCensusBaseline(
            published.Id,
            completion.ApplicableCountingFields);
    }

    private async Task<Dictionary<Guid, CensusWorkListBuilder.CensusProgressSummary>> LoadPrimaryProgressSummariesAsync(
        IReadOnlyList<Guid> productIds)
    {
        if (productIds.Count == 0)
            return new Dictionary<Guid, CensusWorkListBuilder.CensusProgressSummary>();

        var assignmentMeta = await _db.ServiceDataModelAssignments.AsNoTracking()
            .Where(a => productIds.Contains(a.CMDBProductId))
            .Select(a => new
            {
                a.Id,
                a.CMDBProductId,
                a.Status,
                a.FieldCompletionPercent,
                a.UpdatedUtc,
                ModelStableKey = a.Version.Model.StableKey
            })
            .ToListAsync();

        var primaryByProduct = new Dictionary<Guid, (Guid Id, ServiceDataModelAssignmentStatus Status, decimal? Pct, string? Key, DateTime Updated)>();
        foreach (var group in assignmentMeta.GroupBy(r => r.CMDBProductId))
        {
            var primary = CensusWorkListBuilder.SelectPrimaryProgress(
                group.Select(r => new CensusWorkListBuilder.CensusProgressSummary(
                    r.Id,
                    r.Status,
                    r.FieldCompletionPercent,
                    AnsweredCountingFields: 0,
                    ApplicableCountingFields: 0,
                    r.ModelStableKey,
                    r.UpdatedUtc)));
            if (primary != null)
                primaryByProduct[group.Key] = (primary.StandingRecordId, primary.Status, primary.FieldCompletionPercent, primary.ModelStableKey, primary.UpdatedUtc);
        }

        if (primaryByProduct.Count == 0)
            return new Dictionary<Guid, CensusWorkListBuilder.CensusProgressSummary>();

        var standingIds = primaryByProduct.Values.Select(v => v.Id).ToList();
        var graphs = await _db.ServiceDataModelAssignments.AsNoTracking()
            .Include(a => a.Product)
            .Include(a => a.Version)
            .ThenInclude(v => v.Model)
            .Include(a => a.Submissions)
            .ThenInclude(s => s.Answers)
            .ThenInclude(ans => ans.Field)
            .Where(a => standingIds.Contains(a.Id))
            .ToListAsync();

        var sharedThemes = await _coreThemes.GetSharedStructureAsync();
        var result = new Dictionary<Guid, CensusWorkListBuilder.CensusProgressSummary>();
        foreach (var assignment in graphs)
        {
            if (!primaryByProduct.TryGetValue(assignment.CMDBProductId, out var meta))
                continue;

            var submission = assignment.Submissions.FirstOrDefault(s => s.IsCurrent);
            var fields = SharedCensusAnswerKeys.FlattenActiveFields(sharedThemes)
                .Select(SharedCensusAnswerKeys.AsTransientModelField)
                .ToList();
            var inputs = BuildAnswerInputs(fields, submission, assignment.Product, out _);
            var completion = ServiceDataModelCompletionCalculator.Calculate(inputs);

            result[assignment.CMDBProductId] = new CensusWorkListBuilder.CensusProgressSummary(
                assignment.Id,
                meta.Status,
                completion.FieldCompletionPercent ?? (completion.ApplicableCountingFields > 0 ? 0m : null),
                completion.AnsweredCountingFields,
                completion.ApplicableCountingFields,
                meta.Key,
                meta.Updated);
        }

        return result;
    }

    public async Task<CensusAssignmentFormViewModel?> GetAssignmentFormAsync(Guid assignmentId, string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return null;

        // Track so live completion (after published structure adds) can be persisted on read.
        var assignment = await LoadAssignmentGraphAsync(assignmentId, tracking: true);
        if (assignment == null)
            return null;

        // Authenticated users may view; CanEdit/CanReview on the form control mutations.
        var form = await BuildFormAsync(assignment, email);
        if (assignment.FieldCompletionPercent != form.FieldCompletionPercent ||
            assignment.MandatoryCompletionPercent != form.MandatoryCompletionPercent)
        {
            assignment.FieldCompletionPercent = form.FieldCompletionPercent;
            assignment.MandatoryCompletionPercent = form.MandatoryCompletionPercent;
            await _db.SaveChangesAsync();
        }

        return form;
    }

    public async Task<CensusSaveAnswersResult> SaveAnswersAsync(
        Guid assignmentId,
        string email,
        IReadOnlyDictionary<Guid, string?> answers,
        DateTime? expectedUpdatedUtc = null,
        Guid? markThemeCompleteGroupId = null)
    {
        var assignment = await LoadAssignmentGraphAsync(assignmentId, tracking: true);
        if (assignment == null)
            return new CensusSaveAnswersResult { NotFound = true };

        if (!await _access.CanEditCensusAsync(_db, email, assignment.CMDBProductId))
            return new CensusSaveAnswersResult { Forbidden = true };

        var isStanding = !assignment.Version.Model.IsRepeatable;
        if (assignment.Status is ServiceDataModelAssignmentStatus.Withdrawn
            or ServiceDataModelAssignmentStatus.NotApplicable)
        {
            return new CensusSaveAnswersResult
            {
                Forbidden = true,
                ErrorMessage = "This assignment cannot be edited in its current status."
            };
        }

        // Standing models keep living answers: submitted/reviewed remain editable.
        // Period-based models stay locked after submit/review until changes are requested.
        if (!isStanding &&
            assignment.Status is ServiceDataModelAssignmentStatus.Submitted
                or ServiceDataModelAssignmentStatus.Reviewed)
        {
            return new CensusSaveAnswersResult
            {
                Forbidden = true,
                ErrorMessage = "This assignment cannot be edited in its current status."
            };
        }

        if (expectedUpdatedUtc.HasValue &&
            TruncateToSeconds(assignment.UpdatedUtc) != TruncateToSeconds(expectedUpdatedUtc.Value))
        {
            return new CensusSaveAnswersResult
            {
                Conflict = true,
                ErrorMessage = "This assignment was updated by someone else. Refresh and try again.",
                Form = await BuildFormAsync(assignment, email)
            };
        }

        var now = DateTime.UtcNow;

        // Preserve submitted/reviewed snapshots on standing models by opening a new editable revision.
        if (isStanding &&
            assignment.Status is ServiceDataModelAssignmentStatus.Submitted
                or ServiceDataModelAssignmentStatus.Reviewed
                or ServiceDataModelAssignmentStatus.ReviewDue)
        {
            OpenEditableRevision(assignment, email, now);
            assignment.Status = ServiceDataModelAssignmentStatus.InProgress;
        }

        var submission = EnsureCurrentSubmission(assignment, email, now);
        var sharedThemes = await _coreThemes.GetSharedStructureAsync();
        var coreFields = SharedCensusAnswerKeys.FlattenActiveFields(sharedThemes);
        var fieldsById = coreFields.ToDictionary(f => f.Id);
        var themeKeyByFieldId = sharedThemes
            .SelectMany(t => t.Fields.Select(f => (ThemeKey: t.StableKey, FieldId: f.Id)))
            .ToDictionary(x => x.FieldId, x => x.ThemeKey);
        var themesWithAnswerChanges = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        string? markCompleteStableKey = null;
        if (markThemeCompleteGroupId is Guid completeGroupId)
        {
            var themeToComplete = sharedThemes.FirstOrDefault(t => t.Id == completeGroupId);
            if (themeToComplete == null || string.IsNullOrWhiteSpace(themeToComplete.StableKey))
            {
                return new CensusSaveAnswersResult
                {
                    Success = false,
                    ErrorMessage = "That theme could not be marked complete.",
                    Form = await BuildFormAsync(assignment, email)
                };
            }

            markCompleteStableKey = themeToComplete.StableKey;
        }

        var updateDescription = false;
        string? descriptionValue = null;
        var updateUrl = false;
        string? urlValue = null;

        foreach (var (fieldId, valueJson) in answers)
        {
            if (!fieldsById.TryGetValue(fieldId, out var coreField))
                continue;

            var field = SharedCensusAnswerKeys.AsTransientModelField(coreField);
            NormalizeLegacyCanonicalKey(field);

            // Title is register-locked: ignore posted values and never propose a rename.
            var effectiveValueJson = CensusAnswerValueNormalizer.Normalize(coreField, valueJson);
            if (ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey(field.CanonicalAttributeKey))
            {
                var registerTitle = ResolveCanonicalDisplayValue(assignment.Product, field.CanonicalAttributeKey!);
                effectiveValueJson = string.IsNullOrWhiteSpace(registerTitle)
                    ? null
                    : ServiceDataModelCanonicalPrefill.ToValueJson(registerTitle, field.FieldType);
            }

            var (isValid, message) = ValidateAnswer(field, effectiveValueJson);
            var existing = SharedCensusAnswerKeys.FindByStableKey(submission.Answers, coreField.StableKey);
            var previousValueJson = existing?.ValueJson;
            if (existing == null)
            {
                existing = new ServiceDataModelAnswer
                {
                    ServiceDataModelSubmissionId = submission.Id,
                    ServiceDataModelFieldId = null,
                    FieldStableKey = coreField.StableKey
                };
                submission.Answers.Add(existing);
                _db.ServiceDataModelAnswers.Add(existing);
            }
            else
            {
                existing.FieldStableKey = coreField.StableKey;
                existing.ServiceDataModelFieldId = null;
            }

            existing.ValueJson = effectiveValueJson;
            existing.IsValid = isValid;
            existing.ValidationMessage = message;
            existing.UpdatedUtc = now;
            existing.UpdatedByEmail = email.Trim();

            if (!AnswerValuesEqual(previousValueJson, effectiveValueJson) &&
                themeKeyByFieldId.TryGetValue(fieldId, out var changedThemeKey))
            {
                themesWithAnswerChanges.Add(changedThemeKey);
            }

            if (string.IsNullOrWhiteSpace(field.CanonicalAttributeKey))
                continue;

            if (ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey(field.CanonicalAttributeKey))
            {
                await ClearPendingProposedRegisterChangeAsync(assignment.Id, coreField.StableKey);
                continue;
            }

            if (ServiceDataModelCanonicalPrefill.IsDescriptionCanonicalKey(field.CanonicalAttributeKey))
            {
                updateDescription = true;
                descriptionValue = ExtractDisplayValue(effectiveValueJson);
                await ClearPendingProposedRegisterChangeAsync(assignment.Id, coreField.StableKey);
                continue;
            }

            if (ServiceDataModelCanonicalPrefill.IsUrlCanonicalKey(field.CanonicalAttributeKey))
            {
                updateUrl = true;
                urlValue = ExtractDisplayValue(effectiveValueJson);
                await ClearPendingProposedRegisterChangeAsync(assignment.Id, coreField.StableKey);
                continue;
            }

            await UpsertProposedRegisterChangeAsync(assignment, field, coreField.StableKey, effectiveValueJson, email, now);
        }

        // Persist register-driven prefill for fields not posted (e.g. suppressed has-public-url)
        // so completion and conditional visibility stay consistent after save.
        await ApplyAndPersistCanonicalPrefillAsync(assignment, submission, sharedThemes, email, now);

        // Ensure title answers always match the register even when the field was not posted.
        await SyncReadOnlyTitleAnswersAsync(assignment, submission, sharedThemes, email, now);

        string? registerWarning = null;
        if (updateDescription || updateUrl)
        {
            var outcome = await _fipsProductWrite.TryUpdateDescriptionAndUrlAsync(
                assignment.CMDBProductId,
                email.Trim(),
                email.Trim(),
                updateDescription,
                descriptionValue,
                updateUrl,
                urlValue);

            if (outcome.Forbidden)
            {
                registerWarning =
                    "Your census answers were saved, but the Service Register could not be updated because you are not allowed to edit this product.";
            }
            else if (outcome.NotFound)
            {
                registerWarning =
                    "Your census answers were saved, but the Service Register product could not be found to update URL or description.";
            }
            else if (assignment.Product != null && outcome.Changes.Count > 0)
            {
                // Keep the in-memory product aligned for completion/prefill on the returned form.
                if (updateDescription)
                    assignment.Product.UserDescription = string.IsNullOrWhiteSpace(descriptionValue)
                        ? null
                        : descriptionValue.Trim();
                if (updateUrl)
                    assignment.Product.ProductURL = string.IsNullOrWhiteSpace(urlValue)
                        ? null
                        : urlValue.Trim();
            }
        }

        if (assignment.Status == ServiceDataModelAssignmentStatus.NotStarted)
            assignment.Status = ServiceDataModelAssignmentStatus.InProgress;

        assignment.LastAnsweredUtc = now;
        assignment.LastAnsweredByEmail = email.Trim();
        assignment.UpdatedUtc = now;
        submission.UpdatedUtc = now;
        submission.UpdatedByEmail = email.Trim();

        await RecalculateCompletionAsync(assignment);

        // Changing answers after Complete this section returns the theme to In progress,
        // unless this save also marks that same theme complete again.
        if (themesWithAnswerChanges.Count > 0)
        {
            var keysToClear = string.IsNullOrWhiteSpace(markCompleteStableKey)
                ? themesWithAnswerChanges
                : themesWithAnswerChanges
                    .Where(k => !string.Equals(k, markCompleteStableKey, StringComparison.OrdinalIgnoreCase))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
            await ClearThemeCompletionsAsync(assignment.Id, keysToClear);
        }

        if (!string.IsNullOrWhiteSpace(markCompleteStableKey))
            await UpsertThemeCompletionAsync(assignment.Id, markCompleteStableKey, email, now);

        WriteSdmAudit("ServiceDataModelAssignment", assignment.Id.ToString(), "SaveAnswers", email,
            JsonSerializer.Serialize(new
            {
                fieldCount = answers.Count,
                assignment.Status,
                updateDescription,
                updateUrl,
                clearedThemeCompletions = themesWithAnswerChanges.ToList(),
                markedCompleteThemeKey = markCompleteStableKey
            }));
        await _db.SaveChangesAsync();

        // Reload for form
        assignment = await LoadAssignmentGraphAsync(assignmentId, tracking: true);
        return new CensusSaveAnswersResult
        {
            Success = true,
            WarningMessage = registerWarning,
            Form = assignment == null ? null : await BuildFormAsync(assignment, email)
        };
    }

    public async Task<CensusActionResult> SubmitAsync(Guid assignmentId, string email)
    {
        var assignment = await LoadAssignmentGraphAsync(assignmentId, tracking: true);
        if (assignment == null)
            return new CensusActionResult { NotFound = true };

        if (!await _access.CanEditCensusAsync(_db, email, assignment.CMDBProductId))
            return new CensusActionResult { Forbidden = true };

        var isStanding = !assignment.Version.Model.IsRepeatable;
        if (assignment.Status is ServiceDataModelAssignmentStatus.Withdrawn
            or ServiceDataModelAssignmentStatus.NotApplicable)
        {
            return new CensusActionResult
            {
                Forbidden = true,
                ErrorMessage = "This assignment cannot be submitted in its current status."
            };
        }

        if (!isStanding &&
            assignment.Status is ServiceDataModelAssignmentStatus.Reviewed)
        {
            return new CensusActionResult
            {
                Forbidden = true,
                ErrorMessage = "This assignment cannot be submitted in its current status."
            };
        }

        var now = DateTime.UtcNow;

        // Standing models: re-submit after review keeps the same assignment and opens a fresh snapshot path.
        if (isStanding &&
            assignment.Status is ServiceDataModelAssignmentStatus.Reviewed
                or ServiceDataModelAssignmentStatus.ReviewDue)
        {
            OpenEditableRevision(assignment, email, now);
            assignment.Status = ServiceDataModelAssignmentStatus.InProgress;
        }

        var submission = EnsureCurrentSubmission(assignment, email, now);
        await RecalculateCompletionAsync(assignment);

        var sharedThemes = await _coreThemes.GetSharedStructureAsync();
        var coreFields = SharedCensusAnswerKeys.FlattenActiveFields(sharedThemes);
        var fields = coreFields.Select(SharedCensusAnswerKeys.AsTransientModelField).ToList();
        var answerInputs = BuildAnswerInputs(fields, submission, assignment.Product, out _);
        var answersByKey = answerInputs.ToDictionary(f => f.StableKey, StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            if (!ServiceDataModelCompletionCalculator.IsFieldVisible(field.VisibilityRuleJson, answersByKey))
                continue;
            if (!field.IsMandatory)
                continue;

            var answer = SharedCensusAnswerKeys.FindByStableKey(submission.Answers, field.StableKey);
            var input = answerInputs.First(a => a.FieldId == field.Id);
            if (!ServiceDataModelCompletionCalculator.IsAnswered(input) || answer is { IsValid: false })
            {
                return new CensusActionResult
                {
                    ErrorMessage = "All applicable mandatory fields must be answered and valid before submit.",
                    Form = await BuildFormAsync(assignment, email)
                };
            }
        }

        // Freeze submitted snapshot: mark current as submitted snapshot; keep as current working copy.
        submission.IsSubmittedSnapshot = true;
        submission.SubmittedUtc = now;
        submission.SubmittedByEmail = email.Trim();
        submission.UpdatedUtc = now;
        submission.UpdatedByEmail = email.Trim();

        assignment.Status = ServiceDataModelAssignmentStatus.Submitted;
        assignment.SubmittedUtc = now;
        assignment.SubmittedByEmail = email.Trim();
        assignment.UpdatedUtc = now;
        assignment.CurrentRevisionNumber = submission.RevisionNumber;

        WriteSdmAudit("ServiceDataModelAssignment", assignment.Id.ToString(), "Submit", email,
            JsonSerializer.Serialize(new { submission.RevisionNumber }));
        await _auditLogger.LogAsync("ServiceDataModelAssignment", assignment.Id.ToString(), "submit", email,
            JsonSerializer.Serialize(new { submission.RevisionNumber }));
        await _db.SaveChangesAsync();

        assignment = await LoadAssignmentGraphAsync(assignmentId, tracking: true);
        return new CensusActionResult
        {
            Success = true,
            Form = assignment == null ? null : await BuildFormAsync(assignment, email)
        };
    }

    public async Task<CensusActionResult> RequestChangesAsync(Guid assignmentId, string email, string note)
    {
        var assignment = await LoadAssignmentGraphAsync(assignmentId, tracking: true);
        if (assignment == null)
            return new CensusActionResult { NotFound = true };

        if (!await _access.CanReviewCensusAsync(_db, email, assignment.CMDBProductId))
            return new CensusActionResult { Forbidden = true };

        if (assignment.Status is not (ServiceDataModelAssignmentStatus.Submitted
            or ServiceDataModelAssignmentStatus.Reviewed
            or ServiceDataModelAssignmentStatus.ReviewDue))
        {
            return new CensusActionResult
            {
                ErrorMessage = "Changes can only be requested for submitted or reviewed assignments."
            };
        }

        var now = DateTime.UtcNow;
        assignment.Status = ServiceDataModelAssignmentStatus.ChangesRequested;
        assignment.ChangesRequestedUtc = now;
        assignment.ChangesRequestedNote = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        assignment.UpdatedUtc = now;

        // Open a new editable revision based on the last answers.
        OpenEditableRevision(assignment, email, now);

        WriteSdmAudit("ServiceDataModelAssignment", assignment.Id.ToString(), "RequestChanges", email,
            JsonSerializer.Serialize(new { hasNote = !string.IsNullOrWhiteSpace(note) }));
        await _auditLogger.LogAsync("ServiceDataModelAssignment", assignment.Id.ToString(), "request-changes",
            email, null);
        await _db.SaveChangesAsync();

        assignment = await LoadAssignmentGraphAsync(assignmentId, tracking: true);
        return new CensusActionResult
        {
            Success = true,
            Form = assignment == null ? null : await BuildFormAsync(assignment, email)
        };
    }

    public async Task<CensusActionResult> ReviewAsync(
        Guid assignmentId,
        string email,
        string? attestationNote)
    {
        var assignment = await LoadAssignmentGraphAsync(assignmentId, tracking: true);
        if (assignment == null)
            return new CensusActionResult { NotFound = true };

        if (!await _access.CanReviewCensusAsync(_db, email, assignment.CMDBProductId))
            return new CensusActionResult { Forbidden = true };

        if (assignment.Status != ServiceDataModelAssignmentStatus.Submitted &&
            assignment.Status != ServiceDataModelAssignmentStatus.ReviewDue)
        {
            return new CensusActionResult
            {
                ErrorMessage = "Only submitted assignments can be reviewed."
            };
        }

        var requiresAttestation = assignment.Version.Model.RequiresReviewerAttestation;
        if (requiresAttestation && string.IsNullOrWhiteSpace(attestationNote))
        {
            return new CensusActionResult
            {
                ErrorMessage = "A reviewer attestation note is required for this model.",
                Form = await BuildFormAsync(assignment, email)
            };
        }

        var now = DateTime.UtcNow;
        assignment.Status = ServiceDataModelAssignmentStatus.Reviewed;
        assignment.ReviewedUtc = now;
        assignment.ReviewedByEmail = email.Trim();
        assignment.ReviewerAttestationNote = string.IsNullOrWhiteSpace(attestationNote)
            ? null
            : attestationNote.Trim();
        assignment.UpdatedUtc = now;

        WriteSdmAudit("ServiceDataModelAssignment", assignment.Id.ToString(), "Review", email,
            JsonSerializer.Serialize(new { hasAttestation = !string.IsNullOrWhiteSpace(attestationNote) }));
        await _auditLogger.LogAsync("ServiceDataModelAssignment", assignment.Id.ToString(), "review", email, null);
        await _db.SaveChangesAsync();

        assignment = await LoadAssignmentGraphAsync(assignmentId, tracking: true);
        return new CensusActionResult
        {
            Success = true,
            Form = assignment == null ? null : await BuildFormAsync(assignment, email)
        };
    }

    public async Task RecalculateCompletionAsync(ServiceDataModelAssignment assignment)
    {
        var submission = assignment.Submissions.FirstOrDefault(s => s.IsCurrent);
        var themes = await _coreThemes.GetSharedStructureAsync();
        var coreFields = SharedCensusAnswerKeys.FlattenActiveFields(themes);
        var fields = coreFields.Select(SharedCensusAnswerKeys.AsTransientModelField).ToList();
        var inputs = BuildAnswerInputs(fields, submission, assignment.Product, out _);
        var themeByFieldId = themes
            .SelectMany(t => t.Fields.Select(f => (ThemeKey: t.StableKey, FieldId: f.Id)))
            .ToDictionary(x => x.FieldId, x => x.ThemeKey);
        var groupKeys = fields
            .Where(f => themeByFieldId.ContainsKey(f.Id))
            .Select(f => (themeByFieldId[f.Id], f.Id))
            .ToList();

        var result = ServiceDataModelCompletionCalculator.Calculate(inputs, groupKeys);
        assignment.FieldCompletionPercent = result.FieldCompletionPercent;
        assignment.MandatoryCompletionPercent = result.MandatoryCompletionPercent;
    }

    private async Task<CensusAssignmentFormViewModel> BuildFormAsync(
        ServiceDataModelAssignment assignment,
        string email)
    {
        var now = DateTime.UtcNow;
        var canEdit = await _access.CanEditCensusAsync(_db, email, assignment.CMDBProductId);
        var canReview = await _access.CanReviewCensusAsync(_db, email, assignment.CMDBProductId);
        var isStanding = !assignment.Version.Model.IsRepeatable;
        // Standing census stays editable after submit/review so people can keep answers current.
        var lockedForPeriodModel = !isStanding &&
            assignment.Status is ServiceDataModelAssignmentStatus.Submitted
                or ServiceDataModelAssignmentStatus.Reviewed;
        var readOnly = lockedForPeriodModel
            || assignment.Status is ServiceDataModelAssignmentStatus.Withdrawn
                or ServiceDataModelAssignmentStatus.NotApplicable;

        var submission = assignment.Submissions.FirstOrDefault(s => s.IsCurrent);
        var sharedThemes = await _coreThemes.GetSharedStructureAsync();
        var coreFields = SharedCensusAnswerKeys.FlattenActiveFields(sharedThemes);
        var fields = coreFields.Select(SharedCensusAnswerKeys.AsTransientModelField).ToList();
        var product = assignment.Product;
        var inputs = BuildAnswerInputs(fields, submission, product, out var suppressedKeys);
        var answersByKey = inputs.ToDictionary(f => f.StableKey, StringComparer.OrdinalIgnoreCase);
        var inputsByFieldId = inputs.ToDictionary(f => f.FieldId);
        var themeByFieldId = sharedThemes
            .SelectMany(t => t.Fields.Select(f => (ThemeKey: t.StableKey, FieldId: f.Id)))
            .ToDictionary(x => x.FieldId, x => x.ThemeKey);
        var completion = ServiceDataModelCompletionCalculator.Calculate(
            inputs,
            fields
                .Where(f => themeByFieldId.ContainsKey(f.Id))
                .Select(f => (themeByFieldId[f.Id], f.Id)));

        var completedThemeKeys = await _db.ServiceDataModelThemeCompletions.AsNoTracking()
            .Where(c => c.ServiceDataModelAssignmentId == assignment.Id)
            .Select(c => c.ThemeStableKey)
            .ToListAsync();
        var completedSet = new HashSet<string>(completedThemeKeys, StringComparer.OrdinalIgnoreCase);

        var groups = new List<CensusFormGroupViewModel>();
        foreach (var t in sharedThemes.OrderBy(x => x.SortOrder))
        {
            var groupFields = new List<CensusFormFieldViewModel>();
            foreach (var f in t.Fields.Where(x => !x.IsDisabled).OrderBy(x => x.SortOrder))
            {
                var projected = SharedCensusAnswerKeys.AsTransientModelField(f);
                NormalizeLegacyCanonicalKey(projected);
                var answer = SharedCensusAnswerKeys.FindByStableKey(
                    submission?.Answers ?? Array.Empty<ServiceDataModelAnswer>(),
                    f.StableKey);
                inputsByFieldId.TryGetValue(f.Id, out var effective);
                var visible = ServiceDataModelCompletionCalculator.IsFieldVisible(
                    f.VisibilityRuleJson, answersByKey);
                var suppressed = suppressedKeys.Contains(f.StableKey);
                var registerValue = string.IsNullOrWhiteSpace(projected.CanonicalAttributeKey)
                    ? null
                    : ResolveCanonicalDisplayValue(product, projected.CanonicalAttributeKey);
                var isTitle = ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey(projected.CanonicalAttributeKey);
                var label = f.Label;
                var guidance = f.Guidance;
                if (isTitle && string.Equals(f.StableKey, "confirm-title", StringComparison.OrdinalIgnoreCase))
                {
                    label = "Service name";
                    guidance = "Taken from the Service Register and cannot be changed here.";
                }

                var valueJson = effective?.ValueJson ?? answer?.ValueJson;
                var options = await ResolveChoiceOptionsAsync(f);
                var listItems = await ResolveListItemsAsync(f.FieldType, valueJson);

                groupFields.Add(new CensusFormFieldViewModel
                {
                    Id = f.Id,
                    StableKey = f.StableKey,
                    Label = label,
                    Guidance = guidance,
                    FieldType = f.FieldType,
                    IsMandatory = f.IsMandatory,
                    CountsTowardsCompletion = f.CountsTowardsCompletion,
                    SortOrder = f.SortOrder,
                    IsDisabled = f.IsDisabled,
                    VisibilityRuleJson = f.VisibilityRuleJson,
                    CanonicalAttributeKey = projected.CanonicalAttributeKey,
                    CurrentRegisterValue = string.IsNullOrWhiteSpace(registerValue) ? null : registerValue,
                    ValueJson = valueJson,
                    IsValid = answer?.IsValid ?? true,
                    ValidationMessage = answer?.ValidationMessage,
                    IsVisible = visible && !suppressed,
                    IsSuppressedFromForm = suppressed,
                    IsReadOnlyRegisterValue = isTitle,
                    AllowMultiple = f.AllowMultiple,
                    Options = options,
                    ListItems = listItems
                });
            }

            completion.GroupBreakdown.TryGetValue(t.StableKey, out var groupCompletion);
            var answered = groupCompletion?.AnsweredCountingFields ?? 0;
            var applicable = groupCompletion?.ApplicableCountingFields ?? 0;
            var isMarkedComplete = completedSet.Contains(t.StableKey);
            var action = CensusThemeStatusHelper.Resolve(isMarkedComplete, answered, applicable);
            groups.Add(new CensusFormGroupViewModel
            {
                Id = t.Id,
                StableKey = t.StableKey,
                Name = t.Name,
                Guidance = t.Guidance,
                SortOrder = t.SortOrder,
                IsDisabled = !t.IsActive,
                FieldCompletionPercent = groupCompletion?.FieldCompletionPercent,
                IsMarkedComplete = isMarkedComplete,
                ThemeStatusLabel = action.StatusLabel,
                ThemeActionLabel = action.ActionLabel,
                ThemeStatusTagClass = action.TagClass,
                Fields = groupFields
            });
        }

        var proposed = await _db.ServiceDataModelProposedRegisterChanges.AsNoTracking()
            .Where(p => p.ServiceDataModelAssignmentId == assignment.Id)
            .OrderByDescending(p => p.CreatedUtc)
            .ToListAsync();

        // Title is never proposed; URL/description are applied to the product — hide any legacy rows.
        proposed = proposed
            .Where(p =>
                !ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey(p.CanonicalAttributeKey) &&
                !ServiceDataModelCanonicalPrefill.IsDirectProductUpdateCanonicalKey(p.CanonicalAttributeKey))
            .ToList();

        var nextIncomplete = groups.FirstOrDefault(g =>
            !g.IsMarkedComplete &&
            !string.Equals(g.ThemeStatusLabel, CensusThemeStatusHelper.NotApplicable, StringComparison.Ordinal));

        return new CensusAssignmentFormViewModel
        {
            AssignmentId = assignment.Id,
            ProductId = assignment.CMDBProductId,
            ProductTitle = product?.Title ?? string.Empty,
            ModelId = assignment.Version.Model.Id,
            ModelName = assignment.Version.Model.Name,
            ModelStableKey = assignment.Version.Model.StableKey,
            VersionId = assignment.Version.Id,
            VersionNumber = assignment.Version.VersionNumber,
            PeriodLabel = assignment.PeriodLabel,
            Status = assignment.Status,
            DueUtc = assignment.DueUtc,
            IsOverdue = ServiceDataModelCompletionCalculator.IsOverdue(assignment.Status, assignment.DueUtc, now),
            FieldCompletionPercent = completion.FieldCompletionPercent,
            MandatoryCompletionPercent = completion.MandatoryCompletionPercent,
            UpdatedUtc = assignment.UpdatedUtc,
            CurrentRevisionNumber = assignment.CurrentRevisionNumber,
            RequiresReviewerAttestation = assignment.Version.Model.RequiresReviewerAttestation,
            ChangesRequestedNote = assignment.ChangesRequestedNote,
            ReviewerAttestationNote = assignment.ReviewerAttestationNote,
            CanEdit = canEdit && !readOnly,
            CanReview = canReview,
            IsReadOnly = readOnly || !canEdit,
            NextIncompleteGroupId = nextIncomplete?.Id,
            Groups = groups,
            ProposedChanges = proposed.Select(p => new CensusProposedChangeViewModel
            {
                Id = p.Id,
                FieldId = p.ServiceDataModelFieldId ?? Guid.Empty,
                CanonicalAttributeKey = p.CanonicalAttributeKey,
                CurrentRegisterValue = p.CurrentRegisterValue,
                ProposedValue = p.ProposedValue,
                Status = p.Status
            }).ToList()
        };
    }

    private async Task UpsertThemeCompletionAsync(
        Guid assignmentId,
        string themeStableKey,
        string email,
        DateTime now)
    {
        var existing = await _db.ServiceDataModelThemeCompletions
            .FirstOrDefaultAsync(c =>
                c.ServiceDataModelAssignmentId == assignmentId &&
                c.ThemeStableKey == themeStableKey);

        if (existing == null)
        {
            _db.ServiceDataModelThemeCompletions.Add(new ServiceDataModelThemeCompletion
            {
                ServiceDataModelAssignmentId = assignmentId,
                ThemeStableKey = themeStableKey,
                CompletedUtc = now,
                CompletedByEmail = email.Trim()
            });
            return;
        }

        existing.CompletedUtc = now;
        existing.CompletedByEmail = email.Trim();
    }

    private async Task ClearThemeCompletionsAsync(Guid assignmentId, IReadOnlyCollection<string> themeStableKeys)
    {
        if (themeStableKeys.Count == 0)
            return;

        var keys = themeStableKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (keys.Count == 0)
            return;

        var rows = await _db.ServiceDataModelThemeCompletions
            .Where(c =>
                c.ServiceDataModelAssignmentId == assignmentId &&
                keys.Contains(c.ThemeStableKey))
            .ToListAsync();
        if (rows.Count > 0)
            _db.ServiceDataModelThemeCompletions.RemoveRange(rows);
    }

    private static bool AnswerValuesEqual(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) && string.IsNullOrWhiteSpace(right))
            return true;
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
            return false;
        return string.Equals(left.Trim(), right.Trim(), StringComparison.Ordinal);
    }

    private async Task UpsertProposedRegisterChangeAsync(
        ServiceDataModelAssignment assignment,
        ServiceDataModelField field,
        string fieldStableKey,
        string? valueJson,
        string email,
        DateTime now)
    {
        var key = field.CanonicalAttributeKey!.Trim();

        // Title is never proposed; URL/description are applied to the product instead.
        if (ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey(key) ||
            ServiceDataModelCanonicalPrefill.IsDirectProductUpdateCanonicalKey(key))
        {
            await ClearPendingProposedRegisterChangeAsync(assignment.Id, fieldStableKey);
            return;
        }

        var current = ResolveCanonicalDisplayValue(assignment.Product, key);
        var proposed = ExtractDisplayValue(valueJson);

        var existing = await _db.ServiceDataModelProposedRegisterChanges
            .Where(p =>
                p.ServiceDataModelAssignmentId == assignment.Id &&
                p.Status == ServiceDataModelProposedChangeStatus.Pending &&
                (p.FieldStableKey == fieldStableKey ||
                 (p.FieldStableKey == null && p.ServiceDataModelFieldId == field.Id)))
            .OrderByDescending(p => p.CreatedUtc)
            .FirstOrDefaultAsync();

        // Confirming the existing register value is not a change proposal.
        if (ServiceDataModelCanonicalPrefill.IsSameAsRegister(proposed, current))
        {
            if (existing != null)
                _db.ServiceDataModelProposedRegisterChanges.Remove(existing);
            return;
        }

        if (existing == null)
        {
            _db.ServiceDataModelProposedRegisterChanges.Add(new ServiceDataModelProposedRegisterChange
            {
                ServiceDataModelAssignmentId = assignment.Id,
                ServiceDataModelFieldId = null,
                FieldStableKey = fieldStableKey,
                CanonicalAttributeKey = key,
                CurrentRegisterValue = current,
                ProposedValue = proposed,
                Status = ServiceDataModelProposedChangeStatus.Pending,
                CreatedUtc = now,
                CreatedByEmail = email.Trim()
            });
        }
        else
        {
            existing.FieldStableKey = fieldStableKey;
            existing.ServiceDataModelFieldId = null;
            existing.CurrentRegisterValue = current;
            existing.ProposedValue = proposed;
            existing.CreatedByEmail = email.Trim();
        }
    }

    private async Task ClearPendingProposedRegisterChangeAsync(Guid assignmentId, string fieldStableKey)
    {
        var pending = await _db.ServiceDataModelProposedRegisterChanges
            .Where(p =>
                p.ServiceDataModelAssignmentId == assignmentId &&
                p.Status == ServiceDataModelProposedChangeStatus.Pending &&
                p.FieldStableKey == fieldStableKey)
            .ToListAsync();
        if (pending.Count > 0)
            _db.ServiceDataModelProposedRegisterChanges.RemoveRange(pending);
    }

    /// <summary>
    /// Forces title canonical answers to the register name and clears any pending rename proposals.
    /// </summary>
    private async Task SyncReadOnlyTitleAnswersAsync(
        ServiceDataModelAssignment assignment,
        ServiceDataModelSubmission submission,
        IReadOnlyList<CoreCensusTheme> sharedThemes,
        string email,
        DateTime now)
    {
        foreach (var coreField in SharedCensusAnswerKeys.FlattenActiveFields(sharedThemes))
        {
            var field = SharedCensusAnswerKeys.AsTransientModelField(coreField);
            NormalizeLegacyCanonicalKey(field);
            if (!ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey(field.CanonicalAttributeKey))
                continue;

            var registerTitle = ResolveCanonicalDisplayValue(assignment.Product, field.CanonicalAttributeKey!);
            var valueJson = string.IsNullOrWhiteSpace(registerTitle)
                ? null
                : ServiceDataModelCanonicalPrefill.ToValueJson(registerTitle, field.FieldType);

            var existing = SharedCensusAnswerKeys.FindByStableKey(submission.Answers, coreField.StableKey);
            if (existing == null)
            {
                if (string.IsNullOrWhiteSpace(valueJson))
                    continue;
                existing = new ServiceDataModelAnswer
                {
                    ServiceDataModelSubmissionId = submission.Id,
                    ServiceDataModelFieldId = null,
                    FieldStableKey = coreField.StableKey
                };
                submission.Answers.Add(existing);
                _db.ServiceDataModelAnswers.Add(existing);
            }
            else
            {
                existing.FieldStableKey = coreField.StableKey;
                existing.ServiceDataModelFieldId = null;
            }

            existing.ValueJson = valueJson;
            existing.IsValid = true;
            existing.ValidationMessage = null;
            existing.UpdatedUtc = now;
            existing.UpdatedByEmail = email.Trim();
            await ClearPendingProposedRegisterChangeAsync(assignment.Id, coreField.StableKey);
        }
    }

    private void OpenEditableRevision(
        ServiceDataModelAssignment assignment,
        string email,
        DateTime now)
    {
        var current = assignment.Submissions.FirstOrDefault(s => s.IsCurrent);
        if (current == null)
            return;

        current.IsCurrent = false;
        var nextRev = (assignment.Submissions.Max(s => (int?)s.RevisionNumber) ?? 0) + 1;
        var newSubmission = new ServiceDataModelSubmission
        {
            ServiceDataModelAssignmentId = assignment.Id,
            RevisionNumber = nextRev,
            IsCurrent = true,
            IsSubmittedSnapshot = false,
            CreatedUtc = now,
            CreatedByEmail = email.Trim(),
            UpdatedUtc = now,
            UpdatedByEmail = email.Trim()
        };
        foreach (var answer in current.Answers)
        {
            newSubmission.Answers.Add(new ServiceDataModelAnswer
            {
                ServiceDataModelFieldId = null,
                FieldStableKey = SharedCensusAnswerKeys.ResolveStableKey(answer),
                ValueJson = answer.ValueJson,
                IsValid = answer.IsValid,
                ValidationMessage = answer.ValidationMessage,
                UpdatedUtc = now,
                UpdatedByEmail = email.Trim()
            });
        }

        assignment.Submissions.Add(newSubmission);
        _db.ServiceDataModelSubmissions.Add(newSubmission);
        assignment.CurrentRevisionNumber = nextRev;
    }

    private ServiceDataModelSubmission EnsureCurrentSubmission(
        ServiceDataModelAssignment assignment,
        string email,
        DateTime now)
    {
        var current = assignment.Submissions.FirstOrDefault(s => s.IsCurrent);
        if (current != null)
            return current;

        var nextRev = (assignment.Submissions.Max(s => (int?)s.RevisionNumber) ?? 0) + 1;
        current = new ServiceDataModelSubmission
        {
            ServiceDataModelAssignmentId = assignment.Id,
            RevisionNumber = nextRev,
            IsCurrent = true,
            IsSubmittedSnapshot = false,
            CreatedUtc = now,
            CreatedByEmail = email.Trim(),
            UpdatedUtc = now,
            UpdatedByEmail = email.Trim()
        };
        assignment.Submissions.Add(current);
        _db.ServiceDataModelSubmissions.Add(current);
        assignment.CurrentRevisionNumber = nextRev;
        return current;
    }

    private async Task<ServiceDataModelAssignment?> LoadAssignmentGraphAsync(
        Guid assignmentId,
        bool tracking = false)
    {
        IQueryable<ServiceDataModelAssignment> query = _db.ServiceDataModelAssignments
            .Include(a => a.Product)
            .ThenInclude(p => p.Phase)
            .Include(a => a.Version)
            .ThenInclude(v => v.Model)
            .Include(a => a.Submissions)
            .ThenInclude(s => s.Answers)
            .ThenInclude(ans => ans.Field);

        if (!tracking)
            query = query.AsNoTracking();

        return await query.FirstOrDefaultAsync(a => a.Id == assignmentId);
    }

    private void WriteSdmAudit(
        string entityType,
        string entityId,
        string action,
        string? actorEmail,
        string? metadataJson)
    {
        _db.ServiceDataModelAuditEvents.Add(new ServiceDataModelAuditEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            ActorEmail = string.IsNullOrWhiteSpace(actorEmail) ? null : actorEmail.Trim(),
            OccurredUtc = DateTime.UtcNow,
            MetadataJson = metadataJson is { Length: > 4000 } ? metadataJson[..4000] : metadataJson
        });
    }

    private static CensusAssignmentListItemViewModel MapListItem(
        ServiceDataModelAssignment a,
        string productTitle,
        DateTime now)
    {
        var product = a.Product;
        var businessAreas = product?.BusinessAreas?
            .Where(b => b.FipsBusinessArea != null)
            .Select(b => b.FipsBusinessArea.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        var types = product?.Types?
            .Where(t => t.FipsType != null)
            .Select(t => t.FipsType.Name)
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        var serviceOwner = product?.Contacts?
            .Where(c =>
                c.FipsContactRole != null &&
                string.Equals(c.FipsContactRole.Name, "Service Owner", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.UserName ?? c.UserEmail ?? "")
            .FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));

        return new CensusAssignmentListItemViewModel
        {
            AssignmentId = a.Id,
            ProductId = a.CMDBProductId,
            ProductTitle = productTitle,
            ModelName = a.Version.Model.Name,
            ModelStableKey = a.Version.Model.StableKey,
            PeriodLabel = a.PeriodLabel,
            Status = a.Status,
            FieldCompletionPercent = a.FieldCompletionPercent,
            MandatoryCompletionPercent = a.MandatoryCompletionPercent,
            DueUtc = a.DueUtc,
            IsOverdue = ServiceDataModelCompletionCalculator.IsOverdue(a.Status, a.DueUtc, now),
            UpdatedUtc = a.UpdatedUtc,
            LastAnsweredUtc = a.LastAnsweredUtc,
            ReviewedUtc = a.ReviewedUtc,
            PhaseName = product?.Phase?.Name,
            BusinessAreaDisplay = businessAreas.Count == 0 ? null : string.Join(", ", businessAreas),
            TypesDisplay = types.Count == 0 ? null : string.Join(", ", types),
            ServiceOwner = string.IsNullOrWhiteSpace(serviceOwner) ? null : serviceOwner
        };
    }

    /// <summary>
    /// Older standing-census seeds lacked CanonicalAttributeKey on service-summary.
    /// </summary>
    private static void NormalizeLegacyCanonicalKey(ServiceDataModelField field)
    {
        if (!string.IsNullOrWhiteSpace(field.CanonicalAttributeKey))
            return;
        if (string.Equals(field.StableKey, "service-summary", StringComparison.OrdinalIgnoreCase))
            field.CanonicalAttributeKey = "description";
    }

    private static List<ServiceDataModelFieldAnswerInput> BuildAnswerInputs(
        IEnumerable<ServiceDataModelField> fields,
        ServiceDataModelSubmission? submission,
        CMDBProduct? product,
        out IReadOnlySet<string> suppressedFieldKeys)
    {
        var fieldList = fields.ToList();
        var answers = submission?.Answers.ToList() ?? new List<ServiceDataModelAnswer>();

        var baseInputs = fieldList.Select(f =>
        {
            var answer = SharedCensusAnswerKeys.FindByStableKey(answers, f.StableKey);
            return new ServiceDataModelFieldAnswerInput(
                f.Id,
                f.StableKey,
                f.FieldType,
                f.IsMandatory,
                f.CountsTowardsCompletion,
                f.VisibilityRuleJson,
                answer?.ValueJson,
                answer?.IsValid ?? true,
                f.IsDisabled);
        }).ToList();

        return ServiceDataModelCanonicalPrefill.Apply(baseInputs, fieldList, product, out suppressedFieldKeys)
            .ToList();
    }

    /// <summary>
    /// Writes register-driven prefill into the current submission for fields that still have no answer
    /// (including suppressed has-URL controllers synthesised as Yes).
    /// </summary>
    private async Task ApplyAndPersistCanonicalPrefillAsync(
        ServiceDataModelAssignment assignment,
        ServiceDataModelSubmission submission,
        IReadOnlyList<CoreCensusTheme> sharedThemes,
        string email,
        DateTime now)
    {
        var fields = SharedCensusAnswerKeys.FlattenActiveFields(sharedThemes)
            .Select(SharedCensusAnswerKeys.AsTransientModelField)
            .ToList();
        foreach (var field in fields)
            NormalizeLegacyCanonicalKey(field);

        var inputs = BuildAnswerInputs(fields, submission, assignment.Product, out _);
        foreach (var input in inputs)
        {
            if (!ServiceDataModelCompletionCalculator.IsAnswered(input))
                continue;

            var existing = SharedCensusAnswerKeys.FindByStableKey(submission.Answers, input.StableKey);
            if (existing != null && !string.IsNullOrWhiteSpace(existing.ValueJson))
                continue;

            if (existing == null)
            {
                existing = new ServiceDataModelAnswer
                {
                    ServiceDataModelSubmissionId = submission.Id,
                    ServiceDataModelFieldId = null,
                    FieldStableKey = input.StableKey
                };
                submission.Answers.Add(existing);
                _db.ServiceDataModelAnswers.Add(existing);
            }
            else
            {
                existing.FieldStableKey = input.StableKey;
                existing.ServiceDataModelFieldId = null;
            }

            existing.ValueJson = input.ValueJson;
            existing.IsValid = true;
            existing.ValidationMessage = null;
            existing.UpdatedUtc = now;
            existing.UpdatedByEmail = email.Trim();
        }

        await Task.CompletedTask;
    }

    internal static string ResolveCanonicalDisplayValue(CMDBProduct? product, string key)
    {
        if (product == null)
            return "(not mapped)";

        var resolved = ServiceDataModelCanonicalPrefill.ResolveDisplayValue(product, key);
        // Unknown keys stay explicitly unmapped for admin diagnostics.
        if (string.IsNullOrEmpty(resolved) &&
            key.Trim().ToLowerInvariant() is not ("title" or "name" or "producttitle" or "product-title"
                or "url" or "producturl" or "product-url"
                or "description" or "summary" or "userdescription" or "user-description" or "phase"))
            return "(not mapped)";

        return resolved;
    }

    private static string? ExtractDisplayValue(string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson))
            return null;
        try
        {
            using var doc = JsonDocument.Parse(valueJson);
            var el = doc.RootElement;
            return el.ValueKind switch
            {
                JsonValueKind.String => el.GetString(),
                JsonValueKind.Number => el.GetRawText(),
                JsonValueKind.True => "Yes",
                JsonValueKind.False => "No",
                _ => el.GetRawText()
            };
        }
        catch (JsonException)
        {
            return valueJson.Trim();
        }
    }

    private async Task<IReadOnlyList<ServiceDataModelFieldOptionViewModel>> ResolveChoiceOptionsAsync(
        CoreCensusThemeField field)
    {
        if (field.FieldType is ServiceDataModelFieldType.SingleChoice
            or ServiceDataModelFieldType.MultipleChoice
            or ServiceDataModelFieldType.Lookup)
        {
            var fromLookup = await CensusAdminLookupOptions.ResolveAsync(_db, field.OptionsLookupKey);
            if (fromLookup.Count > 0)
            {
                return fromLookup
                    .Select(o => new ServiceDataModelFieldOptionViewModel
                    {
                        ValueKey = o.ValueKey,
                        Label = o.Label,
                        SortOrder = o.SortOrder
                    })
                    .ToList();
            }
        }

        return field.Options.OrderBy(o => o.SortOrder)
            .Select(o => new ServiceDataModelFieldOptionViewModel
            {
                Id = o.Id,
                ValueKey = o.ValueKey,
                Label = o.Label,
                SortOrder = o.SortOrder
            }).ToList();
    }

    private async Task<IReadOnlyList<CensusListItemViewModel>> ResolveListItemsAsync(
        ServiceDataModelFieldType fieldType,
        string? valueJson)
    {
        var ids = ParseIdList(valueJson);
        if (ids.Count == 0)
            return Array.Empty<CensusListItemViewModel>();

        if (fieldType == ServiceDataModelFieldType.Services)
        {
            var products = await _db.CMDBProducts.AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .Select(p => new { p.Id, p.Title, p.CMDBID })
                .ToListAsync();
            var byId = products.ToDictionary(p => p.Id);
            return ids
                .Where(id => byId.ContainsKey(id))
                .Select(id => new CensusListItemViewModel
                {
                    Id = id.ToString(),
                    Label = byId[id].Title ?? id.ToString(),
                    Subtitle = byId[id].CMDBID
                })
                .ToList();
        }

        if (fieldType == ServiceDataModelFieldType.ServiceLines)
        {
            var lines = await _db.ServiceLines.AsNoTracking()
                .Where(s => ids.Contains(s.Id))
                .Select(s => new { s.Id, s.Name, s.Slug })
                .ToListAsync();
            var byId = lines.ToDictionary(s => s.Id);
            return ids
                .Where(id => byId.ContainsKey(id))
                .Select(id => new CensusListItemViewModel
                {
                    Id = id.ToString(),
                    Label = byId[id].Name,
                    Subtitle = byId[id].Slug
                })
                .ToList();
        }

        return Array.Empty<CensusListItemViewModel>();
    }

    private static List<Guid> ParseIdList(string? valueJson)
    {
        var ids = new List<Guid>();
        foreach (var raw in CensusAnswerValueNormalizer.ParseStringList(valueJson))
        {
            if (Guid.TryParse(raw, out var id))
                ids.Add(id);
        }
        return ids;
    }

    private static (bool IsValid, string? Message) ValidateAnswer(
        ServiceDataModelField field,
        string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson))
            return (true, null);

        try
        {
            using var doc = JsonDocument.Parse(valueJson);
            var el = doc.RootElement;

            switch (field.FieldType)
            {
                case ServiceDataModelFieldType.Number:
                    if (el.ValueKind != JsonValueKind.Number &&
                        !(el.ValueKind == JsonValueKind.String && decimal.TryParse(el.GetString(), out _)))
                        return (false, "Enter a number.");
                    var number = el.ValueKind == JsonValueKind.Number
                        ? el.GetDecimal()
                        : decimal.Parse(el.GetString()!);
                    if (field.MinNumber.HasValue && number < field.MinNumber.Value)
                        return (false, $"Enter a number of at least {field.MinNumber}.");
                    if (field.MaxNumber.HasValue && number > field.MaxNumber.Value)
                        return (false, $"Enter a number of at most {field.MaxNumber}.");
                    break;

                case ServiceDataModelFieldType.YesNo:
                    if (el.ValueKind is not (JsonValueKind.True or JsonValueKind.False or JsonValueKind.String))
                        return (false, "Select yes or no.");
                    if (el.ValueKind == JsonValueKind.String)
                    {
                        var s = el.GetString();
                        if (!string.Equals(s, "Yes", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s, "No", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s, "true", StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                            return (false, "Select yes or no.");
                    }
                    break;

                case ServiceDataModelFieldType.Url:
                    var url = el.ValueKind == JsonValueKind.String ? el.GetString() : el.GetRawText();
                    if (!string.IsNullOrWhiteSpace(url) &&
                        !Uri.TryCreate(url, UriKind.Absolute, out _))
                        return (false, "Enter a valid URL.");
                    break;
            }

            if (!string.IsNullOrWhiteSpace(field.ValidationPattern) &&
                el.ValueKind == JsonValueKind.String)
            {
                var text = el.GetString() ?? string.Empty;
                if (!Regex.IsMatch(text, field.ValidationPattern))
                    return (false, "Enter a value in the required format.");
            }
        }
        catch (JsonException)
        {
            // Plain text allowed for Text/Multiline
            if (field.FieldType is ServiceDataModelFieldType.Number)
                return (false, "Enter a number.");
        }

        return (true, null);
    }

    private static DateTime TruncateToSeconds(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, DateTimeKind.Utc);
}
