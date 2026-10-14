using Compass.Data;
using Compass.Models;
using Compass.Services;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Raid;

/// <summary>
/// RAID register visibility: only explicit register members (or Admin / Central Ops) can see a register
/// and all of its items. Item-level owners / SROs / creators may still see their own risks and issues
/// even when they are not register members.
/// </summary>
public static class RaidRegisterAccess
{
    public static async Task<bool> IsRaidRegisterAdminAsync(
        IPermissionService permissions,
        string? userEmail)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return false;
        // Super admin, Central Operations Admin, or Admin group.
        return await permissions.IsOperationConsoleUserAsync(userEmail.Trim());
    }

    public static bool IsExplicitMember(RaidRegister register, int userId) =>
        register.Users.Any(u => u.UserId == userId)
        || register.CreatedByUserId == userId;

    public static RaidRegisterRole? TryGetMemberRole(RaidRegister register, int userId)
    {
        var membership = register.Users.FirstOrDefault(u => u.UserId == userId);
        if (membership != null)
            return membership.Role;

        if (register.CreatedByUserId == userId)
            return RaidRegisterRole.Owner;

        return null;
    }

    public static bool CanManageRegister(RaidRegister register, int userId, bool isAdmin)
    {
        if (isAdmin) return true;
        var role = TryGetMemberRole(register, userId);
        return role is RaidRegisterRole.Owner or RaidRegisterRole.Manager;
    }

    public static async Task<bool> UserIsMemberOfRegisterAsync(
        CompassDbContext db,
        int registerId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await db.RaidRegisters.AsNoTracking()
            .AnyAsync(r =>
                r.Id == registerId
                && !r.IsDeleted
                && (r.CreatedByUserId == userId || r.Users.Any(u => u.UserId == userId)),
                cancellationToken);
    }

    /// <summary>
    /// Risk is visible when: admin; not on any live register; user is a member of a register that
    /// contains it; or user has direct item-level involvement.
    /// </summary>
    public static IQueryable<Risk> WhereRiskVisibleToViewer(
        this IQueryable<Risk> query,
        CompassDbContext db,
        int? userId,
        string emailLower,
        bool isAdmin)
    {
        if (isAdmin)
            return query;

        if (!userId.HasValue && string.IsNullOrEmpty(emailLower))
            return query.Where(_ => false);

        var uid = userId;

        return query.Where(r =>
            // Not linked to any active register → existing broader RAID rules still apply via caller filters
            !db.RaidRegisterRisks.Any(rr => rr.RiskId == r.Id && !rr.RaidRegister.IsDeleted)
            // Member of at least one register that contains this risk
            || (uid.HasValue && db.RaidRegisterRisks.Any(rr =>
                rr.RiskId == r.Id
                && !rr.RaidRegister.IsDeleted
                && (rr.RaidRegister.CreatedByUserId == uid.Value
                    || rr.RaidRegister.Users.Any(u => u.UserId == uid.Value))))
            // Item-level access (owner / SRO / creator / action assignee)
            || (uid.HasValue && (r.CreatedByUserId == uid.Value || r.OwnerUserId == uid.Value || r.SroUserId == uid.Value))
            || (r.OwnerUserId == null && r.OwnerEmail != null && r.OwnerEmail.ToLower() == emailLower)
            || r.RiskActions.Any(ra =>
                !ra.Action.IsDeleted &&
                ((uid.HasValue && ra.Action.AssignedToUserId == uid) ||
                 (ra.Action.AssignedToEmail != null && ra.Action.AssignedToEmail.ToLower() == emailLower))));
    }

    public static IQueryable<Issue> WhereIssueVisibleToViewer(
        this IQueryable<Issue> query,
        CompassDbContext db,
        int? userId,
        string emailLower,
        bool isAdmin)
    {
        if (isAdmin)
            return query;

        if (!userId.HasValue && string.IsNullOrEmpty(emailLower))
            return query.Where(_ => false);

        var uid = userId;

        return query.Where(i =>
            !db.RaidRegisterIssues.Any(ri => ri.IssueId == i.Id && !ri.RaidRegister.IsDeleted)
            || (uid.HasValue && db.RaidRegisterIssues.Any(ri =>
                ri.IssueId == i.Id
                && !ri.RaidRegister.IsDeleted
                && (ri.RaidRegister.CreatedByUserId == uid.Value
                    || ri.RaidRegister.Users.Any(u => u.UserId == uid.Value))))
            || (uid.HasValue && (i.CreatedByUserId == uid.Value || i.OwnerUserId == uid.Value || i.SroUserId == uid.Value))
            || i.IssueActions.Any(ia =>
                !ia.Action.IsDeleted &&
                ((uid.HasValue && ia.Action.AssignedToUserId == uid) ||
                 (ia.Action.AssignedToEmail != null && ia.Action.AssignedToEmail.ToLower() == emailLower))));
    }

    public static async Task<bool> CanViewRiskAsync(
        CompassDbContext db,
        Risk risk,
        int? userId,
        string emailLower,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin) return true;

        var onRegisters = await db.RaidRegisterRisks.AsNoTracking()
            .Where(rr => rr.RiskId == risk.Id && !rr.RaidRegister.IsDeleted)
            .Select(rr => new { rr.RaidRegisterId, rr.RaidRegister.CreatedByUserId })
            .ToListAsync(cancellationToken);

        if (onRegisters.Count == 0)
            return true; // not register-scoped

        if (userId.HasValue)
        {
            var registerIds = onRegisters.Select(x => x.RaidRegisterId).ToList();
            var isMember = await db.RaidRegisterUsers.AsNoTracking()
                .AnyAsync(u => registerIds.Contains(u.RaidRegisterId) && u.UserId == userId.Value, cancellationToken)
                || onRegisters.Any(x => x.CreatedByUserId == userId.Value);
            if (isMember) return true;

            if (risk.CreatedByUserId == userId.Value || risk.OwnerUserId == userId.Value || risk.SroUserId == userId.Value)
                return true;
        }

        if (risk.OwnerUserId == null
            && !string.IsNullOrWhiteSpace(risk.OwnerEmail)
            && risk.OwnerEmail.Trim().ToLowerInvariant() == emailLower)
            return true;

        if (userId.HasValue)
        {
            var assigned = await db.RiskActions.AsNoTracking()
                .AnyAsync(ra => ra.RiskId == risk.Id && !ra.Action.IsDeleted && ra.Action.AssignedToUserId == userId.Value, cancellationToken);
            if (assigned) return true;
        }

        return false;
    }

    public static async Task<bool> CanViewIssueAsync(
        CompassDbContext db,
        Issue issue,
        int? userId,
        string emailLower,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin) return true;

        var onRegisters = await db.RaidRegisterIssues.AsNoTracking()
            .Where(ri => ri.IssueId == issue.Id && !ri.RaidRegister.IsDeleted)
            .Select(ri => new { ri.RaidRegisterId, ri.RaidRegister.CreatedByUserId })
            .ToListAsync(cancellationToken);

        if (onRegisters.Count == 0)
            return true;

        if (userId.HasValue)
        {
            var registerIds = onRegisters.Select(x => x.RaidRegisterId).ToList();
            var isMember = await db.RaidRegisterUsers.AsNoTracking()
                .AnyAsync(u => registerIds.Contains(u.RaidRegisterId) && u.UserId == userId.Value, cancellationToken)
                || onRegisters.Any(x => x.CreatedByUserId == userId.Value);
            if (isMember) return true;

            if (issue.CreatedByUserId == userId.Value || issue.OwnerUserId == userId.Value || issue.SroUserId == userId.Value)
                return true;

            var assigned = await db.IssueActions.AsNoTracking()
                .AnyAsync(ia => ia.IssueId == issue.Id && !ia.Action.IsDeleted && ia.Action.AssignedToUserId == userId.Value, cancellationToken);
            if (assigned) return true;
        }

        return false;
    }

    /// <summary>
    /// Edit gate for register-linked risks: Admin, item owner/SRO/creator, or Owner/Manager
    /// of a register that contains the risk. Risks not on any register keep prior open edit.
    /// </summary>
    public static async Task<bool> CanManageRiskAsync(
        CompassDbContext db,
        Risk risk,
        int? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin) return true;
        if (!userId.HasValue) return false;

        if (risk.CreatedByUserId == userId.Value
            || risk.OwnerUserId == userId.Value
            || risk.SroUserId == userId.Value)
            return true;

        var registerIds = await db.RaidRegisterRisks.AsNoTracking()
            .Where(rr => rr.RiskId == risk.Id && !rr.RaidRegister.IsDeleted)
            .Select(rr => rr.RaidRegisterId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (registerIds.Count == 0)
            return true;

        return await UserIsOwnerOrManagerOfAnyRegisterAsync(db, registerIds, userId.Value, cancellationToken);
    }

    public static async Task<bool> CanManageIssueAsync(
        CompassDbContext db,
        Issue issue,
        int? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default)
    {
        if (isAdmin) return true;
        if (!userId.HasValue) return false;

        if (issue.CreatedByUserId == userId.Value
            || issue.OwnerUserId == userId.Value
            || issue.SroUserId == userId.Value)
            return true;

        var registerIds = await db.RaidRegisterIssues.AsNoTracking()
            .Where(ri => ri.IssueId == issue.Id && !ri.RaidRegister.IsDeleted)
            .Select(ri => ri.RaidRegisterId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (registerIds.Count == 0)
            return true;

        return await UserIsOwnerOrManagerOfAnyRegisterAsync(db, registerIds, userId.Value, cancellationToken);
    }

    private static async Task<bool> UserIsOwnerOrManagerOfAnyRegisterAsync(
        CompassDbContext db,
        IReadOnlyList<int> registerIds,
        int userId,
        CancellationToken cancellationToken)
    {
        if (registerIds.Count == 0) return false;

        var isManager = await db.RaidRegisterUsers.AsNoTracking()
            .AnyAsync(
                u => registerIds.Contains(u.RaidRegisterId)
                     && u.UserId == userId
                     && (u.Role == RaidRegisterRole.Owner || u.Role == RaidRegisterRole.Manager),
                cancellationToken);
        if (isManager) return true;

        return await db.RaidRegisters.AsNoTracking()
            .AnyAsync(r => registerIds.Contains(r.Id) && !r.IsDeleted && r.CreatedByUserId == userId, cancellationToken);
    }
}
