using Compass.Models;
using Compass.Models.Modern.Work;

namespace Compass.Models.Fips;

public class FipsProductsViewModel
{
    public string ActiveTab { get; set; } = "all";

    public List<FipsProductRow> Products { get; set; } = new();

    public int AllProductsCount { get; set; }
    /// <summary>Total rows in <c>CMDBProducts</c> (cheap COUNT); helps diagnose empty lists vs filters.</summary>
    public int TotalProductsInDatabase { get; set; }
    public int MyProductsCount { get; set; }
    public int NewProductsCount { get; set; }
    /// <summary>Count of <see cref="CMDBProductStatus.Rejected"/> — excluded from the FIPS register (e.g. sync rules).</summary>
    public int InactiveProductsCount { get; set; }
    /// <summary>Count of <see cref="CMDBProductStatus.Inactive"/> — retired in Compass; CMDB sync skips these.</summary>
    public int RetiredCount { get; set; }

    /// <summary>CMDB sync is available on this list (Operations → Service register only).</summary>
    public bool CanSyncFromCmdb { get; set; }

    // Filter options
    public List<FipsBusinessArea> BusinessAreaOptions { get; set; } = new();
    public List<FipsChannel> ChannelOptions { get; set; } = new();
    public List<FipsUserGroup> UserGroupOptions { get; set; } = new();
    public List<FipsType> TypeOptions { get; set; } = new();
    public List<PhaseLookup> PhaseOptions { get; set; } = new();

    /// <summary>Active <see cref="BusinessAreaLookup"/> rows (for bulk checkboxes on the new products tab).</summary>
    public List<BusinessAreaLookup> BusinessAreaLookups { get; set; } = new();

    // Current filter values
    public string? Search { get; set; }
    public int? BusinessAreaId { get; set; }
    public int? ChannelId { get; set; }
    public int? UserGroupId { get; set; }
    public int? TypeId { get; set; }
    public int? PhaseId { get; set; }
}

public class FipsProductRow
{
    public Guid Id { get; set; }
    public int UniqueID { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? CMDBDescription { get; set; }
    public string? UserDescription { get; set; }
    public string? PhaseName { get; set; }
    public string? BusinessAreaDisplay { get; set; }
    /// <summary>Comma-separated type names (e.g. All products tab).</summary>
    public string? TypesDisplay { get; set; }
    /// <summary>Comma-separated channel names (e.g. All products tab).</summary>
    public string? ChannelsDisplay { get; set; }
    public int UserGroupCount { get; set; }
    public int ContactCount { get; set; }
    public string? ServiceOwner { get; set; }
    public string? ReportingContact { get; set; }
    public CMDBProductStatus Status { get; set; }
    public int QualityScore { get; set; }
    public int QualityScoreMax { get; set; } = 7;
}

public class FipsProductDetailViewModel
{
    public CMDBProduct Product { get; set; } = null!;
    public bool CanManage { get; set; }
    public string? CurrentUserEmail { get; set; }

    /// <summary>When <c>operations</c>, main nav highlights Operations → Service register (legacy; prefer <see cref="IsOperationsServiceRegisterProduct"/>).</summary>
    public string? NavContext { get; set; }

    /// <summary>Operations → Service register product page: full edit without contact checks, CMDB tools, separate from Manage FIPS.</summary>
    public bool IsOperationsServiceRegisterProduct { get; set; }

    /// <summary>Pretty-printed <see cref="CMDBProduct.LastCmdbSnapshotJson"/> for display; raw text if parsing fails.</summary>
    public string? CmdbSnapshotJsonFormatted { get; set; }

    /// <summary>True when the user is opening the edit form (otherwise read-only overview for managers).</summary>
    public bool EditMode { get; set; }

    /// <summary><c>information</c>, <c>history</c>, <c>risks</c>, or <c>issues</c>.</summary>
    public string ActiveDetailTab { get; set; } = "information";

    /// <summary>Resolved <see cref="Compass.Models.FipsService.ServiceId"/> when the CMDB product maps to the Service table.</summary>
    public int? ResolvedFipsServiceId { get; set; }

    public List<FipsProductRaidListItem> ProductRisks { get; set; } = new();

    public List<FipsProductRaidListItem> ProductIssues { get; set; } = new();

    public string BusinessAreasDisplay => string.Join(", ", Product.BusinessAreas.Select(ba => ba.FipsBusinessArea.Name));
    public string ChannelsDisplay => string.Join(", ", Product.Channels.Select(c => c.FipsChannel.Name));
    public string UserGroupsDisplay => string.Join(", ", Product.UserGroups.Select(ug => ug.FipsUserGroup.Name));
    public string TypesDisplay => string.Join(", ", Product.Types.Select(t => t.FipsType.Name));

    // Edit dropdowns (populated when CanManage = true)
    public List<PhaseLookup> PhaseOptions { get; set; } = new();
    /// <summary>Admin business-area lookups (same source as <c>/modern/admin</c> → Business areas).</summary>
    public List<BusinessAreaLookup> BusinessAreaLookupOptions { get; set; } = new();

    /// <summary>Set in edit mode: lookup IDs currently linked to the product (for checkbox state).</summary>
    public HashSet<int> SelectedBusinessAreaLookupIds { get; set; } = new();
    public List<FipsChannel> ChannelOptions { get; set; } = new();
    public List<FipsUserGroup> UserGroupOptions { get; set; } = new();
    public List<FipsType> TypeOptions { get; set; } = new();

    // Audit history
    public List<FipsAuditRow> AuditHistory { get; set; } = new();

    /// <summary>Read-only lines for custom categorisation groups (group label + selected item names).</summary>
    public List<FipsCategorisationSummaryLine> CategorisationSummaryLines { get; set; } = new();

    /// <summary>Edit checkboxes grouped by categorisation dimension (when <see cref="EditMode"/>).</summary>
    public List<FipsCategorisationGroupEditSection> CategorisationGroupSections { get; set; } = new();
}

public sealed record FipsCategorisationSummaryLine(string GroupName, string ItemsDisplay);

public sealed class FipsCategorisationGroupEditSection
{
    public int GroupId { get; init; }
    public string GroupName { get; init; } = "";
    public List<FipsCategorisationItemCheckboxOption> Items { get; init; } = new();
}

public sealed class FipsCategorisationItemCheckboxOption
{
    public int ItemId { get; init; }
    public string Name { get; init; } = "";
}

/// <summary>Risk or issue row on FIPS product RAID tabs (aligns with RAID register table columns).</summary>
public class FipsProductRaidListItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Status { get; set; }
    public int? Score { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsClosed { get; set; }

    public string? BusinessAreaLabel { get; set; }
    public string RelationKind { get; set; } = "Unknown";
    public int? RelationProjectId { get; set; }
    public string? RelationTarget { get; set; }
    public string? OwnerLabel { get; set; }
    public string? LikelihoodLabel { get; set; }
    public string? ImpactLabel { get; set; }
    public string? TierName { get; set; }
    public string? PriorityLabel { get; set; }
    public string? SeverityName { get; set; }
}

public class FipsAuditRow
{
    public DateTime ChangedAt { get; set; }
    public string? ChangedBy { get; set; }
    /// <summary>Audit log action, e.g. <c>update</c>.</summary>
    public string? ChangeType { get; set; }
    /// <summary>Field or area changed (maps to audit <c>EntityReference</c>).</summary>
    public string? FieldName { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
}

public class FipsAdminViewModel
{
    public string ActiveTab { get; set; } = "channels";

    public List<FipsChannel> Channels { get; set; } = new();
    public List<FipsType> Types { get; set; } = new();
    public List<FipsUserGroup> UserGroups { get; set; } = new();
    public List<FipsContactRole> ContactRoles { get; set; } = new();
    public List<FipsBusinessArea> BusinessAreas { get; set; } = new();
}
