namespace Compass.Services.ServiceSchema;

public sealed record ServiceSchemaLookupSeed(
    string Key,
    string Name,
    string Description,
    IReadOnlyList<(string Code, string Label)> Values);

public sealed class ServiceSchemaSectionDefinition
{
    public required string Key { get; init; }
    public required string Title { get; init; }
    public required string Help { get; init; }
    public required string DomainKey { get; init; }
    public string? LookupSetKey { get; init; }
    public string? LookupLabel { get; init; }
    public string? SecondaryLookupSetKey { get; init; }
    public string? SecondaryLookupLabel { get; init; }
    public string? CatalogueKind { get; init; }
    public bool CapturePerson { get; init; }
    public bool CaptureStaffRole { get; init; }
    public bool CaptureUrl { get; init; }
    public string TitleLabel { get; init; } = "Title";
    public string NarrativeLabel { get; init; } = "How this applies to this service";
}

public static class ServiceSchemaCatalog
{
    public const string FrameworkHomeUrl = "https://understand-digital-data-roles-skills.service.gov.uk/";

    public static IReadOnlyList<ServiceSchemaLookupSeed> Lookups { get; } =
    [
        Seed("entity_status", "Entity status", "Status of a local census record.",
            ("DRAFT", "Draft"), ("ACTIVE", "Active"), ("UNDER_REVIEW", "Under review"), ("SUPERSEDED", "Superseded"), ("RETIRED", "Retired")),
        Seed("service_type", "Service type", "How a service is classified. Not derived from the register type list.",
            ("EXTERNAL_FACING", "External facing"), ("INTERNAL", "Internal"), ("ENABLING", "Enabling"), ("SHARED", "Shared"), ("OPERATIONAL", "Operational"), ("OTHER", "Other")),
        Seed("service_product_relationship", "Service and product relationship", "How a register product relates to a census service.",
            ("DELIVERS", "Delivers"), ("SUPPORTS", "Supports"), ("USES", "Uses"), ("DEPENDS_ON", "Depends on"), ("PART_OF", "Part of"), ("REPLACES", "Replaces")),
        Seed("verification_status", "Verification status", "Whether an assertion has been checked.",
            ("UNVERIFIED", "Unverified"), ("VERIFIED", "Verified"), ("DISPUTED", "Disputed"), ("SUPERSEDED", "Superseded")),
        Seed("missing_information_status", "Missing information", "Section-level declaration. An empty list is not the same as confirmed none.",
            ("NOT_COLLECTED", "Not yet collected"), ("UNKNOWN_AFTER_REVIEW", "Unknown after review"), ("NOT_APPLICABLE", "Not applicable"), ("CONFIRMED_NONE", "Confirmed none"), ("SOURCE_UNAVAILABLE", "Source unavailable")),
        Seed("user_group_type", "User group type", "Shared classification for user groups.",
            ("INDIVIDUAL", "Individual"), ("EDUCATION_PROVIDER", "Education provider"), ("EDUCATION_WORKFORCE", "Education workforce"), ("LOCAL_AUTHORITY", "Local authority"), ("EMPLOYER", "Employer"), ("PARTNER_ORG", "Partner organisation"), ("DFE_STAFF", "DfE staff"), ("GOVERNMENT_ORG", "Government organisation"), ("OTHER", "Other")),
        Seed("user_relationship_type", "User relationship", "How a user group relates to this service.",
            ("DIRECT_USER", "Direct user"), ("SERVICE_RECIPIENT", "Service recipient"), ("BENEFICIARY", "Beneficiary"), ("REPRESENTATIVE", "Representative"), ("INTERMEDIARY", "Intermediary"), ("INFORMATION_PROVIDER", "Information provider"), ("OPERATOR", "Operator"), ("DECISION_MAKER", "Decision maker"), ("APPROVER", "Approver"), ("SUPPORT_USER", "Support user"), ("INFORMATION_CONSUMER", "Information consumer"), ("ASSURER", "Assurer"), ("OTHER", "Other")),
        Seed("need_type", "Need type", "Classification of a user need, not a prescribed solution.",
            ("INFORMATION", "Information"), ("TRANSACTION", "Transaction"), ("DECISION", "Decision"), ("EVIDENCE", "Evidence"), ("SUPPORT", "Support"), ("COMMUNICATION", "Communication"), ("ACCESS", "Access"), ("MANAGEMENT", "Management"), ("COMPLIANCE", "Compliance"), ("OPERATIONAL", "Operational"), ("OTHER", "Other")),
        Seed("need_criticality", "Need criticality", "Consequence if the need is unmet.",
            ("ESSENTIAL", "Essential"), ("HIGH", "High"), ("MEDIUM", "Medium"), ("LOW", "Low"), ("UNKNOWN", "Unknown")),
        Seed("need_validation_status", "Need validation", "Whether a need has been tested.",
            ("ASSUMED", "Assumed"), ("RESEARCHED", "Researched"), ("VALIDATED", "Validated"), ("DISPUTED", "Disputed"), ("SUPERSEDED", "Superseded")),
        Seed("journey_type", "Journey type", "A journey can cross more than one service.",
            ("END_TO_END", "End to end"), ("SERVICE", "Service"), ("TRANSACTION", "Transaction"), ("OPERATIONAL", "Operational"), ("INTERNAL", "Internal"), ("SUPPORT", "Support"), ("OTHER", "Other")),
        Seed("need_fulfilment_status", "Need fulfilment", "Assessment of how well a need is met. Verified assessments need a narrative.",
            ("FULLY_MET", "Fully met"), ("MOSTLY_MET", "Mostly met"), ("PARTIALLY_MET", "Partially met"), ("POORLY_MET", "Poorly met"), ("NOT_MET", "Not met"), ("NOT_ASSESSED", "Not assessed")),
        Seed("user_evidence_type", "User evidence type", "An assumption must not be presented as validated research.",
            ("USER_RESEARCH", "User research"), ("SERVICE_DATA", "Service data"), ("ANALYTICS", "Analytics"), ("SURVEY", "Survey"), ("FEEDBACK", "Feedback"), ("SUPPORT_DATA", "Support data"), ("COMPLAINTS", "Complaints"), ("OBSERVATION", "Observation"), ("EXTERNAL_RESEARCH", "External research"), ("POLICY_EVIDENCE", "Policy evidence"), ("ASSUMPTION", "Assumption"), ("OTHER", "Other")),
        Seed("feature_type", "Feature type", "Classification of a feature.",
            ("INTERACTION", "Interaction"), ("WORKFLOW", "Workflow"), ("DATA", "Data"), ("INTEGRATION", "Integration"), ("OPERATIONAL", "Operational"), ("OTHER", "Other")),
        Seed("reuse_status", "Reuse status", "A candidate is not approval to reuse.",
            ("NOT_ASSESSED", "Not assessed"), ("CANDIDATE", "Candidate"), ("AVAILABLE", "Available"), ("RESTRICTED", "Restricted"), ("NOT_SUITABLE", "Not suitable")),
        Seed("dependency_type", "Dependency type", "Kind of dependency.",
            ("TECHNICAL", "Technical"), ("DATA", "Data"), ("OPERATIONAL", "Operational"), ("ORGANISATIONAL", "Organisational"), ("PROCESS", "Process"), ("SUPPLIER", "Supplier"), ("OTHER", "Other")),
        Seed("finding_type", "Finding type", "Current-state observation.",
            ("PROBLEM", "Problem"), ("GAP", "Gap"), ("DUPLICATION", "Duplication"), ("RISK", "Risk"), ("UNMET_NEED", "Unmet need"), ("GOOD_PRACTICE", "Good practice"), ("OTHER", "Other")),
        Seed("intervention_type", "Intervention type", "A proposal, distinct from an approved decision.",
            ("REUSE", "Reuse"), ("IMPROVE", "Improve"), ("INTEGRATE", "Integrate"), ("CONSOLIDATE", "Consolidate"), ("STANDARDISE", "Standardise"), ("AUTOMATE", "Automate"), ("BUILD", "Build"), ("BUY", "Buy"), ("REPLACE", "Replace"), ("RETIRE", "Retire"), ("STOP", "Stop")),
        Seed("resource_type", "Resource type", "Evidence and guidance linked from an authoritative location.",
            ("EVIDENCE", "Evidence"), ("GUIDANCE", "Guidance"), ("STANDARD", "Standard"), ("RESEARCH", "Research"), ("BUSINESS_CASE", "Business case"), ("REPORT", "Report"), ("DATA_MODEL", "Data model"), ("ARCHITECTURE", "Architecture"), ("PROCESS_MAP", "Process map"), ("DECISION", "Decision"), ("OTHER", "Other")),
        Seed("delivery_model", "Delivery model", "Who organises and carries out delivery or change.",
            ("IN_HOUSE", "In house"), ("SUPPLIER_LED", "Supplier led"), ("JOINT_DFE_SUPPLIER", "Joint DfE and supplier"), ("CROSS_GOVERNMENT", "Cross government"), ("PARTNER_LED", "Partner led"), ("OTHER", "Other"), ("UNKNOWN", "Unknown")),
        Seed("delivery_approach", "Delivery approach", "How development or change is carried out.",
            ("AGILE", "Agile"), ("WATERFALL", "Waterfall"), ("HYBRID", "Hybrid"), ("CONTINUOUS", "Continuous"), ("OTHER", "Other"), ("UNKNOWN", "Unknown")),
        Seed("lifecycle_arrangement", "Lifecycle arrangement", "Who retains responsibility across build, transition and run.",
            ("PERSISTENT_PRODUCT_TEAM", "Persistent product team"), ("DESIGN_BUILD_RUN", "Design, build, run"), ("BUILD_HANDOVER", "Build then handover"), ("BUILD_TRANSFER", "Build then transfer"), ("RUN_IMPROVE", "Run and improve"), ("PROJECT_BASED", "Project based"), ("SHARED_PLATFORM_TEAM", "Shared platform team"), ("OTHER", "Other"), ("UNKNOWN", "Unknown")),
        Seed("support_model", "Support model", "How operational support is organised. Distinct from the delivery model.",
            ("PRODUCT_TEAM", "Product team"), ("DEDICATED_INTERNAL", "Dedicated internal"), ("SHARED_INTERNAL", "Shared internal"), ("SUPPLIER_MANAGED", "Supplier managed"), ("HYBRID", "Hybrid"), ("CROSS_GOVERNMENT", "Cross government"), ("PARTNER_PROVIDED", "Partner provided"), ("UNDEFINED", "No formal support model"), ("UNKNOWN", "Unknown")),
        Seed("support_function", "Support function", "What a support arrangement covers.",
            ("USER_SUPPORT", "User support"), ("SERVICE_DESK", "Service desk"), ("APPLICATION", "Application"), ("TECHNICAL", "Technical"), ("INFRASTRUCTURE", "Infrastructure"), ("PLATFORM", "Platform"), ("DATA", "Data"), ("SECURITY", "Security"), ("OPERATIONS", "Operations"), ("MAINTENANCE", "Maintenance")),
        Seed("support_level", "Support level", "Tier of support.",
            ("L1", "L1"), ("L2", "L2"), ("L3", "L3"), ("SPECIALIST", "Specialist"), ("NOT_TIERED", "Not tiered")),
        Seed("support_hours_type", "Support hours", "When support is available.",
            ("BUSINESS_HOURS", "Business hours"), ("EXTENDED_HOURS", "Extended hours"), ("24_7", "24 hours"), ("ON_CALL", "On call"), ("BEST_EFFORTS", "Best efforts"), ("NO_FORMAL_SUPPORT", "No formal support"), ("UNKNOWN", "Unknown")),
        Seed("relationship_type", "Relationship type", "Cross-domain relationship.",
            ("SUPPORTS", "Supports"), ("CONTRIBUTES_TO", "Contributes to"), ("ENABLES", "Enables"), ("IMPLEMENTS", "Implements"), ("DELIVERS", "Delivers"), ("DEPENDS_ON", "Depends on"), ("SUPERSEDES", "Supersedes"))
    ];

    public static IReadOnlyList<ServiceSchemaSectionDefinition> Sections { get; } =
    [
        Section("outcomes", "Outcomes", "Intended change this service contributes to.", "outcome", titleLabel: "Outcome", narrativeLabel: "How this service contributes"),
        Section("benefits", "Benefits", "Expected value of this service.", "benefit", titleLabel: "Benefit", narrativeLabel: "Who benefits and what is expected to change"),
        Section("user-groups", "User groups", "Who this service is for, and the relationship.", "user_group", "user_relationship_type", "Relationship", catalogue: "user_group", titleLabel: "User group", narrativeLabel: "How this group relates to the service"),
        Section("user-needs", "User needs", "Needs met or intended to be met, expressed independently of a solution.", "user_need", "need_type", "Need type", "need_criticality", "Criticality if unmet", catalogue: "user_need", titleLabel: "Need", narrativeLabel: "How this service supports the need"),
        Section("journeys", "Journeys", "End-to-end journeys this service supports. A journey can cross several services.", "journey", "journey_type", "Journey type", catalogue: "journey", titleLabel: "Journey", narrativeLabel: "Which part of the journey this service supports"),
        Section("fulfilment", "Unmet or poorly met needs", "Assessment of how well a need is met. Mark verified only with a narrative.", "need_fulfilment", "need_fulfilment_status", "Fulfilment", catalogue: "user_need", titleLabel: "Need", narrativeLabel: "Assessment and evidence"),
        Section("responsibilities", "Responsibilities", "People named on the service register, and any further responsibilities.", "responsibility", capturePerson: true, captureStaffRole: true, titleLabel: "Responsibility", narrativeLabel: "What this person is accountable for"),
        Section("capabilities", "Capabilities", "Capabilities this service provides, supports or depends on.", "capability", "relationship_type", "Relationship", titleLabel: "Capability", narrativeLabel: "How the capability is used"),
        Section("features", "Features", "Functionality, including whether it is custom, configured or reused.", "feature", "feature_type", "Feature type", "reuse_status", "Reuse", catalogue: "feature", titleLabel: "Feature", narrativeLabel: "How it is implemented on this service"),
        Section("technology", "Technology", "Technology and platforms this service uses.", "technology", titleLabel: "Technology or platform", narrativeLabel: "How it is used", url: true),
        Section("dependencies", "Dependencies", "What this service depends on, and how critical that is.", "dependency", "dependency_type", "Dependency type", titleLabel: "Dependency", narrativeLabel: "Why it matters"),
        Section("data", "Data and information", "Information assets this service creates, reads, updates or publishes.", "data", "relationship_type", "Use", titleLabel: "Information asset", narrativeLabel: "How the data is used", url: true),
        Section("measures", "Measures", "Measures for this service.", "measure", titleLabel: "Measure", narrativeLabel: "What this measure tells us", url: true),
        Section("assurance", "Assurance", "Assurance activity for this service.", "assurance", titleLabel: "Assurance activity", narrativeLabel: "What it covers", url: true),
        Section("risks", "Risks and issues", "Risks and issues for this service.", "risk", titleLabel: "Risk or issue", narrativeLabel: "How it relates to this service", url: true),
        Section("findings", "Findings", "Current-state observations, with evidence.", "finding", "finding_type", "Finding type", titleLabel: "Finding", narrativeLabel: "Consequence and evidence"),
        Section("opportunities", "Opportunities", "Proposed improvements.", "opportunity", "intervention_type", "Proposed intervention", titleLabel: "Opportunity", narrativeLabel: "Outcome sought"),
        Section("resources", "Evidence and resources", "Evidence, guidance or research for this service.", "resource", "resource_type", "Resource type", "user_evidence_type", "Evidence type", titleLabel: "Resource", narrativeLabel: "What this resource supports", url: true),
        Section("delivery", "Delivery and support", "Who delivers change, and how support is organised. These are separate.", "delivery", "delivery_model", "Delivery model", "support_model", "Support model", titleLabel: "Arrangement", narrativeLabel: "What is in place"),
        Section("support", "Support arrangements", "Repeatable support responsibilities: function, level and hours.", "support", "support_function", "Function", "support_level", "Level", titleLabel: "Support arrangement", narrativeLabel: "Hours, escalation and who provides it")
    ];

    public static IReadOnlyList<(string Kind, string Label, string LookupSetKey)> CatalogueKinds { get; } =
    [
        ("pattern", "Patterns, components and APIs", ""),
        ("technology", "Technologies and components", ""),
        ("stack", "Stacks and platforms", ""),
        ("integration", "Integrations", ""),
        ("dependency", "Dependencies", ""),
        ("user_group", "User groups", "user_group_type"),
        ("user_need", "User needs", "need_type"),
        ("journey", "Journeys", "journey_type"),
        ("feature", "Features", "feature_type")
    ];

    public static ServiceSchemaSectionDefinition? FindSection(string? key) =>
        Sections.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase));

    public static IReadOnlyList<string> DomainsUsingLookup(string lookupKey) =>
        Sections.Where(s =>
                string.Equals(s.LookupSetKey, lookupKey, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(s.SecondaryLookupSetKey, lookupKey, StringComparison.OrdinalIgnoreCase))
            .Select(s => s.DomainKey)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static ServiceSchemaLookupSeed Seed(string key, string name, string description, params (string Code, string Label)[] values) =>
        new(key, name, description, values);

    private static ServiceSchemaSectionDefinition Section(
        string key,
        string title,
        string help,
        string domain,
        string? lookup = null,
        string? lookupLabel = null,
        string? secondary = null,
        string? secondaryLabel = null,
        string? catalogue = null,
        bool capturePerson = false,
        bool captureStaffRole = false,
        bool url = false,
        string titleLabel = "Title",
        string narrativeLabel = "How this applies to this service") =>
        new()
        {
            Key = key,
            Title = title,
            Help = help,
            DomainKey = domain,
            LookupSetKey = lookup,
            LookupLabel = lookupLabel,
            SecondaryLookupSetKey = secondary,
            SecondaryLookupLabel = secondaryLabel,
            CatalogueKind = catalogue,
            CapturePerson = capturePerson,
            CaptureStaffRole = captureStaffRole,
            CaptureUrl = url,
            TitleLabel = titleLabel,
            NarrativeLabel = narrativeLabel
        };
}
