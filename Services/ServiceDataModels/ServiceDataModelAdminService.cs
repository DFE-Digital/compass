using System.Text.Json;
using Compass.Data;
using Compass.Helpers;
using Compass.Models.Fips;
using Compass.Models.ServiceDataModels;
using Compass.Services;
using Compass.ViewModels.Modern.ServiceDataModels;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.ServiceDataModels;

public sealed class ServiceDataModelAdminService : IServiceDataModelAdminService
{
    private readonly CompassDbContext _db;
    private readonly IServiceDataModelAccessService _access;
    private readonly IAuditLogger _auditLogger;

    public ServiceDataModelAdminService(
        CompassDbContext db,
        IServiceDataModelAccessService access,
        IAuditLogger auditLogger)
    {
        _db = db;
        _access = access;
        _auditLogger = auditLogger;
    }

    public async Task<ServiceDataModelListViewModel> ListModelsAsync(string actorEmail)
    {
        var canManage = await _access.CanManageModelsAsync(actorEmail);
        var models = await _db.ServiceDataModels
            .AsNoTracking()
            .OrderBy(m => m.Name)
            .ToListAsync();

        var modelIds = models.Select(m => m.Id).ToList();
        var versions = await _db.ServiceDataModelVersions
            .AsNoTracking()
            .Where(v => modelIds.Contains(v.ServiceDataModelId))
            .ToListAsync();

        var publishedVersionIds = versions
            .Where(v => v.Status == ServiceDataModelLifecycleStatus.Published)
            .Select(v => v.Id)
            .ToList();

        var assignedCounts = publishedVersionIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _db.ServiceDataModelAssignments
                .AsNoTracking()
                .Where(a => publishedVersionIds.Contains(a.ServiceDataModelVersionId))
                .GroupBy(a => a.ServiceDataModelVersionId)
                .Select(g => new { VersionId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.VersionId, x => x.Count);

        var rows = models.Select(m =>
        {
            var modelVersions = versions
                .Where(v => v.ServiceDataModelId == m.Id)
                .OrderByDescending(v => v.VersionNumber)
                .ToList();
            var current = modelVersions.FirstOrDefault();
            var latestPublished = modelVersions
                .FirstOrDefault(v => v.Status == ServiceDataModelLifecycleStatus.Published);
            var assigned = latestPublished != null && assignedCounts.TryGetValue(latestPublished.Id, out var c)
                ? c
                : 0;

            return new ServiceDataModelListRowViewModel
            {
                Id = m.Id,
                Name = m.Name,
                StableKey = m.StableKey,
                OwnerDisplayName = m.OwnerDisplayName,
                OwnerEmail = m.OwnerEmail,
                LifecycleStatus = m.LifecycleStatus,
                CurrentVersionNumber = current?.VersionNumber,
                CurrentVersionStatus = current?.Status,
                AssignedCount = assigned,
                UpdatedUtc = m.UpdatedUtc
            };
        }).ToList();

        return new ServiceDataModelListViewModel
        {
            Rows = rows,
            CanManage = canManage
        };
    }

    public async Task<Guid> CreateModelAsync(
        string stableKey,
        string name,
        string? description,
        string? ownerDisplayName,
        string? ownerEmail,
        string? classification,
        bool isRepeatable,
        string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            throw new UnauthorizedAccessException("Not authorised to manage service data models.");

        var key = (stableKey ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Stable key and name are required.");

        if (await _db.ServiceDataModels.AnyAsync(m => m.StableKey == key))
            throw new InvalidOperationException($"A model with stable key '{key}' already exists.");

        var now = DateTime.UtcNow;
        var model = new ServiceDataModel
        {
            StableKey = key,
            Name = name.Trim(),
            Description = NullIfWhiteSpace(description),
            OwnerDisplayName = NullIfWhiteSpace(ownerDisplayName),
            OwnerEmail = NullIfWhiteSpace(ownerEmail),
            Classification = NullIfWhiteSpace(classification),
            IsRepeatable = isRepeatable,
            LifecycleStatus = ServiceDataModelLifecycleStatus.Draft,
            CreatedUtc = now,
            CreatedByEmail = actorEmail.Trim(),
            UpdatedUtc = now,
            UpdatedByEmail = actorEmail.Trim()
        };

        var version = new ServiceDataModelVersion
        {
            ServiceDataModelId = model.Id,
            VersionNumber = 1,
            Status = ServiceDataModelLifecycleStatus.Draft,
            CreatedUtc = now,
            CreatedByEmail = actorEmail.Trim()
        };

        _db.ServiceDataModels.Add(model);
        _db.ServiceDataModelVersions.Add(version);
        await WriteSdmAuditAsync("ServiceDataModel", model.Id.ToString(), "Create", actorEmail,
            JsonSerializer.Serialize(new { model.StableKey, model.Name }));
        await _auditLogger.LogAsync("ServiceDataModel", model.Id.ToString(), "create", actorEmail,
            JsonSerializer.Serialize(new { model.StableKey, model.Name }));
        await _db.SaveChangesAsync();
        return model.Id;
    }

    public async Task<ServiceDataModelDetailViewModel?> GetModelDetailAsync(Guid modelId, string actorEmail)
    {
        var model = await _db.ServiceDataModels
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modelId);
        if (model == null)
            return null;

        var versions = await _db.ServiceDataModelVersions
            .AsNoTracking()
            .Where(v => v.ServiceDataModelId == modelId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();

        var draft = versions.FirstOrDefault(v => v.Status == ServiceDataModelLifecycleStatus.Draft);
        var published = versions.FirstOrDefault(v => v.Status == ServiceDataModelLifecycleStatus.Published);
        // Prefer the live published definition so overview themes match respondents after publish.
        // Fall back to draft only when nothing is published yet.
        var structureVersion = ServiceDataModelStructureVersion.Resolve(published, draft);

        var draftGroups = Array.Empty<ServiceDataModelGroupViewModel>();
        var groupCount = 0;
        var fieldCount = 0;
        if (structureVersion != null)
        {
            var groups = await _db.ServiceDataModelGroups
                .AsNoTracking()
                .Include(g => g.Fields)
                .ThenInclude(f => f.Options)
                .Where(g => g.ServiceDataModelVersionId == structureVersion.Id)
                .OrderBy(g => g.SortOrder)
                .ThenBy(g => g.Name)
                .ToListAsync();

            draftGroups = groups.Select(MapGroup).ToArray();
            groupCount = groups.Count;
            fieldCount = groups.Sum(g => g.Fields.Count);
        }

        var rules = await _db.ServiceDataModelApplicabilityRules
            .AsNoTracking()
            .Where(r => r.ServiceDataModelId == modelId)
            .ToListAsync();

        var explicitServices = await _db.ServiceDataModelExplicitServices
            .AsNoTracking()
            .Include(e => e.Product)
            .Where(e => e.ServiceDataModelId == modelId)
            .ToListAsync();

        var assignedCount = 0;
        if (published != null)
        {
            assignedCount = await _db.ServiceDataModelAssignments
                .AsNoTracking()
                .CountAsync(a => a.ServiceDataModelVersionId == published.Id);
        }

        var canManage = await _access.CanManageModelsAsync(actorEmail);
        return new ServiceDataModelDetailViewModel
        {
            Id = model.Id,
            StableKey = model.StableKey,
            Name = model.Name,
            Description = model.Description,
            OwnerDisplayName = model.OwnerDisplayName,
            OwnerEmail = model.OwnerEmail,
            Classification = model.Classification,
            IsRepeatable = model.IsRepeatable,
            IsReportable = model.IsReportable,
            LifecycleStatus = model.LifecycleStatus,
            RequiresReviewerAttestation = model.RequiresReviewerAttestation,
            ProgressLabelThresholdPercent = model.ProgressLabelThresholdPercent,
            DefaultDueDaysAfterPublish = model.DefaultDueDaysAfterPublish,
            ReviewCadenceLabel = model.ReviewCadenceLabel,
            ApplicabilityMode = model.ApplicabilityMode,
            CreatedUtc = model.CreatedUtc,
            UpdatedUtc = model.UpdatedUtc,
            UpdatedByEmail = model.UpdatedByEmail,
            DraftVersionId = draft?.Id,
            DraftVersionNumber = draft?.VersionNumber,
            PublishedVersionId = published?.Id,
            PublishedVersionNumber = published?.VersionNumber,
            StructureVersionId = structureVersion?.Id,
            StructureVersionNumber = structureVersion?.VersionNumber,
            StructureVersionStatus = structureVersion?.Status,
            AssignedCount = assignedCount,
            GroupCount = groupCount,
            FieldCount = fieldCount,
            DraftGroups = draftGroups,
            CanEditStructure = canManage &&
                structureVersion != null &&
                model.LifecycleStatus != ServiceDataModelLifecycleStatus.Retired &&
                ServiceDataModelStructureVersion.AllowsEdits(structureVersion.Status),
            ApplicabilityRules = rules.Select(r => new ServiceDataModelApplicabilityRuleViewModel
            {
                Id = r.Id,
                ProductStatus = r.ProductStatus,
                PhaseId = r.PhaseId,
                FipsTypeId = r.FipsTypeId,
                FipsBusinessAreaId = r.FipsBusinessAreaId,
                FipsDirectorateId = r.FipsDirectorateId
            }).ToList(),
            ExplicitServices = explicitServices.Select(e => new ServiceDataModelExplicitServiceViewModel
            {
                Id = e.Id,
                CMDBProductId = e.CMDBProductId,
                ProductTitle = e.Product?.Title,
                Mode = e.Mode
            }).ToList(),
            Versions = versions.Select(v => new ServiceDataModelVersionSummaryViewModel
            {
                Id = v.Id,
                VersionNumber = v.VersionNumber,
                Status = v.Status,
                PeriodLabel = v.PeriodLabel,
                PublishedUtc = v.PublishedUtc,
                ChangeSummary = v.ChangeSummary
            }).ToList(),
            CanManage = canManage
        };
    }

    public async Task<bool> UpdateModelSettingsAsync(
        Guid modelId,
        ServiceDataModelSettingsInput input,
        string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var model = await _db.ServiceDataModels.FirstOrDefaultAsync(m => m.Id == modelId);
        if (model == null || model.LifecycleStatus == ServiceDataModelLifecycleStatus.Retired)
            return false;

        model.Name = input.Name.Trim();
        model.Description = NullIfWhiteSpace(input.Description);
        model.OwnerDisplayName = NullIfWhiteSpace(input.OwnerDisplayName);
        model.OwnerEmail = NullIfWhiteSpace(input.OwnerEmail);
        model.Classification = NullIfWhiteSpace(input.Classification);
        model.IsRepeatable = input.IsRepeatable;
        model.IsReportable = input.IsReportable;
        model.RequiresReviewerAttestation = input.RequiresReviewerAttestation;
        model.ProgressLabelThresholdPercent = input.ProgressLabelThresholdPercent;
        model.DefaultDueDaysAfterPublish = input.DefaultDueDaysAfterPublish;
        model.ReviewCadenceLabel = NullIfWhiteSpace(input.ReviewCadenceLabel);
        model.UpdatedUtc = DateTime.UtcNow;
        model.UpdatedByEmail = actorEmail.Trim();

        await WriteSdmAuditAsync("ServiceDataModel", model.Id.ToString(), "UpdateSettings", actorEmail,
            JsonSerializer.Serialize(new { model.Name }));
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Guid?> EnsureDraftVersionAsync(Guid modelId, string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        var model = await _db.ServiceDataModels.FirstOrDefaultAsync(m => m.Id == modelId);
        if (model == null || model.LifecycleStatus == ServiceDataModelLifecycleStatus.Retired)
            return null;

        var existingDraft = await _db.ServiceDataModelVersions
            .FirstOrDefaultAsync(v =>
                v.ServiceDataModelId == modelId &&
                v.Status == ServiceDataModelLifecycleStatus.Draft);
        if (existingDraft != null)
            return existingDraft.Id;

        var latest = await _db.ServiceDataModelVersions
            .Include(v => v.Groups)
            .ThenInclude(g => g.Fields)
            .ThenInclude(f => f.Options)
            .Where(v => v.ServiceDataModelId == modelId)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();

        if (latest == null)
        {
            var first = new ServiceDataModelVersion
            {
                ServiceDataModelId = modelId,
                VersionNumber = 1,
                Status = ServiceDataModelLifecycleStatus.Draft,
                CreatedUtc = DateTime.UtcNow,
                CreatedByEmail = actorEmail.Trim()
            };
            _db.ServiceDataModelVersions.Add(first);
            model.UpdatedUtc = DateTime.UtcNow;
            model.UpdatedByEmail = actorEmail.Trim();
            await _db.SaveChangesAsync();
            return first.Id;
        }

        if (latest.Status != ServiceDataModelLifecycleStatus.Published &&
            latest.Status != ServiceDataModelLifecycleStatus.Retired)
            return latest.Id;

        var draft = new ServiceDataModelVersion
        {
            ServiceDataModelId = modelId,
            VersionNumber = latest.VersionNumber + 1,
            Status = ServiceDataModelLifecycleStatus.Draft,
            CreatedUtc = DateTime.UtcNow,
            CreatedByEmail = actorEmail.Trim()
        };
        _db.ServiceDataModelVersions.Add(draft);

        foreach (var group in latest.Groups.OrderBy(g => g.SortOrder))
        {
            var newGroup = new ServiceDataModelGroup
            {
                ServiceDataModelVersionId = draft.Id,
                StableKey = group.StableKey,
                Name = group.Name,
                Guidance = group.Guidance,
                SortOrder = group.SortOrder,
                IsDisabled = group.IsDisabled
            };
            _db.ServiceDataModelGroups.Add(newGroup);

            foreach (var field in group.Fields.OrderBy(f => f.SortOrder))
            {
                var newField = new ServiceDataModelField
                {
                    ServiceDataModelGroupId = newGroup.Id,
                    StableKey = field.StableKey,
                    Label = field.Label,
                    Guidance = field.Guidance,
                    FieldType = field.FieldType,
                    IsMandatory = field.IsMandatory,
                    CountsTowardsCompletion = field.CountsTowardsCompletion,
                    IsReportable = field.IsReportable,
                    SortOrder = field.SortOrder,
                    IsDisabled = field.IsDisabled,
                    VisibilityRuleJson = field.VisibilityRuleJson,
                    CanonicalAttributeKey = field.CanonicalAttributeKey,
                    ValidationPattern = field.ValidationPattern,
                    MinNumber = field.MinNumber,
                    MaxNumber = field.MaxNumber
                };
                _db.ServiceDataModelFields.Add(newField);

                foreach (var opt in field.Options.OrderBy(o => o.SortOrder))
                {
                    _db.ServiceDataModelFieldOptions.Add(new ServiceDataModelFieldOption
                    {
                        ServiceDataModelFieldId = newField.Id,
                        ValueKey = opt.ValueKey,
                        Label = opt.Label,
                        SortOrder = opt.SortOrder
                    });
                }
            }
        }

        model.UpdatedUtc = DateTime.UtcNow;
        model.UpdatedByEmail = actorEmail.Trim();
        await WriteSdmAuditAsync("ServiceDataModelVersion", draft.Id.ToString(), "EnsureDraft", actorEmail,
            JsonSerializer.Serialize(new { modelId, draft.VersionNumber, copiedFrom = latest.VersionNumber }));
        await _db.SaveChangesAsync();
        return draft.Id;
    }

    public async Task<Guid?> AddGroupAsync(Guid modelId, ServiceDataModelGroupInput input, string actorEmail)
    {
        var versionId = await RequireEditableStructureVersionAsync(modelId, actorEmail);
        if (versionId == null)
            return null;

        var name = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
            return null;

        var existingKeys = await _db.ServiceDataModelGroups
            .Where(g => g.ServiceDataModelVersionId == versionId.Value)
            .Select(g => g.StableKey)
            .ToListAsync();
        var key = CensusStableKeyHelper.EnsureUnique(
            CensusStableKeyHelper.FromName(name, fallback: "theme"),
            existingKeys);

        var maxSort = await _db.ServiceDataModelGroups
            .Where(g => g.ServiceDataModelVersionId == versionId.Value)
            .Select(g => (int?)g.SortOrder)
            .MaxAsync() ?? 0;

        var group = new ServiceDataModelGroup
        {
            ServiceDataModelVersionId = versionId.Value,
            StableKey = key,
            Name = name,
            Guidance = NullIfWhiteSpace(input.Guidance),
            SortOrder = input.SortOrder > 0 ? input.SortOrder : maxSort + 1
        };
        _db.ServiceDataModelGroups.Add(group);
        await TouchModelAsync(modelId, actorEmail);
        await WriteSdmAuditAsync("ServiceDataModelGroup", group.Id.ToString(), "Create", actorEmail,
            JsonSerializer.Serialize(new { modelId, group.StableKey, group.Name, versionId = versionId.Value }));
        await _db.SaveChangesAsync();
        await RefreshPublishedSnapshotIfNeededAsync(versionId.Value);
        await RecalculateAssignmentsForVersionAsync(versionId.Value);
        await _db.SaveChangesAsync();
        return group.Id;
    }

    public async Task<bool> UpdateGroupAsync(Guid groupId, ServiceDataModelGroupInput input, string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var group = await _db.ServiceDataModelGroups
            .Include(g => g.Version)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null || !ServiceDataModelStructureVersion.AllowsEdits(group.Version.Status))
            return false;

        var name = input.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(name))
            return false;

        // Stable key is immutable — ignore any posted value so answers stay linked.
        group.Name = name;
        group.Guidance = NullIfWhiteSpace(input.Guidance);
        if (input.SortOrder > 0)
            group.SortOrder = input.SortOrder;
        await TouchModelAsync(group.Version.ServiceDataModelId, actorEmail);
        await WriteSdmAuditAsync("ServiceDataModelGroup", group.Id.ToString(), "Update", actorEmail,
            JsonSerializer.Serialize(new { group.StableKey, group.Name, group.SortOrder }));
        await _db.SaveChangesAsync();
        await RefreshPublishedSnapshotIfNeededAsync(group.ServiceDataModelVersionId);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetGroupDisabledAsync(Guid groupId, bool isDisabled, string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var group = await _db.ServiceDataModelGroups
            .Include(g => g.Version)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null || !ServiceDataModelStructureVersion.AllowsEdits(group.Version.Status))
            return false;

        group.IsDisabled = isDisabled;
        await TouchModelAsync(group.Version.ServiceDataModelId, actorEmail);
        await WriteSdmAuditAsync("ServiceDataModelGroup", group.Id.ToString(),
            isDisabled ? "Disable" : "Enable", actorEmail,
            JsonSerializer.Serialize(new { group.StableKey, group.Name }));
        await _db.SaveChangesAsync();
        await RefreshPublishedSnapshotIfNeededAsync(group.ServiceDataModelVersionId);
        await RecalculateAssignmentsForVersionAsync(group.ServiceDataModelVersionId);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderGroupsAsync(Guid modelId, IReadOnlyList<Guid> orderedGroupIds, string actorEmail)
    {
        var versionId = await RequireEditableStructureVersionAsync(modelId, actorEmail);
        if (versionId == null || orderedGroupIds.Count == 0)
            return false;

        var groups = await _db.ServiceDataModelGroups
            .Where(g => g.ServiceDataModelVersionId == versionId.Value)
            .ToListAsync();
        if (groups.Count == 0)
            return false;

        var byId = groups.ToDictionary(g => g.Id);
        var order = 1;
        foreach (var id in orderedGroupIds)
        {
            if (!byId.TryGetValue(id, out var group))
                continue;
            group.SortOrder = order++;
        }

        // Keep any omitted groups after the reordered set
        foreach (var group in groups.Where(g => !orderedGroupIds.Contains(g.Id)).OrderBy(g => g.SortOrder))
            group.SortOrder = order++;

        await TouchModelAsync(modelId, actorEmail);
        await WriteSdmAuditAsync("ServiceDataModelVersion", versionId.Value.ToString(), "ReorderGroups", actorEmail,
            JsonSerializer.Serialize(new { modelId, orderedGroupIds }));
        await _db.SaveChangesAsync();
        await RefreshPublishedSnapshotIfNeededAsync(versionId.Value);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Guid?> AddFieldAsync(Guid groupId, ServiceDataModelFieldInput input, string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        var group = await _db.ServiceDataModelGroups
            .Include(g => g.Version)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null || !ServiceDataModelStructureVersion.AllowsEdits(group.Version.Status))
            return null;

        var label = input.Label?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(label))
            return null;

        var existingKeys = await _db.ServiceDataModelFields
            .Where(f => f.ServiceDataModelGroupId == groupId)
            .Select(f => f.StableKey)
            .ToListAsync();
        input.StableKey = CensusStableKeyHelper.EnsureUnique(
            CensusStableKeyHelper.FromName(label, fallback: "question"),
            existingKeys);
        input.Label = label;

        if (input.SortOrder <= 0)
        {
            var maxSort = await _db.ServiceDataModelFields
                .Where(f => f.ServiceDataModelGroupId == groupId)
                .Select(f => (int?)f.SortOrder)
                .MaxAsync() ?? 0;
            input.SortOrder = maxSort + 1;
        }

        var field = MapFieldFromInput(input, group.Id);
        _db.ServiceDataModelFields.Add(field);
        await TouchModelAsync(group.Version.ServiceDataModelId, actorEmail);
        await WriteSdmAuditAsync("ServiceDataModelField", field.Id.ToString(), "Create", actorEmail,
            JsonSerializer.Serialize(new { groupId, field.StableKey, field.Label, versionId = group.ServiceDataModelVersionId }));
        await _db.SaveChangesAsync();
        await RefreshPublishedSnapshotIfNeededAsync(group.ServiceDataModelVersionId);
        await RecalculateAssignmentsForVersionAsync(group.ServiceDataModelVersionId);
        await _db.SaveChangesAsync();
        return field.Id;
    }

    public async Task<bool> UpdateFieldAsync(Guid fieldId, ServiceDataModelFieldInput input, string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var field = await _db.ServiceDataModelFields
            .Include(f => f.Group)
            .ThenInclude(g => g.Version)
            .FirstOrDefaultAsync(f => f.Id == fieldId);
        if (field == null || !ServiceDataModelStructureVersion.AllowsEdits(field.Group.Version.Status))
            return false;

        var label = input.Label?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(label))
            return false;

        // Stable key is immutable — ignore any posted value so answers stay linked.
        field.Label = label;
        field.Guidance = NullIfWhiteSpace(input.Guidance);
        field.FieldType = input.FieldType;
        field.IsMandatory = input.IsMandatory;
        field.CountsTowardsCompletion = input.CountsTowardsCompletion;
        field.IsReportable = input.IsReportable;
        if (input.SortOrder > 0)
            field.SortOrder = input.SortOrder;
        field.VisibilityRuleJson = NullIfWhiteSpace(input.VisibilityRuleJson);
        field.CanonicalAttributeKey = NullIfWhiteSpace(input.CanonicalAttributeKey);
        field.ValidationPattern = NullIfWhiteSpace(input.ValidationPattern);
        field.MinNumber = input.MinNumber;
        field.MaxNumber = input.MaxNumber;

        await TouchModelAsync(field.Group.Version.ServiceDataModelId, actorEmail);
        await WriteSdmAuditAsync("ServiceDataModelField", field.Id.ToString(), "Update", actorEmail,
            JsonSerializer.Serialize(new { field.StableKey, field.Label }));
        await _db.SaveChangesAsync();
        await RefreshPublishedSnapshotIfNeededAsync(field.Group.ServiceDataModelVersionId);
        await RecalculateAssignmentsForVersionAsync(field.Group.ServiceDataModelVersionId);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> SetFieldDisabledAsync(Guid fieldId, bool isDisabled, string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var field = await _db.ServiceDataModelFields
            .Include(f => f.Group)
            .ThenInclude(g => g.Version)
            .FirstOrDefaultAsync(f => f.Id == fieldId);
        if (field == null || !ServiceDataModelStructureVersion.AllowsEdits(field.Group.Version.Status))
            return false;

        field.IsDisabled = isDisabled;
        await TouchModelAsync(field.Group.Version.ServiceDataModelId, actorEmail);
        await WriteSdmAuditAsync("ServiceDataModelField", field.Id.ToString(),
            isDisabled ? "Disable" : "Enable", actorEmail,
            JsonSerializer.Serialize(new { field.StableKey, field.Label }));
        await _db.SaveChangesAsync();
        await RefreshPublishedSnapshotIfNeededAsync(field.Group.ServiceDataModelVersionId);
        await RecalculateAssignmentsForVersionAsync(field.Group.ServiceDataModelVersionId);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ReorderFieldsAsync(Guid groupId, IReadOnlyList<Guid> orderedFieldIds, string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var group = await _db.ServiceDataModelGroups
            .Include(g => g.Version)
            .FirstOrDefaultAsync(g => g.Id == groupId);
        if (group == null || !ServiceDataModelStructureVersion.AllowsEdits(group.Version.Status))
            return false;

        var fields = await _db.ServiceDataModelFields
            .Where(f => f.ServiceDataModelGroupId == groupId)
            .ToListAsync();
        if (fields.Count == 0)
            return false;

        var byId = fields.ToDictionary(f => f.Id);
        var order = 1;
        foreach (var id in orderedFieldIds)
        {
            if (!byId.TryGetValue(id, out var field))
                continue;
            field.SortOrder = order++;
        }

        foreach (var field in fields.Where(f => !orderedFieldIds.Contains(f.Id)).OrderBy(f => f.SortOrder))
            field.SortOrder = order++;

        await TouchModelAsync(group.Version.ServiceDataModelId, actorEmail);
        await WriteSdmAuditAsync("ServiceDataModelGroup", groupId.ToString(), "ReorderFields", actorEmail,
            JsonSerializer.Serialize(new { orderedFieldIds }));
        await _db.SaveChangesAsync();
        await RefreshPublishedSnapshotIfNeededAsync(group.ServiceDataModelVersionId);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Guid?> AddFieldOptionAsync(
        Guid fieldId,
        ServiceDataModelFieldOptionInput input,
        string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        var field = await _db.ServiceDataModelFields
            .Include(f => f.Group)
            .ThenInclude(g => g.Version)
            .FirstOrDefaultAsync(f => f.Id == fieldId);
        if (field == null || !ServiceDataModelStructureVersion.AllowsEdits(field.Group.Version.Status))
            return null;

        var maxSort = await _db.ServiceDataModelFieldOptions
            .Where(o => o.ServiceDataModelFieldId == fieldId)
            .Select(o => (int?)o.SortOrder)
            .MaxAsync() ?? 0;

        var option = new ServiceDataModelFieldOption
        {
            ServiceDataModelFieldId = fieldId,
            ValueKey = input.ValueKey.Trim(),
            Label = input.Label.Trim(),
            SortOrder = input.SortOrder > 0 ? input.SortOrder : maxSort + 1
        };
        _db.ServiceDataModelFieldOptions.Add(option);
        await TouchModelAsync(field.Group.Version.ServiceDataModelId, actorEmail);
        await WriteSdmAuditAsync("ServiceDataModelFieldOption", option.Id.ToString(), "Create", actorEmail,
            JsonSerializer.Serialize(new { fieldId, option.ValueKey, option.Label }));
        await _db.SaveChangesAsync();
        await RefreshPublishedSnapshotIfNeededAsync(field.Group.ServiceDataModelVersionId);
        await _db.SaveChangesAsync();
        return option.Id;
    }

    public async Task<bool> SetApplicabilityAsync(
        Guid modelId,
        ServiceDataModelApplicabilityInput input,
        string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var model = await _db.ServiceDataModels.FirstOrDefaultAsync(m => m.Id == modelId);
        if (model == null || model.LifecycleStatus == ServiceDataModelLifecycleStatus.Retired)
            return false;

        model.ApplicabilityMode = input.Mode;
        model.UpdatedUtc = DateTime.UtcNow;
        model.UpdatedByEmail = actorEmail.Trim();

        var existingRules = await _db.ServiceDataModelApplicabilityRules
            .Where(r => r.ServiceDataModelId == modelId)
            .ToListAsync();
        _db.ServiceDataModelApplicabilityRules.RemoveRange(existingRules);

        var existingExplicit = await _db.ServiceDataModelExplicitServices
            .Where(e => e.ServiceDataModelId == modelId)
            .ToListAsync();
        _db.ServiceDataModelExplicitServices.RemoveRange(existingExplicit);

        foreach (var rule in input.Rules ?? Array.Empty<ServiceDataModelApplicabilityRuleInput>())
        {
            _db.ServiceDataModelApplicabilityRules.Add(new ServiceDataModelApplicabilityRule
            {
                ServiceDataModelId = modelId,
                ProductStatus = rule.ProductStatus,
                PhaseId = rule.PhaseId,
                FipsTypeId = rule.FipsTypeId,
                FipsBusinessAreaId = rule.FipsBusinessAreaId,
                FipsDirectorateId = rule.FipsDirectorateId
            });
        }

        foreach (var id in (input.ExplicitIncludeProductIds ?? Array.Empty<Guid>()).Distinct())
        {
            _db.ServiceDataModelExplicitServices.Add(new ServiceDataModelExplicitService
            {
                ServiceDataModelId = modelId,
                CMDBProductId = id,
                Mode = ServiceDataModelExplicitServiceMode.Include
            });
        }

        foreach (var id in (input.ExplicitExcludeProductIds ?? Array.Empty<Guid>()).Distinct())
        {
            _db.ServiceDataModelExplicitServices.Add(new ServiceDataModelExplicitService
            {
                ServiceDataModelId = modelId,
                CMDBProductId = id,
                Mode = ServiceDataModelExplicitServiceMode.Exclude
            });
        }

        await WriteSdmAuditAsync("ServiceDataModel", modelId.ToString(), "SetApplicability", actorEmail,
            JsonSerializer.Serialize(new
            {
                mode = input.Mode.ToString(),
                ruleCount = input.Rules?.Count ?? 0,
                includes = input.ExplicitIncludeProductIds?.Count ?? 0,
                excludes = input.ExplicitExcludeProductIds?.Count ?? 0
            }));
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<ServiceDataModelApplicabilityPreviewViewModel?> PreviewApplicabilityAsync(Guid modelId)
    {
        var model = await _db.ServiceDataModels.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == modelId);
        if (model == null)
            return null;

        var productIds = await ResolveApplicableProductIdsAsync(modelId, model.ApplicabilityMode);
        var sampleTitles = await _db.CMDBProducts.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .OrderBy(p => p.Title)
            .Select(p => p.Title)
            .Take(20)
            .ToListAsync();

        return new ServiceDataModelApplicabilityPreviewViewModel
        {
            ModelId = modelId,
            Count = productIds.Count,
            SampleProductTitles = sampleTitles
        };
    }

    public async Task<bool> PublishVersionAsync(
        Guid modelId,
        ServiceDataModelPublishInput input,
        string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var model = await _db.ServiceDataModels.FirstOrDefaultAsync(m => m.Id == modelId);
        if (model == null || model.LifecycleStatus == ServiceDataModelLifecycleStatus.Retired)
            return false;

        var draft = await _db.ServiceDataModelVersions
            .Include(v => v.Groups)
            .ThenInclude(g => g.Fields)
            .ThenInclude(f => f.Options)
            .FirstOrDefaultAsync(v =>
                v.ServiceDataModelId == modelId &&
                v.Status == ServiceDataModelLifecycleStatus.Draft);
        if (draft == null)
            return false;

        var now = DateTime.UtcNow;
        var dueDays = input.DueDays ?? model.DefaultDueDaysAfterPublish;
        // Missing due days means no deadline (not overdue), including for standing models such as Service Census.
        var dueUtc = dueDays.HasValue ? now.AddDays(dueDays.Value) : (DateTime?)null;

        // Retire any previously published version of this model (history retained).
        var priorPublished = await _db.ServiceDataModelVersions
            .Where(v =>
                v.ServiceDataModelId == modelId &&
                v.Status == ServiceDataModelLifecycleStatus.Published)
            .ToListAsync();
        foreach (var prior in priorPublished)
        {
            prior.Status = ServiceDataModelLifecycleStatus.Retired;
            prior.RetiredUtc = now;
            prior.RetiredByEmail = actorEmail.Trim();
        }

        draft.Status = ServiceDataModelLifecycleStatus.Published;
        draft.ChangeSummary = NullIfWhiteSpace(input.ChangeSummary);
        // Period labels remain available for repeatable/periodic models; standing models typically leave them unset.
        draft.PeriodLabel = NullIfWhiteSpace(input.PeriodLabel);
        draft.PeriodStartUtc = input.PeriodStartUtc;
        draft.PeriodEndUtc = input.PeriodEndUtc;
        draft.PublishedUtc = now;
        draft.PublishedByEmail = actorEmail.Trim();
        draft.DefinitionSnapshotJson = BuildDefinitionSnapshot(draft);

        model.LifecycleStatus = ServiceDataModelLifecycleStatus.Published;
        model.UpdatedUtc = now;
        model.UpdatedByEmail = actorEmail.Trim();

        var applicableIds = await ResolveApplicableProductIdsAsync(modelId, model.ApplicabilityMode);
        var existingSet = new HashSet<Guid>();

        // Standing (non-repeatable) models: keep the current open assignment on the new definition
        // so re-publish does not wipe living answers or create a fresh period assignment.
        if (!model.IsRepeatable && priorPublished.Count > 0)
        {
            var migrated = await MigrateStandingAssignmentsToPublishedVersionAsync(
                priorPublished.Select(v => v.Id).ToList(),
                draft,
                applicableIds,
                now);
            existingSet.UnionWith(migrated);
        }

        var alreadyOnDraft = await _db.ServiceDataModelAssignments
            .Where(a => a.ServiceDataModelVersionId == draft.Id)
            .Select(a => a.CMDBProductId)
            .ToListAsync();
        existingSet.UnionWith(alreadyOnDraft);

        foreach (var productId in applicableIds)
        {
            if (existingSet.Contains(productId))
                continue;

            _db.ServiceDataModelAssignments.Add(new ServiceDataModelAssignment
            {
                ServiceDataModelVersionId = draft.Id,
                CMDBProductId = productId,
                PeriodLabel = model.IsRepeatable ? draft.PeriodLabel : null,
                PeriodStartUtc = model.IsRepeatable ? draft.PeriodStartUtc : null,
                PeriodEndUtc = model.IsRepeatable ? draft.PeriodEndUtc : null,
                DueUtc = dueUtc,
                Status = ServiceDataModelAssignmentStatus.NotStarted,
                CreatedUtc = now,
                UpdatedUtc = now
            });
        }

        await WriteSdmAuditAsync("ServiceDataModelVersion", draft.Id.ToString(), "Publish", actorEmail,
            JsonSerializer.Serialize(new
            {
                modelId,
                draft.VersionNumber,
                draft.PeriodLabel,
                isRepeatable = model.IsRepeatable,
                assignmentCount = applicableIds.Count,
                migratedCount = existingSet.Count,
                dueDays
            }));
        await _auditLogger.LogAsync("ServiceDataModelVersion", draft.Id.ToString(), "publish", actorEmail,
            JsonSerializer.Serialize(new { modelId, draft.VersionNumber, draft.PeriodLabel }));
        await _db.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Moves open standing assignments from retired published versions onto the newly published
    /// definition, remapping answers by field stable key so living answers are retained.
    /// </summary>
    private async Task<HashSet<Guid>> MigrateStandingAssignmentsToPublishedVersionAsync(
        IReadOnlyList<Guid> priorVersionIds,
        ServiceDataModelVersion draft,
        IReadOnlyList<Guid> applicableProductIds,
        DateTime now)
    {
        var migratedProducts = new HashSet<Guid>();
        if (priorVersionIds.Count == 0)
            return migratedProducts;

        var applicableSet = applicableProductIds.ToHashSet();
        var openStatuses = new[]
        {
            ServiceDataModelAssignmentStatus.NotStarted,
            ServiceDataModelAssignmentStatus.InProgress,
            ServiceDataModelAssignmentStatus.Submitted,
            ServiceDataModelAssignmentStatus.ChangesRequested,
            ServiceDataModelAssignmentStatus.Reviewed,
            ServiceDataModelAssignmentStatus.ReviewDue
        };

        var assignments = await _db.ServiceDataModelAssignments
            .Include(a => a.Submissions)
            .ThenInclude(s => s.Answers)
            .Where(a =>
                priorVersionIds.Contains(a.ServiceDataModelVersionId) &&
                applicableSet.Contains(a.CMDBProductId) &&
                openStatuses.Contains(a.Status))
            .ToListAsync();

        if (assignments.Count == 0)
            return migratedProducts;

        var priorFields = await _db.ServiceDataModelFields
            .AsNoTracking()
            .Include(f => f.Group)
            .Where(f => priorVersionIds.Contains(f.Group.ServiceDataModelVersionId))
            .Select(f => new { f.Id, f.StableKey, f.Group.ServiceDataModelVersionId })
            .ToListAsync();

        // One standing assignment per product: keep the most recently updated when duplicates exist.
        foreach (var keep in assignments
                     .GroupBy(a => a.CMDBProductId)
                     .Select(g => g.OrderByDescending(a => a.UpdatedUtc).First()))
        {
            var oldFieldIdToKey = priorFields
                .Where(f => f.ServiceDataModelVersionId == keep.ServiceDataModelVersionId)
                .ToDictionary(f => f.Id, f => f.StableKey);

            foreach (var submission in keep.Submissions)
            {
                foreach (var answer in submission.Answers.ToList())
                {
                    var key = SharedCensusAnswerKeys.ResolveStableKey(answer)
                              ?? (answer.ServiceDataModelFieldId.HasValue &&
                                  oldFieldIdToKey.TryGetValue(answer.ServiceDataModelFieldId.Value, out var k)
                                  ? k
                                  : null);
                    if (string.IsNullOrWhiteSpace(key))
                    {
                        _db.ServiceDataModelAnswers.Remove(answer);
                        continue;
                    }

                    // Shared base set: keep answers by stable key; drop copied field FKs.
                    answer.FieldStableKey = key;
                    answer.ServiceDataModelFieldId = null;
                }
            }

            var proposed = await _db.ServiceDataModelProposedRegisterChanges
                .Where(p => p.ServiceDataModelAssignmentId == keep.Id)
                .ToListAsync();
            foreach (var change in proposed)
            {
                var key = !string.IsNullOrWhiteSpace(change.FieldStableKey)
                    ? change.FieldStableKey
                    : (change.ServiceDataModelFieldId.HasValue &&
                       oldFieldIdToKey.TryGetValue(change.ServiceDataModelFieldId.Value, out var k)
                        ? k
                        : null);
                if (string.IsNullOrWhiteSpace(key))
                    continue;

                change.FieldStableKey = key;
                change.ServiceDataModelFieldId = null;
            }

            keep.ServiceDataModelVersionId = draft.Id;
            keep.PeriodLabel = null;
            keep.PeriodStartUtc = null;
            keep.PeriodEndUtc = null;
            keep.UpdatedUtc = now;
            migratedProducts.Add(keep.CMDBProductId);
        }

        return migratedProducts;
    }

    public async Task<bool> RetireModelAsync(Guid modelId, string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var model = await _db.ServiceDataModels.FirstOrDefaultAsync(m => m.Id == modelId);
        if (model == null)
            return false;

        var now = DateTime.UtcNow;
        model.LifecycleStatus = ServiceDataModelLifecycleStatus.Retired;
        model.UpdatedUtc = now;
        model.UpdatedByEmail = actorEmail.Trim();

        var openVersions = await _db.ServiceDataModelVersions
            .Where(v =>
                v.ServiceDataModelId == modelId &&
                v.Status != ServiceDataModelLifecycleStatus.Retired)
            .ToListAsync();
        foreach (var version in openVersions)
        {
            version.Status = ServiceDataModelLifecycleStatus.Retired;
            version.RetiredUtc = now;
            version.RetiredByEmail = actorEmail.Trim();
        }

        await WriteSdmAuditAsync("ServiceDataModel", modelId.ToString(), "Retire", actorEmail, null);
        await _auditLogger.LogAsync("ServiceDataModel", modelId.ToString(), "retire", actorEmail, null);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<Guid?> SeedDefaultServiceCensusDraftAsync(string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        var key = ServiceCensusDefaults.StableKey;
        var existing = await _db.ServiceDataModels.AsNoTracking()
            .FirstOrDefaultAsync(m => m.StableKey == key);
        if (existing != null)
        {
            await EnsureSeededCensusCanonicalKeysAsync(existing.Id);
            return existing.Id;
        }

        var modelId = await CreateModelAsync(
            key,
            ServiceCensusDefaults.Name,
            ServiceCensusDefaults.Description,
            ServiceCensusDefaults.OwnerDisplayName,
            null,
            ServiceCensusDefaults.Classification,
            isRepeatable: ServiceCensusDefaults.IsRepeatable,
            actorEmail);

        var model = await _db.ServiceDataModels.FirstAsync(m => m.Id == modelId);
        model.RequiresReviewerAttestation = ServiceCensusDefaults.RequiresReviewerAttestation;
        model.DefaultDueDaysAfterPublish = ServiceCensusDefaults.DefaultDueDaysAfterPublish;
        model.IsReportable = ServiceCensusDefaults.IsReportable;
        model.ApplicabilityMode = ServiceDataModelApplicabilityMode.AllActive;
        model.ReviewCadenceLabel = ServiceCensusDefaults.ReviewCadenceLabel;

        var draftId = await EnsureDraftVersionAsync(modelId, actorEmail);
        if (draftId == null)
            return modelId;

        // Themes/questions come from the shared default catalogue — do not seed per-model copies.
        await WriteSdmAuditAsync("ServiceDataModel", modelId.ToString(), "SeedDefault", actorEmail,
            JsonSerializer.Serialize(new { key, sharedThemes = true }));
        await _db.SaveChangesAsync();
        return modelId;
    }

    /// <summary>
    /// Backfills canonical attribute keys on the standing census seed fields when an older draft
    /// was created before description mapping existed.
    /// </summary>
    private async Task EnsureSeededCensusCanonicalKeysAsync(Guid modelId)
    {
        var draft = await _db.ServiceDataModelVersions
            .Include(v => v.Groups)
            .ThenInclude(g => g.Fields)
            .Where(v =>
                v.ServiceDataModelId == modelId &&
                v.Status == ServiceDataModelLifecycleStatus.Draft)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();

        // Also patch the current published version so live assignments pick up description mapping.
        var published = await _db.ServiceDataModelVersions
            .Include(v => v.Groups)
            .ThenInclude(g => g.Fields)
            .Where(v =>
                v.ServiceDataModelId == modelId &&
                v.Status == ServiceDataModelLifecycleStatus.Published)
            .OrderByDescending(v => v.VersionNumber)
            .FirstOrDefaultAsync();

        var changed = false;
        foreach (var version in new[] { draft, published }.Where(v => v != null))
        {
            foreach (var field in version!.Groups.SelectMany(g => g.Fields))
            {
                if (string.Equals(field.StableKey, "service-summary", StringComparison.OrdinalIgnoreCase) &&
                    string.IsNullOrWhiteSpace(field.CanonicalAttributeKey))
                {
                    field.CanonicalAttributeKey = "description";
                    changed = true;
                }

                if (string.Equals(field.StableKey, "confirm-title", StringComparison.OrdinalIgnoreCase) &&
                    ServiceDataModelCanonicalPrefill.IsTitleCanonicalKey(field.CanonicalAttributeKey ?? "title"))
                {
                    if (!string.Equals(field.Label, "Service name", StringComparison.Ordinal))
                    {
                        field.Label = "Service name";
                        changed = true;
                    }

                    const string titleGuidance = "Taken from the Service Register and cannot be changed here.";
                    if (!string.Equals(field.Guidance, titleGuidance, StringComparison.Ordinal))
                    {
                        field.Guidance = titleGuidance;
                        changed = true;
                    }

                    if (string.IsNullOrWhiteSpace(field.CanonicalAttributeKey))
                    {
                        field.CanonicalAttributeKey = "title";
                        changed = true;
                    }
                }
            }
        }

        if (changed)
            await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Resolves applicable CMDB product IDs for the model's current applicability configuration.
    /// AllActive: Active and not explicitly excluded.
    /// ByAttributes: OR of matching rules on Active products, plus Includes, minus Excludes.
    /// ExplicitOnly: only Includes.
    /// </summary>
    private async Task<IReadOnlyList<Guid>> ResolveApplicableProductIdsAsync(
        Guid modelId,
        ServiceDataModelApplicabilityMode mode)
    {
        var explicitRows = await _db.ServiceDataModelExplicitServices.AsNoTracking()
            .Where(e => e.ServiceDataModelId == modelId)
            .ToListAsync();
        var includes = explicitRows
            .Where(e => e.Mode == ServiceDataModelExplicitServiceMode.Include)
            .Select(e => e.CMDBProductId)
            .ToHashSet();
        var excludes = explicitRows
            .Where(e => e.Mode == ServiceDataModelExplicitServiceMode.Exclude)
            .Select(e => e.CMDBProductId)
            .ToHashSet();

        if (mode == ServiceDataModelApplicabilityMode.ExplicitOnly)
            return includes.ToList();

        if (mode == ServiceDataModelApplicabilityMode.AllActive)
        {
            var activeIds = await _db.CMDBProducts.AsNoTracking()
                .Where(p => p.Status == CMDBProductStatus.Active)
                .Select(p => p.Id)
                .ToListAsync();
            return activeIds.Where(id => !excludes.Contains(id)).ToList();
        }

        // ByAttributes
        var rules = await _db.ServiceDataModelApplicabilityRules.AsNoTracking()
            .Where(r => r.ServiceDataModelId == modelId)
            .ToListAsync();

        var activeProducts = await _db.CMDBProducts.AsNoTracking()
            .Include(p => p.Types)
            .Include(p => p.BusinessAreas)
            .Include(p => p.Directorates)
            .Where(p => p.Status == CMDBProductStatus.Active)
            .ToListAsync();

        var matched = new HashSet<Guid>();
        foreach (var product in activeProducts)
        {
            if (rules.Count == 0)
                break;

            foreach (var rule in rules)
            {
                if (RuleMatches(product, rule))
                {
                    matched.Add(product.Id);
                    break;
                }
            }
        }

        matched.UnionWith(includes);
        matched.ExceptWith(excludes);
        return matched.ToList();
    }

    private static bool RuleMatches(CMDBProduct product, ServiceDataModelApplicabilityRule rule)
    {
        var hasCriterion = rule.ProductStatus.HasValue
            || rule.PhaseId.HasValue
            || rule.FipsTypeId.HasValue
            || rule.FipsBusinessAreaId.HasValue
            || rule.FipsDirectorateId.HasValue;

        // Empty rule matches all Active products (caller already filtered to Active).
        if (!hasCriterion)
            return true;

        if (rule.ProductStatus.HasValue && product.Status != rule.ProductStatus.Value)
            return false;
        if (rule.PhaseId.HasValue && product.PhaseId != rule.PhaseId.Value)
            return false;
        if (rule.FipsTypeId.HasValue &&
            product.Types.All(t => t.FipsTypeId != rule.FipsTypeId.Value))
            return false;
        if (rule.FipsBusinessAreaId.HasValue &&
            product.BusinessAreas.All(b => b.FipsBusinessAreaId != rule.FipsBusinessAreaId.Value))
            return false;
        if (rule.FipsDirectorateId.HasValue &&
            product.Directorates.All(d => d.FipsDirectorateId != rule.FipsDirectorateId.Value))
            return false;

        return true;
    }

    /// <summary>
    /// Returns the version that structural adds/edits should target: the published
    /// definition when live (so respondents see new themes/questions immediately),
    /// otherwise an ensured draft for unpublished models.
    /// </summary>
    private async Task<Guid?> RequireEditableStructureVersionAsync(Guid modelId, string actorEmail)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return null;

        var model = await _db.ServiceDataModels.FirstOrDefaultAsync(m => m.Id == modelId);
        if (model == null || model.LifecycleStatus == ServiceDataModelLifecycleStatus.Retired)
            return null;

        var published = await _db.ServiceDataModelVersions
            .FirstOrDefaultAsync(v =>
                v.ServiceDataModelId == modelId &&
                v.Status == ServiceDataModelLifecycleStatus.Published);
        if (published != null)
            return published.Id;

        return await EnsureDraftVersionAsync(modelId, actorEmail);
    }

    private async Task RefreshPublishedSnapshotIfNeededAsync(Guid versionId)
    {
        var version = await _db.ServiceDataModelVersions
            .Include(v => v.Groups)
            .ThenInclude(g => g.Fields)
            .ThenInclude(f => f.Options)
            .FirstOrDefaultAsync(v => v.Id == versionId);
        if (version == null || version.Status != ServiceDataModelLifecycleStatus.Published)
            return;

        version.DefinitionSnapshotJson = BuildDefinitionSnapshot(version);
    }

    /// <summary>
    /// Recalculates stored completion % for every assignment on a version after structural adds/disables.
    /// Existing answers are left untouched; new unanswered counting fields lower the percentage.
    /// </summary>
    private async Task RecalculateAssignmentsForVersionAsync(Guid versionId)
    {
        var assignments = await _db.ServiceDataModelAssignments
            .Include(a => a.Product)
            .Include(a => a.Version)
            .ThenInclude(v => v.Groups)
            .ThenInclude(g => g.Fields)
            .Include(a => a.Submissions)
            .ThenInclude(s => s.Answers)
            .Where(a => a.ServiceDataModelVersionId == versionId)
            .ToListAsync();

        foreach (var assignment in assignments)
        {
            if (assignment.Status is ServiceDataModelAssignmentStatus.Withdrawn
                or ServiceDataModelAssignmentStatus.NotApplicable)
                continue;

            var submission = assignment.Submissions.FirstOrDefault(s => s.IsCurrent);
            var fields = assignment.Version.Groups
                .Where(g => !g.IsDisabled)
                .SelectMany(g => g.Fields.Where(f => !f.IsDisabled))
                .ToList();

            IReadOnlySet<string> suppressed;
            var inputs = BuildCompletionInputs(fields, submission, assignment.Product, out suppressed);
            // Prefill suppression only affects form display; completion uses the same calculator path as census.
            _ = suppressed;

            var groupKeys = fields.Select(f =>
            {
                var group = assignment.Version.Groups.First(g => g.Fields.Any(x => x.Id == f.Id));
                return (group.StableKey, f.Id);
            }).ToList();

            var result = ServiceDataModelCompletionCalculator.Calculate(inputs, groupKeys);
            assignment.FieldCompletionPercent = result.FieldCompletionPercent;
            assignment.MandatoryCompletionPercent = result.MandatoryCompletionPercent;
            assignment.UpdatedUtc = DateTime.UtcNow;
        }
    }

    private static List<ServiceDataModelFieldAnswerInput> BuildCompletionInputs(
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

    private async Task TouchModelAsync(Guid modelId, string actorEmail)
    {
        var model = await _db.ServiceDataModels.FirstOrDefaultAsync(m => m.Id == modelId);
        if (model == null)
            return;
        model.UpdatedUtc = DateTime.UtcNow;
        model.UpdatedByEmail = actorEmail.Trim();
    }

    private Task WriteSdmAuditAsync(
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
            ActorEmail = NullIfWhiteSpace(actorEmail),
            OccurredUtc = DateTime.UtcNow,
            MetadataJson = Truncate(metadataJson, 4000)
        });
        return Task.CompletedTask;
    }

    private static ServiceDataModelGroupViewModel MapGroup(ServiceDataModelGroup group) =>
        new()
        {
            Id = group.Id,
            StableKey = group.StableKey,
            Name = group.Name,
            Guidance = group.Guidance,
            SortOrder = group.SortOrder,
            IsDisabled = group.IsDisabled,
            Fields = group.Fields
                .OrderBy(f => f.SortOrder)
                .ThenBy(f => f.Label)
                .Select(f => new ServiceDataModelFieldViewModel
                {
                    Id = f.Id,
                    StableKey = f.StableKey,
                    Label = f.Label,
                    Guidance = f.Guidance,
                    FieldType = f.FieldType,
                    IsMandatory = f.IsMandatory,
                    CountsTowardsCompletion = f.CountsTowardsCompletion,
                    IsReportable = f.IsReportable,
                    SortOrder = f.SortOrder,
                    IsDisabled = f.IsDisabled,
                    VisibilityRuleJson = f.VisibilityRuleJson,
                    CanonicalAttributeKey = f.CanonicalAttributeKey,
                    ValidationPattern = f.ValidationPattern,
                    MinNumber = f.MinNumber,
                    MaxNumber = f.MaxNumber,
                    Options = f.Options
                        .OrderBy(o => o.SortOrder)
                        .Select(o => new ServiceDataModelFieldOptionViewModel
                        {
                            Id = o.Id,
                            ValueKey = o.ValueKey,
                            Label = o.Label,
                            SortOrder = o.SortOrder
                        }).ToList()
                }).ToList()
        };

    private static ServiceDataModelField MapFieldFromInput(ServiceDataModelFieldInput input, Guid groupId) =>
        new()
        {
            ServiceDataModelGroupId = groupId,
            StableKey = input.StableKey.Trim(),
            Label = input.Label.Trim(),
            Guidance = NullIfWhiteSpace(input.Guidance),
            FieldType = input.FieldType,
            IsMandatory = input.IsMandatory,
            CountsTowardsCompletion = input.CountsTowardsCompletion,
            IsReportable = input.IsReportable,
            SortOrder = input.SortOrder,
            VisibilityRuleJson = NullIfWhiteSpace(input.VisibilityRuleJson),
            CanonicalAttributeKey = NullIfWhiteSpace(input.CanonicalAttributeKey),
            ValidationPattern = NullIfWhiteSpace(input.ValidationPattern),
            MinNumber = input.MinNumber,
            MaxNumber = input.MaxNumber
        };

    private static string BuildDefinitionSnapshot(ServiceDataModelVersion version)
    {
        var payload = new
        {
            version.VersionNumber,
            Groups = version.Groups.OrderBy(g => g.SortOrder).Select(g => new
            {
                g.StableKey,
                g.Name,
                g.SortOrder,
                g.IsDisabled,
                Fields = g.Fields.OrderBy(f => f.SortOrder).Select(f => new
                {
                    f.StableKey,
                    f.Label,
                    FieldType = f.FieldType.ToString(),
                    f.IsMandatory,
                    f.CountsTowardsCompletion,
                    f.IsDisabled,
                    f.CanonicalAttributeKey,
                    f.VisibilityRuleJson,
                    Options = f.Options.OrderBy(o => o.SortOrder).Select(o => new
                    {
                        o.ValueKey,
                        o.Label
                    })
                })
            })
        };
        return JsonSerializer.Serialize(payload);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value[..max];
    }
}
