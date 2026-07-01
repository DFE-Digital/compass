using Compass.Data;
using Compass.Models;
using Compass.Models.Modern.Work;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Modern;

/// <summary>
/// Merges legacy junction-table governance roles with <see cref="ProjectContact"/> rows for modern work UI.
/// </summary>
internal static class ProjectGovernanceContacts
{
    internal const string GovernanceTeamStatus = "governance";

    private const int SroJunctionIdBase = -1_000_000;
    private const int ServiceOwnerJunctionIdBase = -2_000_000;
    private const int PmoJunctionIdBase = -3_000_000;

    private static readonly Dictionary<string, int> StandardRoleToId = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SRO"] = 1,
        ["Service Owner"] = 2,
        ["PMO Contact"] = 3,
        ["Reporting contact"] = 4
    };

    internal static bool IsJunctionContactId(int id) => id < 0;

    internal static bool TryGovernanceRoleKindFromJunctionContactId(int contactId, out string kind)
    {
        kind = contactId switch
        {
            _ when contactId <= SroJunctionIdBase && contactId > ServiceOwnerJunctionIdBase => GovernanceRoleKindFromTypeId(1),
            _ when contactId <= ServiceOwnerJunctionIdBase && contactId > PmoJunctionIdBase => GovernanceRoleKindFromTypeId(2),
            _ when contactId <= PmoJunctionIdBase => GovernanceRoleKindFromTypeId(3),
            _ => ""
        };
        return !string.IsNullOrEmpty(kind);
    }

    internal static bool TryGovernanceRoleKindToTypeId(string? kind, out int roleTypeId) =>
        (kind ?? "").Trim().ToLowerInvariant() switch
        {
            "sro" => Assign(1, out roleTypeId),
            "serviceowner" => Assign(2, out roleTypeId),
            "pmo" => Assign(3, out roleTypeId),
            _ => Assign(0, out roleTypeId)
        };

    internal static string GovernanceRoleKindFromTypeId(int roleTypeId) => roleTypeId switch
    {
        1 => "sro",
        2 => "serviceowner",
        3 => "pmo",
        _ => ""
    };

    internal static string GovernanceRoleDisplayName(int roleTypeId) => roleTypeId switch
    {
        1 => "Senior Responsible Officer(s)",
        2 => "Service Owner(s)",
        3 => "PMO Contacts",
        _ => "Contacts"
    };

    private static bool Assign(int value, out int roleTypeId)
    {
        roleTypeId = value;
        return value > 0;
    }

    internal static async Task AddGovernanceRoleUserAsync(
        CompassDbContext db,
        int projectId,
        int roleTypeId,
        Guid azureObjectId,
        CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .Include(p => p.SeniorResponsibleOfficers)
            .Include(p => p.ServiceOwners)
            .Include(p => p.PmoContacts)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);
        if (project == null) return;

        var user = await db.Users.FirstOrDefaultAsync(
            u => u.AzureObjectId == azureObjectId.ToString(), cancellationToken);
        if (user == null) return;

        switch (roleTypeId)
        {
            case 1 when project.SeniorResponsibleOfficers.All(s => s.UserId != user.Id):
                project.SeniorResponsibleOfficers.Add(new ProjectSeniorResponsibleOfficer
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
                break;
            case 2 when project.ServiceOwners.All(s => s.UserId != user.Id):
                project.ServiceOwners.Add(new ProjectServiceOwner
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
                break;
            case 3 when project.PmoContacts.All(s => s.UserId != user.Id):
                project.PmoContacts.Add(new ProjectPmoContact
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
                break;
        }

        project.UpdatedAt = DateTime.UtcNow;
    }

    internal static async Task RemoveGovernanceRoleUserAsync(
        CompassDbContext db,
        int projectId,
        int roleTypeId,
        Guid azureObjectId,
        CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .Include(p => p.SeniorResponsibleOfficers).ThenInclude(s => s.User)
            .Include(p => p.ServiceOwners).ThenInclude(s => s.User)
            .Include(p => p.PmoContacts).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);
        if (project == null) return;

        var target = azureObjectId.ToString();

        switch (roleTypeId)
        {
            case 1:
                var sro = project.SeniorResponsibleOfficers.FirstOrDefault(
                    s => string.Equals(s.User?.AzureObjectId, target, StringComparison.OrdinalIgnoreCase));
                if (sro != null) db.ProjectSeniorResponsibleOfficers.Remove(sro);
                break;
            case 2:
                var so = project.ServiceOwners.FirstOrDefault(
                    s => string.Equals(s.User?.AzureObjectId, target, StringComparison.OrdinalIgnoreCase));
                if (so != null) db.ProjectServiceOwners.Remove(so);
                break;
            case 3:
                var pmo = project.PmoContacts.FirstOrDefault(
                    p => string.Equals(p.User?.AzureObjectId, target, StringComparison.OrdinalIgnoreCase));
                if (pmo != null) db.ProjectPmoContacts.Remove(pmo);
                break;
        }

        project.UpdatedAt = DateTime.UtcNow;
    }

    internal static List<Guid> ParseSelectedObjectIds(string? csv) =>
        string.IsNullOrWhiteSpace(csv)
            ? new List<Guid>()
            : csv.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => Guid.TryParse(x, out _))
                .Select(Guid.Parse)
                .Distinct()
                .ToList();

    internal static async Task ReplaceGovernanceRoleUsersAsync(
        CompassDbContext db,
        int projectId,
        int roleTypeId,
        IReadOnlyList<Guid> selectedAzureObjectIds,
        CancellationToken cancellationToken)
    {
        var project = await db.Projects
            .Include(p => p.SeniorResponsibleOfficers).ThenInclude(s => s.User)
            .Include(p => p.ServiceOwners).ThenInclude(s => s.User)
            .Include(p => p.PmoContacts).ThenInclude(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);
        if (project == null)
            return;

        var selected = selectedAzureObjectIds.Distinct().ToList();

        switch (roleTypeId)
        {
            case 1:
                await ReplaceJunctionCollectionAsync(
                    db, project.SeniorResponsibleOfficers, selected,
                    row => db.ProjectSeniorResponsibleOfficers.Remove(row),
                    userId => project.SeniorResponsibleOfficers.Add(new ProjectSeniorResponsibleOfficer
                    {
                        ProjectId = projectId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    }),
                    cancellationToken);
                break;
            case 2:
                await ReplaceJunctionCollectionAsync(
                    db, project.ServiceOwners, selected,
                    row => db.ProjectServiceOwners.Remove(row),
                    userId => project.ServiceOwners.Add(new ProjectServiceOwner
                    {
                        ProjectId = projectId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    }),
                    cancellationToken);
                break;
            case 3:
                await ReplaceJunctionCollectionAsync(
                    db, project.PmoContacts, selected,
                    row => db.ProjectPmoContacts.Remove(row),
                    userId => project.PmoContacts.Add(new ProjectPmoContact
                    {
                        ProjectId = projectId,
                        UserId = userId,
                        CreatedAt = DateTime.UtcNow
                    }),
                    cancellationToken);
                break;
            default:
                return;
        }

        project.UpdatedAt = DateTime.UtcNow;
    }

    private static async Task ReplaceJunctionCollectionAsync<TRow>(
        CompassDbContext db,
        ICollection<TRow> collection,
        IReadOnlyList<Guid> selectedAzureObjectIds,
        Action<TRow> remove,
        Action<int> addByUserId,
        CancellationToken cancellationToken)
        where TRow : class
    {
        foreach (var row in collection.ToList())
        {
            var user = GetJunctionUser(row);
            if (user?.AzureObjectId == null || !Guid.TryParse(user.AzureObjectId, out var guid) || !selectedAzureObjectIds.Contains(guid))
                remove(row);
        }

        var existingGuids = collection
            .Select(GetJunctionUser)
            .Where(u => u?.AzureObjectId != null && Guid.TryParse(u.AzureObjectId, out _))
            .Select(u => Guid.Parse(u!.AzureObjectId!))
            .ToHashSet();

        foreach (var objectId in selectedAzureObjectIds)
        {
            if (existingGuids.Contains(objectId))
                continue;

            var user = await db.Users.FirstOrDefaultAsync(u => u.AzureObjectId == objectId.ToString(), cancellationToken);
            if (user == null)
                continue;

            addByUserId(user.Id);
            existingGuids.Add(objectId);
        }
    }

    private static User? GetJunctionUser<TRow>(TRow row) => row switch
    {
        ProjectSeniorResponsibleOfficer sro => sro.User,
        ProjectServiceOwner so => so.User,
        ProjectPmoContact pmo => pmo.User,
        _ => null
    };

    internal static void PopulateWorkItemContacts(WorkItem work, Project project)
    {
        work.Contacts.Clear();
        var seenUserRole = new HashSet<(int UserId, int RoleTypeId)>();

        foreach (var sro in project.SeniorResponsibleOfficers.OrderBy(x => x.Id))
        {
            if (sro.User == null) continue;
            work.Contacts.Add(ToWorkItemContact(SroJunctionIdBase - sro.Id, work.Id, 1, null, sro.User));
            seenUserRole.Add((sro.UserId, 1));
        }

        foreach (var so in project.ServiceOwners.OrderBy(x => x.Id))
        {
            if (so.User == null) continue;
            work.Contacts.Add(ToWorkItemContact(ServiceOwnerJunctionIdBase - so.Id, work.Id, 2, null, so.User));
            seenUserRole.Add((so.UserId, 2));
        }

        foreach (var pmo in project.PmoContacts.OrderBy(x => x.Id))
        {
            if (pmo.User == null) continue;
            work.Contacts.Add(ToWorkItemContact(PmoJunctionIdBase - pmo.Id, work.Id, 3, null, pmo.User));
            seenUserRole.Add((pmo.UserId, 3));
        }

        foreach (var pc in project.ProjectContacts.OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Id))
        {
            if (string.Equals(pc.TeamStatus, GovernanceTeamStatus, StringComparison.OrdinalIgnoreCase))
                continue;

            var roleId = StandardRoleToId.TryGetValue(pc.Role, out var rid) ? rid : 5;
            if (roleId is >= 1 and <= 4 && pc.UserId is int uid && seenUserRole.Contains((uid, roleId)))
                continue;

            work.Contacts.Add(new WorkItemContact
            {
                Id = pc.Id,
                WorkItemId = work.Id,
                ContactRoleTypeId = roleId,
                RoleName = roleId == 5 ? pc.Role : null,
                DisplayName = pc.Name ?? "",
                AppUser = pc.User
            });

            if (roleId is >= 1 and <= 4 && pc.UserId is int userId)
                seenUserRole.Add((userId, roleId));
        }

        foreach (var pc in project.ProjectContacts
                     .Where(pc => string.Equals(pc.TeamStatus, GovernanceTeamStatus, StringComparison.OrdinalIgnoreCase))
                     .OrderBy(pc => pc.SortOrder).ThenBy(pc => pc.Id))
        {
            work.Contacts.Add(new WorkItemContact
            {
                Id = pc.Id,
                WorkItemId = work.Id,
                ContactRoleTypeId = 5,
                RoleName = pc.Role,
                DisplayName = pc.Name ?? "",
                AppUser = pc.User
            });
        }
    }

    internal static async Task SyncJunctionOnAddContactAsync(
        CompassDbContext db,
        int projectId,
        int contactRoleTypeId,
        User user,
        CancellationToken cancellationToken)
    {
        if (contactRoleTypeId is < 1 or > 3)
            return;

        var project = await db.Projects
            .Include(p => p.SeniorResponsibleOfficers)
            .Include(p => p.ServiceOwners)
            .Include(p => p.PmoContacts)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);
        if (project == null)
            return;

        switch (contactRoleTypeId)
        {
            case 1 when project.SeniorResponsibleOfficers.All(s => s.UserId != user.Id):
                project.SeniorResponsibleOfficers.Add(new ProjectSeniorResponsibleOfficer
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
                break;
            case 2 when project.ServiceOwners.All(s => s.UserId != user.Id):
                project.ServiceOwners.Add(new ProjectServiceOwner
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
                break;
            case 3 when project.PmoContacts.All(s => s.UserId != user.Id):
                project.PmoContacts.Add(new ProjectPmoContact
                {
                    ProjectId = projectId,
                    UserId = user.Id,
                    CreatedAt = DateTime.UtcNow
                });
                break;
        }

        project.UpdatedAt = DateTime.UtcNow;
    }

    internal static async Task<bool> TryRemoveContactAsync(
        CompassDbContext db,
        int projectId,
        int contactId,
        CancellationToken cancellationToken)
    {
        if (contactId >= 0)
            return false;

        var project = await db.Projects
            .Include(p => p.SeniorResponsibleOfficers)
            .Include(p => p.ServiceOwners)
            .Include(p => p.PmoContacts)
            .FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);
        if (project == null)
            return true;

        if (contactId <= SroJunctionIdBase && contactId > ServiceOwnerJunctionIdBase)
        {
            var junctionId = SroJunctionIdBase - contactId;
            var row = project.SeniorResponsibleOfficers.FirstOrDefault(x => x.Id == junctionId);
            if (row != null)
                db.ProjectSeniorResponsibleOfficers.Remove(row);
        }
        else if (contactId <= ServiceOwnerJunctionIdBase && contactId > PmoJunctionIdBase)
        {
            var junctionId = ServiceOwnerJunctionIdBase - contactId;
            var row = project.ServiceOwners.FirstOrDefault(x => x.Id == junctionId);
            if (row != null)
                db.ProjectServiceOwners.Remove(row);
        }
        else if (contactId <= PmoJunctionIdBase)
        {
            var junctionId = PmoJunctionIdBase - contactId;
            var row = project.PmoContacts.FirstOrDefault(x => x.Id == junctionId);
            if (row != null)
                db.ProjectPmoContacts.Remove(row);
        }

        project.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static WorkItemContact ToWorkItemContact(int id, int workItemId, int roleTypeId, string? roleName, User user) =>
        new()
        {
            Id = id,
            WorkItemId = workItemId,
            ContactRoleTypeId = roleTypeId,
            RoleName = roleName,
            DisplayName = user.Name ?? user.Email ?? "—",
            AppUser = user
        };
}
