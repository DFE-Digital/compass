using System.ComponentModel.DataAnnotations;
using Compass.Models.Ddr;

namespace Compass.ViewModels.Modern.Ddr;

/// <summary>Sentinel for the Register deviation-type filter when a record is flagged but has no type.</summary>
public static class DdrRegisterQueryValues
{
    public const string UnsetDeviationType = "_unset";
}

/// <summary>Top-level register / index page (search, filters, results).</summary>
public class DdrRegisterViewModel
{
    public string? Search { get; set; }
    public string? Category { get; set; }
    public string? Status { get; set; }
    public bool? Deviation { get; set; }
    /// <summary>Optional deviation subtype filter (requires deviation; uses <see cref="DdrRegisterQueryValues.UnsetDeviationType"/> for flagged records with no type).</summary>
    public string? DeviationType { get; set; }
    public bool? RetrospectiveOnly { get; set; }
    public bool? ReviewOverdue { get; set; }

    public int TotalCount { get; set; }
    public int FilteredCount { get; set; }

    public List<DdrRegisterRow> Rows { get; set; } = new();

    /// <summary>Pre-filter context. <c>null</c> = full register.</summary>
    public Guid? ProductIdFilter { get; set; }
    public string? ProductTitle { get; set; }
    public int? WorkItemIdFilter { get; set; }
    public string? WorkItemTitle { get; set; }

    public bool CanCreate { get; set; }
}

public class DdrRegisterRow
{
    public int Id { get; set; }
    public string Reference { get; set; } = string.Empty;
    public string ShortTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public bool DeviationFlag { get; set; }
    public DateTime? ReviewDate { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? AuthorDisplayName { get; set; }
    public List<string> ProductTitles { get; set; } = new();
    public List<string> WorkItemTitles { get; set; } = new();

    /// <summary>True when status is in-use / proposed / approved AND review date is in the past.</summary>
    public bool ReviewOverdue { get; set; }
}

/// <summary>DDR detail view.</summary>
public class DdrDetailViewModel
{
    public DesignDecisionRecord Record { get; set; } = default!;

    public List<DdrAlternative> Alternatives { get; set; } = new();
    public List<DdrEvidence> Evidence { get; set; } = new();
    public List<DdrStandardLink> Standards { get; set; } = new();
    public List<DdrComponentPatternLink> ComponentsAndPatterns { get; set; } = new();
    public List<DdrRelatedRecord> Related { get; set; } = new();
    public List<DdrComment> Comments { get; set; } = new();
    public List<DdrInsightClassification> InsightClassifications { get; set; } = new();
    public List<DdrRecommendedFollowUp> RecommendedFollowUps { get; set; } = new();
    public List<DdrGitHubIssueLink> GitHubIssueLinks { get; set; } = new();
    public List<DdrAuditEvent> AuditEvents { get; set; } = new();

    public List<LinkedProductRow> LinkedProducts { get; set; } = new();
    public List<LinkedWorkItemRow> LinkedWorkItems { get; set; } = new();

    public bool CanEdit { get; set; }
    public bool CanReview { get; set; }
    public bool ReviewOverdue { get; set; }

    public class LinkedProductRow
    {
        public Guid ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class LinkedWorkItemRow
    {
        public int WorkItemId { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}

/// <summary>Single-page create form. The spec describes a wizard; MVP uses one page split into named sections to keep the form manageable while still enforcing all validation rules in §11.</summary>
public class DdrCreateViewModel
{
    [Required(ErrorMessage = "Select a category.")]
    public string? Category { get; set; }

    /// <summary>Optional pre-filled product link from the product page entry point.</summary>
    public Guid? PreFilledProductId { get; set; }

    /// <summary>Optional pre-filled work item link from the work item page entry point.</summary>
    public int? PreFilledWorkItemId { get; set; }

    public string? PreFilledProductTitle { get; set; }
    public string? PreFilledWorkItemTitle { get; set; }

    public List<Guid> LinkedProductIds { get; set; } = new();
    public List<int> LinkedWorkItemIds { get; set; } = new();

    [Required(ErrorMessage = "Enter a short title.")]
    [StringLength(120, MinimumLength = 10, ErrorMessage = "Short title must be between 10 and 120 characters.")]
    public string? ShortTitle { get; set; }

    [Required(ErrorMessage = "Enter the context or problem statement.")]
    [StringLength(4000, MinimumLength = 50, ErrorMessage = "Context must be between 50 and 4,000 characters.")]
    public string? ContextProblemStatement { get; set; }

    [Required(ErrorMessage = "Enter the decision.")]
    [StringLength(3000, MinimumLength = 50, ErrorMessage = "Decision must be between 50 and 3,000 characters.")]
    public string? Decision { get; set; }

    [Required(ErrorMessage = "Enter the rationale.")]
    [StringLength(4000, MinimumLength = 50, ErrorMessage = "Rationale must be between 50 and 4,000 characters.")]
    public string? Rationale { get; set; }

    [Required(ErrorMessage = "Enter the consequences and trade-offs.")]
    [StringLength(3000, MinimumLength = 30, ErrorMessage = "Consequences and trade-offs must be between 30 and 3,000 characters.")]
    public string? ConsequencesTradeoffs { get; set; }

    /// <summary>Multi-line text — one alternative per line. Validated as 1+ entries server-side.</summary>
    [StringLength(4000, ErrorMessage = "Alternatives must be 4,000 characters or fewer.")]
    public string? AlternativesText { get; set; }

    /// <summary>Optional URL list (newline separated) of supporting evidence.</summary>
    [StringLength(4000, ErrorMessage = "Evidence must be 4,000 characters or fewer.")]
    public string? EvidenceText { get; set; }

    /// <summary>Optional standards reference list.</summary>
    [StringLength(4000, ErrorMessage = "Standards text must be 4,000 characters or fewer.")]
    public string? StandardsText { get; set; }

    public bool DeviationFlag { get; set; }
    public string? DeviationType { get; set; }

    [StringLength(4000, ErrorMessage = "Deviation details must be 4,000 characters or fewer.")]
    public string? DeviationDetails { get; set; }
    public string? ApprovalRoute { get; set; }
    public string? ApprovedBy { get; set; }

    [Required(ErrorMessage = "Select a status.")]
    public string? Status { get; set; } = "Draft";

    [Required(ErrorMessage = "Enter when this decision should be reviewed.")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Review trigger must be between 10 and 500 characters.")]
    public string? ReviewTrigger { get; set; }

    public DateTime? ReviewDate { get; set; }

    /// <summary>GOV.UK date input parts for <see cref="ReviewDate"/> (POST + draft reload).</summary>
    public int? ReviewDateDay { get; set; }
    public int? ReviewDateMonth { get; set; }
    public int? ReviewDateYear { get; set; }

    public bool RetrospectiveRecord { get; set; }
    public string? OriginalDecisionDate { get; set; }

    [StringLength(4000, ErrorMessage = "Retrospective context must be 4,000 characters or fewer.")]
    public string? RetrospectiveContext { get; set; }

    public string? CurrentValidity { get; set; }

    [StringLength(4000, ErrorMessage = "Validity rationale must be 4,000 characters or fewer.")]
    public string? CurrentValidityRationale { get; set; }

    [StringLength(1000, ErrorMessage = "Message to DesignOps must be 1,000 characters or fewer.")]
    public string? MessageToDesignOps { get; set; }

    /// <summary>Posted action — "draft" or "submit".</summary>
    public string? Action { get; set; }

    /// <summary>When set, POST updates this draft reference instead of creating a new record.</summary>
    public string? EditingReference { get; set; }

    // Static option lists exposed to the view to avoid threading via ViewBag.
    public static IReadOnlyList<string> CategoryOptions => DdrControlledValues.Categories;
    public static IReadOnlyList<string> StatusOptions => DdrControlledValues.Statuses;
    public static IReadOnlyList<string> DeviationTypeOptions => DdrControlledValues.DeviationTypes;
    public static IReadOnlyList<string> ApprovalRouteOptions => DdrControlledValues.ApprovalRoutes;
    public static IReadOnlyList<string> CurrentValidityOptions => DdrControlledValues.CurrentValidityValues;

    /// <summary>Available products for the picker (CMDBProduct.Id + title).</summary>
    public List<ProductOption> ProductOptions { get; set; } = new();

    /// <summary>Available work items for the picker.</summary>
    public List<WorkItemOption> WorkItemOptions { get; set; } = new();

    public class ProductOption
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }

    public class WorkItemOption
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
    }
}

/// <summary>DesignOps oversight queue (§10.6).</summary>
public class DdrOversightViewModel
{
    public string ActiveFilter { get; set; } = "new";
    public List<DdrRegisterRow> Rows { get; set; } = new();
    public Dictionary<string, int> Counts { get; set; } = new();
}

/// <summary>Lightweight panel embedded on FIPS product / work item detail views.</summary>
public class DdrEmbeddedPanelViewModel
{
    /// <summary>"product" or "workitem".</summary>
    public string ContextKind { get; set; } = "product";
    public Guid? ProductId { get; set; }
    public int? WorkItemId { get; set; }

    public int LinkedCount { get; set; }
    public int DeviationCount { get; set; }
    public int OverdueReviewCount { get; set; }
    public Dictionary<string, int> CountsByStatus { get; set; } = new();
    public List<DdrRegisterRow> RecentRecords { get; set; } = new();

    public string CreateUrl { get; set; } = "#";
    public string RegisterUrl { get; set; } = "#";
    public string? ExportUrl { get; set; }
}

/// <summary>Breakdown row linking to the filtered register.</summary>
public sealed class DdrDashboardBreakdownRow
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public string RegisterUrl { get; set; } = "#";
}

/// <summary>DDR dashboard — counts by category and deviation type.</summary>
public sealed class DdrDashboardViewModel
{
    public int TotalDdrs { get; set; }
    public IReadOnlyList<DdrDashboardBreakdownRow> ByCategory { get; set; } = Array.Empty<DdrDashboardBreakdownRow>();
    public IReadOnlyList<DdrDashboardBreakdownRow> ByDeviationType { get; set; } = Array.Empty<DdrDashboardBreakdownRow>();
}

/// <summary>Reporting page — required at <c>/modern/reporting/design-decision-records</c> (§14).</summary>
public class DdrReportingViewModel
{
    public int TotalDdrs { get; set; }
    public int DdrsThisMonth { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
    public Dictionary<string, int> ByCategory { get; set; } = new();
    public int ProductsWithDdrs { get; set; }
    public int TotalProducts { get; set; }
    public int DeviationsCount { get; set; }
    public int OverdueReviewCount { get; set; }
    public int RetrospectiveCount { get; set; }
    public Dictionary<string, int> DeviationsByType { get; set; } = new();
    public Dictionary<string, int> InsightCounts { get; set; } = new();
}
