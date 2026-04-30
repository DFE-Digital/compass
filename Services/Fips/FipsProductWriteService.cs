using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public sealed class FipsProductWriteService : IFipsProductWriteService
{
    private readonly CompassDbContext _db;

    public FipsProductWriteService(CompassDbContext db)
    {
        _db = db;
    }

    public async Task<FipsProductWriteOutcome> TryUpdateAsync(
        Guid productId,
        string actorEmail,
        string? auditChangedByDisplay,
        bool requireServiceOwnerManager,
        string? userDescription,
        int? phaseId,
        string? productURL,
        int[]? businessAreaIds,
        int[]? channelIds,
        int[]? userGroupIds,
        int[]? typeIds,
        int[]? categorisationItemIds = null,
        int? reportingContactUserId = null,
        bool isEnterpriseService = false,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.CMDBProducts
            .Include(p => p.BusinessAreas)
            .Include(p => p.Channels)
            .Include(p => p.UserGroups)
            .Include(p => p.Types)
            .Include(p => p.CategorisationItems)
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            return new FipsProductWriteOutcome { NotFound = true };

        if (requireServiceOwnerManager && !IsContactManager(product, actorEmail))
            return new FipsProductWriteOutcome { Forbidden = true };

        var changes = new List<string>();

        var changedBy = string.IsNullOrWhiteSpace(auditChangedByDisplay) ? actorEmail : auditChangedByDisplay.Trim();

        if (product.UserDescription != userDescription)
        {
            LogAudit(product.Id, actorEmail, changedBy, "update", "UserDescription", product.UserDescription, userDescription);
            product.UserDescription = userDescription;
            changes.Add("User description");
        }

        if (product.PhaseId != phaseId)
        {
            LogAudit(product.Id, actorEmail, changedBy, "update", "PhaseId", product.PhaseId?.ToString(), phaseId?.ToString());
            product.PhaseId = phaseId;
            changes.Add("Phase");
        }

        if (product.ProductURL != productURL)
        {
            LogAudit(product.Id, actorEmail, changedBy, "update", "ProductURL", product.ProductURL, productURL);
            product.ProductURL = productURL;
            changes.Add("Product URL");
        }

        if (product.IsEnterpriseService != isEnterpriseService)
        {
            LogAudit(product.Id, actorEmail, changedBy, "update", "IsEnterpriseService",
                product.IsEnterpriseService.ToString(), isEnterpriseService.ToString());
            product.IsEnterpriseService = isEnterpriseService;
            changes.Add("Enterprise service");
        }

        SyncJoinTable(product.BusinessAreas, businessAreaIds ?? Array.Empty<int>(),
            product.Id, changes, "Business areas", actorEmail, changedBy,
            (pid, fkId) => new CMDBProductBusinessArea { CMDBProductId = pid, FipsBusinessAreaId = fkId },
            x => x.FipsBusinessAreaId);

        SyncJoinTable(product.Channels, channelIds ?? Array.Empty<int>(),
            product.Id, changes, "Channels", actorEmail, changedBy,
            (pid, fkId) => new CMDBProductChannel { CMDBProductId = pid, FipsChannelId = fkId },
            x => x.FipsChannelId);

        SyncJoinTable(product.UserGroups, userGroupIds ?? Array.Empty<int>(),
            product.Id, changes, "User groups", actorEmail, changedBy,
            (pid, fkId) => new CMDBProductUserGroup { CMDBProductId = pid, FipsUserGroupId = fkId },
            x => x.FipsUserGroupId);

        SyncJoinTable(product.Types, typeIds ?? Array.Empty<int>(),
            product.Id, changes, "Types", actorEmail, changedBy,
            (pid, fkId) => new CMDBProductType { CMDBProductId = pid, FipsTypeId = fkId },
            x => x.FipsTypeId);

        var catRequested = categorisationItemIds ?? Array.Empty<int>();
        var allowedCatIds = await _db.FipsCategorisationItems.AsNoTracking()
            .Where(i => catRequested.Contains(i.Id) && i.Active && i.Group.Active)
            .Select(i => i.Id)
            .ToListAsync(cancellationToken);
        SyncJoinTable(product.CategorisationItems, allowedCatIds.ToArray(),
            product.Id, changes, "Categorisation", actorEmail, changedBy,
            (pid, fkId) => new CMDBProductFipsCategorisationItem
                { CMDBProductId = pid, FipsCategorisationItemId = fkId },
            x => x.FipsCategorisationItemId);

        await TryUpsertReportingContactAsync(product, reportingContactUserId, actorEmail, changedBy, changes, cancellationToken);

        if (changes.Count > 0)
        {
            product.UpdatedAt = DateTime.UtcNow;
            product.UpdatedBy = actorEmail;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return new FipsProductWriteOutcome { Changes = changes };
    }

    public async Task<FipsProductWriteOutcome> TryChangeStatusAsync(
        Guid productId,
        string actorEmail,
        string? auditChangedByDisplay,
        bool requireServiceOwnerManager,
        CMDBProductStatus newStatus,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.CMDBProducts
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            return new FipsProductWriteOutcome { NotFound = true };

        if (requireServiceOwnerManager && !IsContactManager(product, actorEmail))
            return new FipsProductWriteOutcome { Forbidden = true };

        var oldStatus = product.Status;
        if (oldStatus == newStatus)
            return new FipsProductWriteOutcome();

        var changedBy = string.IsNullOrWhiteSpace(auditChangedByDisplay) ? actorEmail : auditChangedByDisplay.Trim();
        LogAudit(product.Id, actorEmail, changedBy, "update", "Status", oldStatus.ToString(), newStatus.ToString());
        product.Status = newStatus;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = actorEmail;
        await _db.SaveChangesAsync(cancellationToken);

        return new FipsProductWriteOutcome { Changes = ["Status"] };
    }

    public async Task<FipsProductWriteOutcome> TryUpdateProductUrlOnlyAsync(
        Guid productId,
        string actorEmail,
        string? auditChangedByDisplay,
        string? productUrl,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.CMDBProducts
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
            return new FipsProductWriteOutcome { NotFound = true };

        var normalized = string.IsNullOrWhiteSpace(productUrl) ? null : productUrl.Trim();

        if (product.ProductURL == normalized)
            return new FipsProductWriteOutcome();

        var changedBy = string.IsNullOrWhiteSpace(auditChangedByDisplay) ? actorEmail : auditChangedByDisplay.Trim();
        LogAudit(product.Id, actorEmail, changedBy, "update", "ProductURL", product.ProductURL, normalized);
        product.ProductURL = normalized;
        product.UpdatedAt = DateTime.UtcNow;
        product.UpdatedBy = actorEmail;
        await _db.SaveChangesAsync(cancellationToken);

        return new FipsProductWriteOutcome { Changes = ["Product URL"] };
    }

    private static bool IsContactManager(CMDBProduct product, string email) =>
        product.Contacts.Any(c =>
            c.CanManage &&
            string.Equals(c.UserEmail, email, StringComparison.OrdinalIgnoreCase));

    private async Task TryUpsertReportingContactAsync(
        CMDBProduct product,
        int? reportingContactUserId,
        string actorEmail,
        string changedByDisplay,
        List<string> changes,
        CancellationToken cancellationToken)
    {
        if (!reportingContactUserId.HasValue || reportingContactUserId.Value <= 0)
            return;

        var selectedUser = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == reportingContactUserId.Value, cancellationToken);

        if (selectedUser == null || string.IsNullOrWhiteSpace(selectedUser.Email))
            return;

        var email = selectedUser.Email.Trim();
        var name = string.IsNullOrWhiteSpace(selectedUser.Name) ? null : selectedUser.Name.Trim();

        if (email.Length > 320)
            email = email[..320];
        if (!string.IsNullOrWhiteSpace(name) && name.Length > 200)
            name = name[..200];

        var reportingRoleId = await _db.FipsContactRoles
            .Where(r => r.Active && (r.Name == "Reporting contact" || r.Name == "Reporting user"))
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(cancellationToken)
            ?? 4;

        var existing = product.Contacts.FirstOrDefault(c =>
            c.FipsContactRoleId == reportingRoleId &&
            string.Equals((c.UserEmail ?? string.Empty).Trim(), email, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            product.Contacts.Add(new CMDBProductContact
            {
                CMDBProductId = product.Id,
                FipsContactRoleId = reportingRoleId,
                UserEmail = email,
                UserName = string.IsNullOrWhiteSpace(name) ? null : name,
                CanManage = false
            });

            LogAudit(product.Id, actorEmail, changedByDisplay, "update", "ReportingContact",
                null, string.IsNullOrWhiteSpace(name) ? email : $"{name} <{email}>");
            changes.Add("Reporting contact");
            return;
        }

        var oldName = existing.UserName;
        var newName = string.IsNullOrWhiteSpace(name) ? oldName : name;
        if (!string.Equals(oldName ?? string.Empty, newName ?? string.Empty, StringComparison.Ordinal))
        {
            existing.UserName = newName;
            LogAudit(product.Id, actorEmail, changedByDisplay, "update", "ReportingContactName", oldName, newName);
            changes.Add("Reporting contact");
        }
    }

    private void LogAudit(Guid productId, string actorEmail, string changedByDisplay, string action, string field, string? previousValue, string? newValue)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Entity = "CMDBProduct",
            EntityId = productId.ToString(),
            EntityReference = field,
            Action = action,
            ChangedBy = changedByDisplay.Length > 200 ? changedByDisplay[..200] : changedByDisplay,
            ChangedByEmail = actorEmail.Length > 320 ? actorEmail[..320] : actorEmail,
            ChangedUtc = DateTime.UtcNow,
            BeforeJson = previousValue,
            AfterJson = newValue
        });
    }

    private void SyncJoinTable<T>(ICollection<T> existing, int[] desiredIds,
        Guid productId, List<string> changes, string label, string actorEmail, string changedByDisplay,
        Func<Guid, int, T> createNew, Func<T, int> getFkId) where T : class
    {
        var currentIds = existing.Select(getFkId).ToHashSet();
        var desired = desiredIds.ToHashSet();

        var toRemove = existing.Where(x => !desired.Contains(getFkId(x))).ToList();
        var toAdd = desired.Where(id => !currentIds.Contains(id)).ToList();

        if (toRemove.Count == 0 && toAdd.Count == 0) return;

        foreach (var item in toRemove) existing.Remove(item);
        foreach (var id in toAdd) existing.Add(createNew(productId, id));

        LogAudit(productId, actorEmail, changedByDisplay, "update", label,
            string.Join(",", currentIds.OrderBy(x => x)),
            string.Join(",", desired.OrderBy(x => x)));

        changes.Add(label);
    }
}
