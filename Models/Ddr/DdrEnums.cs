namespace Compass.Models.Ddr;

/// <summary>
/// Controlled values for the Design Decision Records (DDR) feature.
/// Sourced from <c>compass/documentation/ddr.md</c> §8 (controlled values).
/// </summary>
public static class DdrControlledValues
{
    /// <summary>§8.1 Category — high-level discipline / area the decision relates to.</summary>
    public static readonly string[] Categories =
    {
        "Service design",
        "Interaction design",
        "Content design",
        "Accessibility",
        "Design system",
        "Research and evidence",
        "Data and reporting",
        "Product design",
        "Governance",
        "Decommissioning",
        "Other",
    };

    /// <summary>§8.2 Status — lifecycle of the recorded decision.</summary>
    public static readonly string[] Statuses =
    {
        "Draft",
        "Proposed",
        "In use",
        "Under review",
        "Approved",
        "Rejected",
        "Superseded",
        "Retired",
    };

    /// <summary>§8.3 Evidence types used on linked evidence rows.</summary>
    public static readonly string[] EvidenceTypes =
    {
        "User research",
        "Analytics",
        "Support data",
        "Accessibility testing",
        "Service assessment finding",
        "Benchmarking",
        "Policy",
        "Technical analysis",
        "Design crit",
        "Prototype testing",
        "Content review",
        "Performance data",
        "Security or data protection advice",
        "Prior art",
        "Other",
    };

    /// <summary>§8.4 Deviation type — required when <c>DeviationFlag</c> is true.</summary>
    public static readonly string[] DeviationTypes =
    {
        "Branding",
        "Component",
        "Pattern",
        "Style",
        "Accessibility",
        "Content",
        "Service standard",
        "Publishing route",
        "Technical",
        "Other",
    };

    /// <summary>§8.5 Insight classifications recorded by DesignOps reviewers.</summary>
    public static readonly string[] InsightClassifications =
    {
        "Reusable pattern opportunity",
        "DDT Manual guidance gap",
        "DfE Frontend component gap",
        "Accessibility support need",
        "Content guidance need",
        "Service design pattern need",
        "Interaction design pattern need",
        "Standards clarification needed",
        "Training or capability need",
        "Governance or assurance issue",
        "Potential Design History",
        "No wider action needed",
    };

    /// <summary>§8.6 Recommended follow-up actions DesignOps may attach to a record.</summary>
    public static readonly string[] RecommendedFollowUps =
    {
        "No action",
        "Share with another team",
        "Add to DDT Manual guidance backlog",
        "Add to DfE Frontend backlog",
        "Add to service patterns backlog",
        "Add to GOV.UK Design System backlog",
        "Add to standards backlog",
        "Add to assessor training backlog",
        "Add to community discussion",
        "Create Design History",
        "Escalate to Design and Run Board",
        "Review standard or policy",
        "Manage centrally by DesignOps",
    };

    /// <summary>Validity values used on retrospective records (§7.1 <c>current_validity</c>).</summary>
    public static readonly string[] CurrentValidityValues =
    {
        "Still valid",
        "Partially valid",
        "No longer valid",
        "Unknown",
    };

    /// <summary>Approval routes selectable when a deviation or high-risk category is recorded (§7.1).</summary>
    public static readonly string[] ApprovalRoutes =
    {
        "Service owner",
        "DesignOps lead",
        "Head of profession",
        "Design and Run Board",
        "Service assessment panel",
        "Other governance route",
    };

    /// <summary>Standard reference systems linked from <see cref="DdrStandardLink"/>.</summary>
    public static readonly string[] StandardTypes =
    {
        "GOV.UK Service Standard",
        "DDT standard",
        "Functional standard (GovS)",
        "WCAG 2.2 success criterion",
        "DfE design principle",
        "Other standard or policy",
    };

    /// <summary>Component / pattern source systems linked from <see cref="DdrComponentPatternLink"/>.</summary>
    public static readonly string[] ComponentSourceSystems =
    {
        "GOV.UK Frontend",
        "GOV.UK Service Manual",
        "DfE Frontend",
        "DfE Service Manual",
        "Other",
    };

    /// <summary>Comment categories on the DDR detail view (§7.4 <c>ddr_comment</c>).</summary>
    public static readonly string[] CommentTypes =
    {
        "Comment",
        "Observation",
        "Question",
        "Action",
    };

    /// <summary>Relationship types between DDRs (§7.4 <c>ddr_related_record</c>).</summary>
    public static readonly string[] RelationshipTypes =
    {
        "Related to",
        "Supersedes",
        "Superseded by",
        "Influences",
        "Influenced by",
    };
}
