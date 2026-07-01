using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.ViewModels.Modern;

/// <summary>Create/edit screen for modern admin lookup panels (card + GOV.UK form, one field per row).</summary>
public class AdminLookupEditorViewModel
{
    public string Panel { get; set; } = "";

    /// <summary>Human-readable heading (e.g. "Edit business area").</summary>
    public string PageHeading { get; set; } = "";

    public bool IsCreate { get; set; }

    public int? Id { get; set; }

    /// <summary>Display name or RAID label.</summary>
    public string Name { get; set; } = "";

    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;

    public string? GuidanceUrl { get; set; }

    public string? Summary { get; set; }

    public string? CssClass { get; set; }

    /// <summary>RAID-style code (risk categories, issue categories).</summary>
    public string? Code { get; set; }

    /// <summary>Risk tier / action source summary line.</summary>
    public string? DetailSummary { get; set; }

    /// <summary>Resource band minimum (inclusive) FTE.</summary>
    public decimal? MinFte { get; set; }

    /// <summary>Resource band maximum (inclusive) FTE. Null means open ended.</summary>
    public decimal? MaxFte { get; set; }

    /// <summary>Organizational group parent (directorate) for portfolios.</summary>
    public int? ParentGroupId { get; set; }

    public List<SelectListItem> ParentGroupOptions { get; set; } = new();

    /// <summary>Mission pillar (optional) for priority outcomes.</summary>
    public int? MissionId { get; set; }

    public List<SelectListItem> MissionOptions { get; set; } = new();

    /// <summary>Theme text for priority outcomes.</summary>
    public string? Theme { get; set; }

    /// <summary>Status (active, proposed, paused, completed, cancelled) for priority outcomes.</summary>
    public string? Status { get; set; }

    /// <summary>Owner user id for priority outcomes.</summary>
    public int? OwnerUserId { get; set; }
    public string? OwnerName { get; set; }
    public string? OwnerEmail { get; set; }

    /// <summary>Theme SRO user id for priority outcomes.</summary>
    public int? ThemeSroUserId { get; set; }
    public string? ThemeSroName { get; set; }
    public string? ThemeSroEmail { get; set; }

    /// <summary>Outcome SRO user id for priority outcomes.</summary>
    public int? OutcomeSroUserId { get; set; }
    public string? OutcomeSroName { get; set; }
    public string? OutcomeSroEmail { get; set; }

    /// <summary>Format (read-only for departments, e.g. "Ministerial department").</summary>
    public string? Format { get; set; }

    /// <summary>GOV.UK web URL for departments.</summary>
    public string? WebUrl { get; set; }

    /// <summary>GOV.UK status for departments (live, exempt, etc).</summary>
    public string? GovukStatus { get; set; }

    /// <summary>Last synced timestamp for departments.</summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>Parent department ID for departments.</summary>
    public int? ParentDepartmentId { get; set; }

    public List<SelectListItem> ParentDepartmentOptions { get; set; } = new();

    /// <summary>Parent category for standard sub-categories.</summary>
    public int? CategoryId { get; set; }

    public List<SelectListItem> CategoryOptions { get; set; } = new();

    /// <summary>Which form fields to show (drives the editor partial).</summary>
    public AdminLookupEditorKind EditorKind { get; set; } = AdminLookupEditorKind.Simple;

    /// <summary>Risk likelihood / impact matrix weight (1–5) when editing those RAID lookups.</summary>
    public int? RaidMatrixScore { get; set; }

    /// <summary>Risk tier: 1 = highest governance (Tier 1), larger = lower. 0 = infer from sort order among non-proposed tiers.</summary>
    public int RiskTierGovernanceLevel { get; set; }

    /// <summary>Risk tier: marks rows used only as escalation targets (not assignable on risks).</summary>
    public bool RiskTierIsProposedTier { get; set; }
}

public enum AdminLookupEditorKind
{
    Simple,
    UniversalBarrier,
    Priority,
    Rag,
    Raid,
    RiskTierLike,
    ActivityType,
    WorkItemTag,
    ResourceBand,
    MissionPillar,
    PriorityOutcome,
    Portfolio,
    Department,
    StandardSubCategory,
}

/// <summary>5-column admin lookup list (Name, Description, Order, Active, Actions).</summary>
public class AdminLookupTable5ColModel
{
    public List<AdminLookupRow> Rows { get; set; } = new();
    public string Panel { get; set; } = "";
    public string Heading { get; set; } = "";
    public string? IntroHtml { get; set; }
    public string AddButtonLabel { get; set; } = "Add";
    /// <summary>When set, description cell also shows code (RAID / tiers).</summary>
    public bool ShowCodeInDescription { get; set; }
}

/// <summary>FIPS simple lookup panel (Name, Description, Order, Status + inline add + toggle).</summary>
public class AdminFipsSimplePanelModel
{
    public string Heading { get; set; } = "";
    public string? IntroHtml { get; set; }
    public List<AdminLookupRow> Rows { get; set; } = new();
    public string AddActionUrl { get; set; } = "";
    public string ToggleActionName { get; set; } = "";
    public string EntityLabel { get; set; } = "item";
}

/// <summary>FIPS business areas: read-only mirror of Admin → Business areas.</summary>
public class AdminFipsBusinessAreasSyncedPanelModel
{
    public List<AdminLookupRow> Rows { get; set; } = new();
    public string MasterBusinessAreasUrl { get; set; } = "";
}
