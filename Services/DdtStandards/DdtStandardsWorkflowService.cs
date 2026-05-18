using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Compass.Data;
using Compass.Helpers;
using Compass.Models;
using Compass.ViewModels.Modern;
using Compass.Security;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.DdtStandards;

public class DdtStandardsWorkflowService : IDdtStandardsWorkflowService
{
    private static readonly string[] WorkflowSteps = ["Draft", "In review", "Approved", "Published"];

    private readonly CompassDbContext _context;
    private readonly IPermissionService _permissions;
    private readonly IUserDirectoryService _userDirectory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<DdtStandardsWorkflowService> _logger;

    public DdtStandardsWorkflowService(
        CompassDbContext context,
        IPermissionService permissions,
        IUserDirectoryService userDirectory,
        IHttpContextAccessor httpContextAccessor,
        ILogger<DdtStandardsWorkflowService> logger)
    {
        _context = context;
        _permissions = permissions;
        _userDirectory = userDirectory;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<DdtStandardEditViewModel> BuildEditViewModelAsync(int? standardId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var categories = await _context.StandardCategories.AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new DdtStandardLookupOption { Id = c.Id, Name = c.Name })
            .ToListAsync(ct);

        var phases = await _context.PhaseLookups.AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.SortOrder)
            .Select(p => new DdtStandardLookupOption { Id = p.Id, Name = p.Name })
            .ToListAsync(ct);

        var vm = new DdtStandardEditViewModel
        {
            Categories = categories,
            Phases = phases,
            CanEdit = true,
            CanSubmit = false,
            CanDelete = false,
            WorkflowStepIndex = 0
        };

        if (!standardId.HasValue)
            return vm;

        var trackedDraft = await _context.DdtStandards
            .FirstOrDefaultAsync(s => s.Id == standardId.Value && !s.IsDeleted, ct);
        if (trackedDraft != null)
            await CorrectDraftVersionFromPublishedParentAsync(trackedDraft, ct);

        var standard = await _context.DdtStandards.AsNoTracking()
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .Include(s => s.Categories)
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.Id == standardId.Value && !s.IsDeleted, ct);

        if (standard == null)
            return vm;

        var userId = GetCurrentUserId(user);
        var canEdit = standard.Stage == "Draft" && await CanEditStandardAsync(standard, userId, user, ct);
        var isCreator = userId.HasValue && standard.CreatorUserId == userId;

        vm.Id = standard.Id;
        vm.Title = standard.Title;
        vm.Summary = standard.Summary;
        vm.Purpose = standard.Purpose;
        vm.Criteria = standard.Criteria;
        vm.HowToMeet = standard.HowToMeet;
        vm.Governance = standard.Governance;
        vm.LegalBasis = standard.LegalBasis;
        vm.LegalStandard = standard.LegalStandard;
        vm.ValidityPeriod = standard.ValidityPeriod;
        vm.RelatedGuidance = standard.RelatedGuidance;
        vm.Stage = standard.Stage;
        vm.Version = standard.Version;
        if (!string.IsNullOrWhiteSpace(standard.PreviousVersion) && standard.FirstPublished.HasValue)
        {
            vm.PublishedVersion = standard.PreviousVersion;
        }
        else if (standard.ParentStandardId.HasValue)
        {
            var publishedVersion = standard.PreviousVersion;
            if (string.IsNullOrWhiteSpace(publishedVersion))
            {
                publishedVersion = await _context.DdtStandards.AsNoTracking()
                    .Where(s => s.Id == standard.ParentStandardId.Value)
                    .Select(s => s.Version)
                    .FirstOrDefaultAsync(ct);
            }

            if (!string.IsNullOrWhiteSpace(publishedVersion))
            {
                vm.PublishedVersion = publishedVersion;
                if (DdtStandardVersioning.IsNewStandardDraftVersion(standard.Version))
                    vm.Version = DdtStandardVersioning.ResolveDraftVersionFromParent(publishedVersion);
            }
        }
        vm.SelectedCategoryIds = standard.Categories.Select(c => c.CategoryId).ToList();
        vm.SelectedPhaseIds = standard.Phases.Select(p => p.PhaseLookupId).ToList();
        vm.OwnerObjectIds = string.Join(",", standard.Owners
            .Select(o => o.User?.AzureObjectId)
            .Where(s => !string.IsNullOrWhiteSpace(s))!);
        vm.ContactObjectIds = string.Join(",", standard.Contacts
            .Select(c => c.User?.AzureObjectId)
            .Where(s => !string.IsNullOrWhiteSpace(s))!);
        vm.CanEdit = canEdit;
        vm.CanSubmit = canEdit;
        vm.CanDelete = canEdit;
        vm.WorkflowStepIndex = GetWorkflowStepIndex(standard.Stage, standard.IsPublished);

        return vm;
    }

    public async Task<DdtStandardWorkflowContextViewModel> BuildWorkflowContextAsync(int standardId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var standard = await _context.DdtStandards.AsNoTracking()
            .Include(s => s.Owners)
            .FirstOrDefaultAsync(s => s.Id == standardId && !s.IsDeleted, ct);

        if (standard == null)
            return new DdtStandardWorkflowContextViewModel { StandardId = standardId };

        var userId = GetCurrentUserId(user);
        var canManage = await StandardsPermissionHelper.CanManageStandardsAsync(_permissions, user);
        var canApprove = await StandardsPermissionHelper.CanApproveStandardsAsync(_permissions, user);
        var canPublishRole = await StandardsPermissionHelper.CanPublishStandardsAsync(_permissions, user);
        var isOwner = userId.HasValue && standard.Owners.Any(o => o.UserId == userId);
        var isCreator = userId.HasValue && standard.CreatorUserId == userId;
        var canEditRole = await CanEditStandardAsync(standard, userId, user, ct);
        var isDraft = standard.Stage == "Draft";
        var isForApproval = standard.Stage == "For Approval";
        var isAwaiting = standard.Stage == "Awaiting Publication";

        var comments = await _context.DdtStandardComments.AsNoTracking()
            .Include(c => c.User)
            .Where(c => c.DdtStandardId == standardId && c.ParentCommentId == null)
            .OrderByDescending(c => c.CreatedAt)
            .Take(20)
            .ToListAsync(ct);

        return new DdtStandardWorkflowContextViewModel
        {
            StandardId = standardId,
            Stage = standard.Stage,
            WorkflowStepIndex = GetWorkflowStepIndex(standard.Stage, standard.IsPublished),
            CanEdit = isDraft && (canEditRole || isCreator),
            CanSubmit = isDraft && (canEditRole || isCreator),
            CanDelete = isDraft && (canEditRole || isCreator),
            CanApprove = isForApproval && canApprove,
            CanReject = isForApproval && canApprove,
            CanPublish = isAwaiting && (canPublishRole || isOwner),
            ForumComments = comments.Select(c => new DdtStandardForumCommentRow
            {
                Title = c.Title,
                Body = c.Comments,
                AuthorName = c.User?.Name ?? "Unknown",
                CreatedAt = c.CreatedAt,
                CommentType = c.CommentType ?? "comment"
            }).ToList()
        };
    }

    public async Task<DdtStandardDetailToolbarViewModel> BuildDetailToolbarAsync(int standardId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var standard = await _context.DdtStandards.AsNoTracking()
            .Include(s => s.Owners).ThenInclude(o => o.User)
            .Include(s => s.Contacts).ThenInclude(c => c.User)
            .FirstOrDefaultAsync(s => s.Id == standardId && !s.IsDeleted, ct);

        if (standard == null)
            return new DdtStandardDetailToolbarViewModel { StandardId = standardId };

        var userId = GetCurrentUserId(user);
        var canOpenEdit = await CanEditStandardAsync(standard, userId, user, ct);

        var ownerDisplay = standard.Owners.Select(o => o.User?.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
            ?? standard.Contacts.Select(c => c.User?.Name).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n))
            ?? "the standard owner";

        var lineageStandards = await _context.DdtStandards.AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => new DdtStandard { Id = s.Id, ParentStandardId = s.ParentStandardId, IsPublished = s.IsPublished, Stage = s.Stage })
            .ToListAsync(ct);
        var lineageIds = DdtStandardsListingHelper.GetLineageIds(lineageStandards, standardId);

        var versionRecordCount = await _context.DdtStandardVersions.AsNoTracking()
            .CountAsync(v => lineageIds.Contains(v.DdtStandardId), ct);
        var publishedInLineage = lineageStandards.Count(s =>
            lineageIds.Contains(s.Id) && s.IsPublished && s.Stage == "Published");

        int? draftEditId = null;
        if (canOpenEdit)
        {
            if (standard.Stage == "Draft")
                draftEditId = standard.Id;
            else if (standard.IsPublished && standard.Stage == "Published")
                draftEditId = DdtStandardsListingHelper.GetActiveDraftIdInLineage(lineageStandards, standardId);
        }

        return new DdtStandardDetailToolbarViewModel
        {
            StandardId = standardId,
            CanOpenEdit = canOpenEdit,
            DraftEditId = draftEditId,
            NeedsDraftFromPublished = canOpenEdit && standard.IsPublished && standard.Stage == "Published" && !draftEditId.HasValue,
            OwnerContactDisplay = ownerDisplay,
            HasVersionHistory = versionRecordCount > 0 || publishedInLineage > 1
        };
    }

    public async Task<DdtStandardHistoryViewModel?> BuildHistoryViewModelAsync(int standardId, CancellationToken ct = default)
    {
        var standard = await _context.DdtStandards.AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == standardId && !s.IsDeleted, ct);
        if (standard == null)
            return null;

        var lineageStandards = await _context.DdtStandards.AsNoTracking()
            .Where(s => !s.IsDeleted)
            .Select(s => new DdtStandard
            {
                Id = s.Id,
                ParentStandardId = s.ParentStandardId,
                Version = s.Version,
                PreviousVersion = s.PreviousVersion,
                IsPublished = s.IsPublished,
                Stage = s.Stage,
                PublishedAt = s.PublishedAt,
                UpdatedAt = s.UpdatedAt
            })
            .ToListAsync(ct);

        var lineageIds = DdtStandardsListingHelper.GetLineageIds(lineageStandards, standardId);

        var versionRows = await _context.DdtStandardVersions.AsNoTracking()
            .Include(v => v.PublishedByUser)
            .Where(v => lineageIds.Contains(v.DdtStandardId))
            .ToListAsync(ct);

        var entries = versionRows.Select(v => new DdtStandardHistoryRow
        {
            VersionNumber = v.VersionNumber,
            PreviousVersion = v.PreviousVersion,
            ChangedAt = v.PublishedAt ?? v.CreatedAt,
            ChangeSummary = v.ChangeSummary,
            ChangeDetails = v.ChangeDetails,
            PublishedByName = v.PublishedByUser?.Name,
            ViewStandardId = v.DdtStandardId,
            IsCurrent = v.DdtStandardId == standardId && v.VersionNumber == standard.Version
        }).ToList();

        foreach (var s in lineageStandards.Where(s => s.IsPublished && s.Stage == "Published"))
        {
            if (entries.Any(e => e.VersionNumber == s.Version && e.ViewStandardId == s.Id))
                continue;

            entries.Add(new DdtStandardHistoryRow
            {
                VersionNumber = s.Version,
                PreviousVersion = s.PreviousVersion,
                ChangedAt = s.PublishedAt ?? s.UpdatedAt,
                ChangeSummary = "Published",
                ViewStandardId = s.Id,
                IsCurrent = s.Id == standardId
            });
        }

        var latestPublishedId = DdtStandardsListingHelper.GetLatestPublishedIdInLineage(lineageStandards, standardId);
        foreach (var e in entries)
            e.IsCurrent = latestPublishedId.HasValue && e.ViewStandardId == latestPublishedId;

        return new DdtStandardHistoryViewModel
        {
            StandardId = standardId,
            Title = standard.Title,
            CurrentVersion = standard.Version,
            Entries = entries
                .OrderByDescending(e => e.ChangedAt ?? DateTime.MinValue)
                .ThenByDescending(e => e.VersionNumber)
                .ToList()
        };
    }

    public async Task<WorkflowOperationResult> EnsureDraftForEditAsync(int publishedStandardId, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var sourceStandard = await _context.DdtStandards
            .Include(s => s.Owners)
            .Include(s => s.Contacts)
            .Include(s => s.Categories)
            .Include(s => s.SubCategories)
            .Include(s => s.Phases)
            .FirstOrDefaultAsync(s => s.Id == publishedStandardId && !s.IsDeleted, ct);

        if (sourceStandard == null)
            return WorkflowOperationResult.Fail("Standard not found.");

        if (sourceStandard.Stage == "Draft")
            return WorkflowOperationResult.Ok(sourceStandard.Id);

        if (!sourceStandard.IsPublished || sourceStandard.Stage != "Published")
            return WorkflowOperationResult.Fail("Only published standards can be edited in place.");

        var userId = GetCurrentUserId(user);
        if (!await CanEditStandardAsync(sourceStandard, userId, user, ct))
            return WorkflowOperationResult.Fail("You do not have permission to edit this standard.");

        await RetireLegacyChildDraftsAsync(publishedStandardId, ct);

        var now = DateTime.UtcNow;
        var publishedVersion = sourceStandard.Version;

        await EnsurePublishedSnapshotAsync(sourceStandard, userId, ct);

        sourceStandard.PreviousVersion = publishedVersion;
        sourceStandard.Stage = "Draft";
        sourceStandard.IsPublished = false;
        sourceStandard.IsModified = true;
        sourceStandard.GovernanceApproval = false;
        sourceStandard.UpdatedAt = now;

        await CreateAuditLogAsync(sourceStandard.Id, "EditStarted",
            $"Editing started in place. Published version {publishedVersion} retained until publication.", user, ct);
        await _context.SaveChangesAsync(ct);

        return WorkflowOperationResult.Ok(sourceStandard.Id, "You can now edit the standard.");
    }

    public async Task<WorkflowOperationResult> SaveDraftAsync(DdtStandardDraftInput input, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(input.Title))
            return WorkflowOperationResult.Fail("Title is required.");

        var userId = GetCurrentUserId(user);
        if (!userId.HasValue)
            return WorkflowOperationResult.Fail("Unable to identify the current user.");

        try
        {
            DdtStandard standard;
            if (input.Id.HasValue)
            {
                standard = await _context.DdtStandards
                    .Include(s => s.Owners)
                    .Include(s => s.Contacts)
                    .Include(s => s.Categories)
                    .Include(s => s.SubCategories)
                    .Include(s => s.Phases)
                    .FirstOrDefaultAsync(s => s.Id == input.Id.Value && !s.IsDeleted, ct)
                    ?? throw new InvalidOperationException("Standard not found.");

                if (standard.Stage != "Draft")
                    return WorkflowOperationResult.Fail("Only draft standards can be edited.");

                var isCreator = standard.CreatorUserId == userId;
                if (!await CanEditStandardAsync(standard, userId, user, ct) && !isCreator)
                    return WorkflowOperationResult.Fail("You can only edit standards that you created, own, or are a contact on.");

                _context.DdtStandardOwners.RemoveRange(standard.Owners);
                _context.DdtStandardContacts.RemoveRange(standard.Contacts);
                _context.DdtStandardCategories.RemoveRange(standard.Categories);
                _context.DdtStandardPhases.RemoveRange(standard.Phases);
            }
            else
            {
                standard = new DdtStandard
                {
                    Stage = "Draft",
                    Version = DdtStandardVersioning.NewStandardDraftVersion,
                    LegacyReference = await GenerateLegacyReferenceAsync(ct),
                    CreatorUserId = userId.Value,
                    DraftCreated = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DdtStandards.Add(standard);
            }

            standard.Title = input.Title.Trim();
            standard.Slug = GenerateSlug(input.Title);
            standard.Summary = input.Summary;
            standard.Purpose = input.Purpose;
            standard.Criteria = input.Criteria;
            standard.HowToMeet = input.HowToMeet;
            standard.Governance = input.Governance;
            standard.LegalBasis = input.LegalBasis;
            standard.LegalStandard = input.LegalStandard;
            standard.ValidityPeriod = input.ValidityPeriod;
            standard.RelatedGuidance = input.RelatedGuidance;
            standard.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            await ApplyRelationshipsAsync(standard, input, ct);
            await CreateAuditLogAsync(standard.Id, input.Id.HasValue ? "Modified" : "Created",
                $"Standard {(input.Id.HasValue ? "updated" : "created")}. Title: {standard.Title}", user, ct);

            return WorkflowOperationResult.Ok(standard.Id, "Draft saved.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving DDT standard draft");
            return WorkflowOperationResult.Fail("An error occurred while saving the draft.");
        }
    }

    public async Task<WorkflowOperationResult> SubmitForReviewAsync(int id, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.Owners)
            .Include(s => s.Contacts)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

        if (standard == null)
            return WorkflowOperationResult.Fail("Standard not found.");

        var userId = GetCurrentUserId(user);
        var isCreator = userId.HasValue && standard.CreatorUserId == userId;
        if (!await CanEditStandardAsync(standard, userId, user, ct) && !isCreator)
            return WorkflowOperationResult.Fail("You can only submit standards that you created, own, or are a contact on.");

        if (standard.Stage != "Draft")
            return WorkflowOperationResult.Fail($"Only draft standards can be submitted. Current stage: {standard.Stage}.");

        if (!standard.Owners.Any())
            return WorkflowOperationResult.Fail("At least one owner must be assigned before submitting for review.");

        if (string.IsNullOrWhiteSpace(standard.Governance))
            return WorkflowOperationResult.Fail("Governance must be completed before submitting for review.");

        if (!standard.ValidityPeriod.HasValue || standard.ValidityPeriod.Value <= 0)
            return WorkflowOperationResult.Fail("Validity period (months) must be set before submitting for review.");

        standard.Stage = "For Approval";
        standard.UpdatedAt = DateTime.UtcNow;
        await CreateAuditLogAsync(standard.Id, "Submitted", "Standard submitted for review. Stage: Draft → For Approval", user, ct);
        await _context.SaveChangesAsync(ct);

        return WorkflowOperationResult.Ok(standard.Id, "Standard submitted for review. It will appear in the In review queue for the standards forum.");
    }

    public async Task<WorkflowOperationResult> ApproveAsync(int id, string? comment, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (!await StandardsPermissionHelper.CanApproveStandardsAsync(_permissions, user))
            return WorkflowOperationResult.Fail("You do not have permission to approve standards.");

        var standard = await _context.DdtStandards.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (standard == null)
            return WorkflowOperationResult.Fail("Standard not found.");

        if (standard.Stage != "For Approval")
            return WorkflowOperationResult.Fail("Only standards in review can be approved.");

        var userId = GetCurrentUserId(user);
        var now = DateTime.UtcNow;
        standard.Stage = "Awaiting Publication";
        standard.GovernanceApproval = true;
        standard.UpdatedAt = now;

        if (!string.IsNullOrWhiteSpace(comment) && userId.HasValue)
        {
            _context.DdtStandardComments.Add(new DdtStandardComment
            {
                DdtStandardId = standard.Id,
                UserId = userId.Value,
                Title = "Approval comment",
                Comments = comment,
                CommentType = "approval",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await CreateAuditLogAsync(standard.Id, "Approved", "Standard approved. Stage: For Approval → Awaiting Publication", user, ct);
        await _context.SaveChangesAsync(ct);

        return WorkflowOperationResult.Ok(standard.Id, "Standard approved. The owner or forum can publish when ready.");
    }

    public async Task<WorkflowOperationResult> RejectAsync(int id, string reason, ClaimsPrincipal user, CancellationToken ct = default)
    {
        if (!await StandardsPermissionHelper.CanApproveStandardsAsync(_permissions, user))
            return WorkflowOperationResult.Fail("You do not have permission to reject standards.");

        if (string.IsNullOrWhiteSpace(reason))
            return WorkflowOperationResult.Fail("A rejection reason is required.");

        var standard = await _context.DdtStandards.FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);
        if (standard == null)
            return WorkflowOperationResult.Fail("Standard not found.");

        if (standard.Stage != "For Approval")
            return WorkflowOperationResult.Fail("Only standards in review can be rejected.");

        var userId = GetCurrentUserId(user);
        var now = DateTime.UtcNow;
        standard.Stage = "Draft";
        standard.UpdatedAt = now;

        if (userId.HasValue)
        {
            _context.DdtStandardComments.Add(new DdtStandardComment
            {
                DdtStandardId = standard.Id,
                UserId = userId.Value,
                Title = "Rejection reason",
                Comments = reason,
                CommentType = "rejection",
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        await CreateAuditLogAsync(standard.Id, "Rejected", $"Standard rejected. Stage: For Approval → Draft. Reason: {reason}", user, ct);
        await _context.SaveChangesAsync(ct);

        return WorkflowOperationResult.Ok(standard.Id, "Standard rejected. The drafter can revise and resubmit.");
    }

    public async Task<WorkflowOperationResult> PublishAsync(int id, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.Owners)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

        if (standard == null)
            return WorkflowOperationResult.Fail("Standard not found.");

        if (standard.Stage != "Awaiting Publication")
            return WorkflowOperationResult.Fail("Only approved standards awaiting publication can be published.");

        var userId = GetCurrentUserId(user);
        var canPublishRole = await StandardsPermissionHelper.CanPublishStandardsAsync(_permissions, user);
        var isOwner = userId.HasValue && standard.Owners.Any(o => o.UserId == userId);
        if (!canPublishRole && !isOwner)
            return WorkflowOperationResult.Fail("You do not have permission to publish this standard.");

        var now = DateTime.UtcNow;
        var lastPublishedVersion = standard.PreviousVersion;
        string newVersion;
        string versionType;
        string changeSummary;

        if (standard.FirstPublished.HasValue && !string.IsNullOrWhiteSpace(lastPublishedVersion))
        {
            newVersion = DdtStandardVersioning.IncrementVersion(lastPublishedVersion, "patch");
            versionType = "patch";
            changeSummary = "Published standard";
        }
        else
        {
            var draftVersion = standard.Version;
            newVersion = DdtStandardVersioning.IsNewStandardDraftVersion(draftVersion) ? "1.0.0" : draftVersion;
            versionType = newVersion == "1.0.0" ? "major" : "patch";
            changeSummary = newVersion == "1.0.0" ? "Initial publication" : "Published standard";
            lastPublishedVersion = draftVersion;
        }

        standard.Stage = "Published";
        standard.IsPublished = true;
        standard.PublishedAt = now;
        standard.FirstPublished ??= now;
        standard.LastUpdated = now;
        standard.UpdatedAt = now;
        standard.IsModified = false;
        standard.PreviousVersion = lastPublishedVersion;
        standard.Version = newVersion;

        _context.DdtStandardVersions.Add(new DdtStandardVersion
        {
            DdtStandardId = standard.Id,
            VersionNumber = newVersion,
            PreviousVersion = lastPublishedVersion,
            VersionType = versionType,
            ChangeSummary = changeSummary,
            Status = "published",
            PublishedAt = now,
            PublishedByUserId = userId,
            CreatedAt = now
        });

        await CreateAuditLogAsync(standard.Id, "Published",
            $"Standard published. Version: {lastPublishedVersion ?? "—"} → {newVersion}", user, ct);
        await _context.SaveChangesAsync(ct);

        return WorkflowOperationResult.Ok(standard.Id, "Standard published successfully.");
    }

    public async Task<WorkflowOperationResult> DeleteDraftAsync(int id, ClaimsPrincipal user, CancellationToken ct = default)
    {
        var standard = await _context.DdtStandards
            .Include(s => s.Owners)
            .Include(s => s.Contacts)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted, ct);

        if (standard == null)
            return WorkflowOperationResult.Fail("Standard not found.");

        if (standard.Stage != "Draft")
            return WorkflowOperationResult.Fail("Only draft standards can be deleted.");

        var userId = GetCurrentUserId(user);
        var isCreator = userId.HasValue && standard.CreatorUserId == userId;
        if (!await CanEditStandardAsync(standard, userId, user, ct) && !isCreator)
            return WorkflowOperationResult.Fail("You can only delete drafts that you created, own, or are a contact on.");

        if (standard.FirstPublished.HasValue)
        {
            var restored = await TryRestorePublishedFromSnapshotAsync(standard, ct);
            if (!restored)
            {
                var revertVersion = standard.PreviousVersion ?? standard.Version;
                standard.Stage = "Published";
                standard.IsPublished = true;
                standard.Version = revertVersion;
                standard.IsModified = false;
                standard.GovernanceApproval = true;
            }

            standard.UpdatedAt = DateTime.UtcNow;
            await CreateAuditLogAsync(standard.Id, "EditDiscarded", "In-place edit discarded; published standard restored.", user, ct);
            await _context.SaveChangesAsync(ct);

            return WorkflowOperationResult.Ok(standard.Id, "Changes discarded. The published standard has been restored.");
        }

        standard.IsDeleted = true;
        standard.UpdatedAt = DateTime.UtcNow;
        await CreateAuditLogAsync(standard.Id, "Deleted", "Draft standard deleted", user, ct);
        await _context.SaveChangesAsync(ct);

        return WorkflowOperationResult.Ok(null, "Draft deleted.");
    }

    private async Task CorrectDraftVersionFromPublishedParentAsync(DdtStandard draft, CancellationToken ct)
    {
        if (draft.FirstPublished.HasValue)
            return;

        if (!draft.ParentStandardId.HasValue || !DdtStandardVersioning.IsNewStandardDraftVersion(draft.Version))
            return;

        var publishedVersion = draft.PreviousVersion;
        if (string.IsNullOrWhiteSpace(publishedVersion))
        {
            publishedVersion = await _context.DdtStandards.AsNoTracking()
                .Where(s => s.Id == draft.ParentStandardId.Value)
                .Select(s => s.Version)
                .FirstOrDefaultAsync(ct);
        }

        if (string.IsNullOrWhiteSpace(publishedVersion))
            return;

        draft.Version = DdtStandardVersioning.ResolveDraftVersionFromParent(publishedVersion);
        draft.PreviousVersion ??= publishedVersion;
        draft.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
    }

    private async Task<bool> CanEditStandardAsync(DdtStandard standard, int? userId, ClaimsPrincipal user, CancellationToken ct)
    {
        var canManage = await StandardsPermissionHelper.CanManageStandardsAsync(_permissions, user);
        if (canManage)
            return true;

        if (!userId.HasValue)
            return false;

        return standard.Owners.Any(o => o.UserId == userId) ||
               standard.Contacts.Any(c => c.UserId == userId);
    }

    private async Task ApplyRelationshipsAsync(DdtStandard standard, DdtStandardDraftInput input, CancellationToken ct)
    {
        if (input.CategoryIds != null)
        {
            foreach (var categoryId in input.CategoryIds.Distinct())
            {
                if (await _context.StandardCategories.AnyAsync(c => c.Id == categoryId, ct))
                {
                    _context.DdtStandardCategories.Add(new DdtStandardCategory
                    {
                        DdtStandardId = standard.Id,
                        CategoryId = categoryId,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }
        }

        if (input.PhaseIds != null)
        {
            foreach (var phaseId in input.PhaseIds.Distinct())
            {
                _context.DdtStandardPhases.Add(new DdtStandardPhase
                {
                    DdtStandardId = standard.Id,
                    PhaseLookupId = phaseId,
                    Enabled = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
            }
        }

        await AddUsersFromObjectIdsAsync(standard.Id, input.OwnerObjectIds, isOwner: true, ct);
        await AddUsersFromObjectIdsAsync(standard.Id, input.ContactObjectIds, isOwner: false, ct);
        await _context.SaveChangesAsync(ct);
    }

    private async Task AddUsersFromObjectIdsAsync(int standardId, string? objectIdsCsv, bool isOwner, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(objectIdsCsv))
            return;

        foreach (var objectIdStr in objectIdsCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!Guid.TryParse(objectIdStr, out var objectIdGuid))
                continue;

            try
            {
                var directoryUser = await _userDirectory.EnsureUserAsync(objectIdGuid);
                var compassUser = await _context.Users.FirstOrDefaultAsync(u => u.AzureObjectId == directoryUser.AzureObjectId, ct);
                if (compassUser == null)
                    continue;

                if (isOwner)
                {
                    _context.DdtStandardOwners.Add(new DdtStandardOwner
                    {
                        DdtStandardId = standardId,
                        UserId = compassUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    _context.DdtStandardContacts.Add(new DdtStandardContact
                    {
                        DdtStandardId = standardId,
                        UserId = compassUser.Id,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to resolve user {ObjectId} for standard {StandardId}", objectIdStr, standardId);
            }
        }
    }

    private int? GetCurrentUserId(ClaimsPrincipal user)
    {
        var objectIdClaim = user.FindFirstValue(CompassClaimTypes.ObjectIdentifier);
        if (!Guid.TryParse(objectIdClaim, out var objectId))
            return null;

        return _context.Users.AsNoTracking()
            .Where(u => u.AzureObjectId == objectId.ToString())
            .Select(u => (int?)u.Id)
            .FirstOrDefault();
    }

    private static int GetWorkflowStepIndex(string stage, bool isPublished)
    {
        if (isPublished && stage == "Published") return 3;
        return stage switch
        {
            "For Approval" or "Under Review" => 1,
            "Awaiting Publication" => 2,
            _ => 0
        };
    }

    private static string GenerateSlug(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return string.Empty;

        var slug = title.ToLowerInvariant();
        slug = Regex.Replace(slug, @"[^a-z0-9\s-]", "");
        slug = Regex.Replace(slug, @"\s+", " ").Trim();
        slug = Regex.Replace(slug, @"\s", "-");
        return Regex.Replace(slug, @"-+", "-");
    }

    private async Task<string> GenerateLegacyReferenceAsync(CancellationToken ct)
    {
        var refs = await _context.DdtStandards.AsNoTracking()
            .Where(s => !string.IsNullOrEmpty(s.LegacyReference) && s.LegacyReference.StartsWith("STD-"))
            .Select(s => s.LegacyReference!)
            .ToListAsync(ct);

        var max = 0;
        foreach (var r in refs)
        {
            if (r.Length > 4 && int.TryParse(r[4..], out var n) && n > max)
                max = n;
        }

        return $"STD-{max + 1}";
    }

    private async Task RetireLegacyChildDraftsAsync(int publishedStandardId, CancellationToken ct)
    {
        var childDraftIds = await _context.DdtStandards
            .Where(s => !s.IsDeleted && s.ParentStandardId == publishedStandardId && s.Stage == "Draft" && s.Id != publishedStandardId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (childDraftIds.Count == 0)
            return;

        var now = DateTime.UtcNow;
        foreach (var childId in childDraftIds)
        {
            var child = await _context.DdtStandards.FindAsync([childId], ct);
            if (child == null)
                continue;

            child.IsDeleted = true;
            child.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(ct);
    }

    private async Task EnsurePublishedSnapshotAsync(DdtStandard standard, int? userId, CancellationToken ct)
    {
        var alreadySnapshotted = await _context.DdtStandardVersions.AsNoTracking()
            .AnyAsync(v => v.DdtStandardId == standard.Id
                && v.VersionNumber == standard.Version
                && v.Status == "published", ct);
        if (alreadySnapshotted)
            return;

        _context.DdtStandardVersions.Add(new DdtStandardVersion
        {
            DdtStandardId = standard.Id,
            VersionNumber = standard.Version,
            PreviousVersion = standard.PreviousVersion,
            VersionType = "patch",
            ChangeSummary = "Snapshot before edit",
            Status = "published",
            Snapshot = SerializeEditSnapshot(standard),
            PublishedAt = standard.PublishedAt ?? DateTime.UtcNow,
            PublishedByUserId = userId,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync(ct);
    }

    private async Task<bool> TryRestorePublishedFromSnapshotAsync(DdtStandard standard, CancellationToken ct)
    {
        var snapshotVersion = await _context.DdtStandardVersions
            .Where(v => v.DdtStandardId == standard.Id && v.Status == "published" && v.Snapshot != null)
            .OrderByDescending(v => v.PublishedAt ?? v.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (snapshotVersion?.Snapshot == null)
            return false;

        DdtStandardEditSnapshot? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<DdtStandardEditSnapshot>(snapshotVersion.Snapshot);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize snapshot for standard {StandardId}", standard.Id);
            return false;
        }

        if (snapshot == null)
            return false;

        await _context.Entry(standard).Collection(s => s.Owners).LoadAsync(ct);
        await _context.Entry(standard).Collection(s => s.Contacts).LoadAsync(ct);
        await _context.Entry(standard).Collection(s => s.Categories).LoadAsync(ct);
        await _context.Entry(standard).Collection(s => s.SubCategories).LoadAsync(ct);
        await _context.Entry(standard).Collection(s => s.Phases).LoadAsync(ct);

        _context.DdtStandardOwners.RemoveRange(standard.Owners);
        _context.DdtStandardContacts.RemoveRange(standard.Contacts);
        _context.DdtStandardCategories.RemoveRange(standard.Categories);
        _context.DdtStandardSubCategories.RemoveRange(standard.SubCategories);
        _context.DdtStandardPhases.RemoveRange(standard.Phases);

        standard.Title = snapshot.Title;
        standard.Slug = snapshot.Slug;
        standard.Summary = snapshot.Summary;
        standard.Purpose = snapshot.Purpose;
        standard.Criteria = snapshot.Criteria;
        standard.HowToMeet = snapshot.HowToMeet;
        standard.Governance = snapshot.Governance;
        standard.GovernanceApproval = snapshot.GovernanceApproval;
        standard.Version = snapshot.Version;
        standard.PreviousVersion = snapshot.PreviousVersion;
        standard.LegalStandard = snapshot.LegalStandard;
        standard.LegalBasis = snapshot.LegalBasis;
        standard.ValidityPeriod = snapshot.ValidityPeriod;
        standard.RelatedGuidance = snapshot.RelatedGuidance;
        standard.Stage = "Published";
        standard.IsPublished = true;
        standard.IsModified = false;

        var now = DateTime.UtcNow;
        foreach (var ownerUserId in snapshot.OwnerUserIds.Distinct())
        {
            _context.DdtStandardOwners.Add(new DdtStandardOwner
            {
                DdtStandardId = standard.Id,
                UserId = ownerUserId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        foreach (var contactUserId in snapshot.ContactUserIds.Distinct())
        {
            _context.DdtStandardContacts.Add(new DdtStandardContact
            {
                DdtStandardId = standard.Id,
                UserId = contactUserId,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        foreach (var categoryId in snapshot.CategoryIds.Distinct())
        {
            _context.DdtStandardCategories.Add(new DdtStandardCategory
            {
                DdtStandardId = standard.Id,
                CategoryId = categoryId,
                CreatedAt = now
            });
        }

        foreach (var subCategoryId in snapshot.SubCategoryIds.Distinct())
        {
            _context.DdtStandardSubCategories.Add(new DdtStandardSubCategory
            {
                DdtStandardId = standard.Id,
                SubCategoryId = subCategoryId,
                CreatedAt = now
            });
        }

        foreach (var phaseId in snapshot.PhaseIds.Distinct())
        {
            _context.DdtStandardPhases.Add(new DdtStandardPhase
            {
                DdtStandardId = standard.Id,
                PhaseLookupId = phaseId,
                Enabled = true,
                CreatedAt = now,
                UpdatedAt = now
            });
        }

        return true;
    }

    private static string SerializeEditSnapshot(DdtStandard standard) =>
        JsonSerializer.Serialize(new DdtStandardEditSnapshot
        {
            Title = standard.Title,
            Slug = standard.Slug,
            Summary = standard.Summary,
            Purpose = standard.Purpose,
            Criteria = standard.Criteria,
            HowToMeet = standard.HowToMeet,
            Governance = standard.Governance,
            GovernanceApproval = standard.GovernanceApproval,
            Version = standard.Version,
            PreviousVersion = standard.PreviousVersion,
            LegalStandard = standard.LegalStandard,
            LegalBasis = standard.LegalBasis,
            ValidityPeriod = standard.ValidityPeriod,
            RelatedGuidance = standard.RelatedGuidance,
            CategoryIds = standard.Categories.Select(c => c.CategoryId).ToList(),
            SubCategoryIds = standard.SubCategories.Select(c => c.SubCategoryId).ToList(),
            PhaseIds = standard.Phases.Where(p => p.Enabled).Select(p => p.PhaseLookupId).ToList(),
            OwnerUserIds = standard.Owners.Select(o => o.UserId).ToList(),
            ContactUserIds = standard.Contacts.Select(c => c.UserId).ToList()
        });

    private async Task CreateAuditLogAsync(int standardId, string action, string? details, ClaimsPrincipal user, CancellationToken ct)
    {
        var http = _httpContextAccessor.HttpContext;
        var userId = GetCurrentUserId(user);
        var dbUser = userId.HasValue ? await _context.Users.FindAsync([userId.Value], ct) : null;
        var userEmail = StandardsPermissionHelper.GetUserEmail(user);

        _context.AuditLogs.Add(new AuditLog
        {
            Entity = "DdtStandard",
            EntityId = standardId.ToString(),
            EntityReference = $"DDT-{standardId}",
            Action = action,
            ChangedBy = dbUser?.Name ?? "Unknown",
            ChangedByUserId = userId?.ToString(),
            ChangedByEmail = userEmail,
            IpAddress = http?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = http?.Request.Headers.UserAgent.ToString(),
            ChangedUtc = DateTime.UtcNow,
            AfterJson = details != null ? JsonSerializer.Serialize(new { Details = details }) : null
        });
    }
}
