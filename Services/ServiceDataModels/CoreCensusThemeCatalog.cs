using Compass.Models.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// In-memory definitions for core census themes from the DfE service/product census schema
/// (service-schema-db RecordAreas + HelpCatalog domain sections / Domain 3 field schema).
/// </summary>
public static class CoreCensusThemeCatalog
{
    public const string ServiceOfferingKey = "source";

    public static readonly IReadOnlyList<(string ValueKey, string Label)> MissingInformationStatusOptions =
    [
        ("NOT_COLLECTED", "Not yet collected"),
        ("UNKNOWN_AFTER_REVIEW", "Unknown after review"),
        ("NOT_APPLICABLE", "Not applicable"),
        ("CONFIRMED_NONE", "Confirmed none"),
        ("SOURCE_UNAVAILABLE", "Source unavailable")
    ];

    public const string SectionInformationStatusGuidance =
        "Only complete this section if none of the questions can be answered, or there is no evidence to provide.";

    public static IReadOnlyList<CoreThemeDefinition> All { get; } = BuildAll();

    public static IReadOnlyList<CoreThemeDefinition> Importable =>
        All.Where(t => !t.IsServiceOffering).ToList();

    public static CoreThemeDefinition? Get(string stableKey) =>
        All.FirstOrDefault(t => string.Equals(t.StableKey, stableKey, StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<CoreThemeDefinition> BuildAll()
    {
        var themes = new List<CoreThemeDefinition>
        {
            // Excluded from import: COMPASS product/service offering identity is on the Service Register.
            Theme(ServiceOfferingKey, "Service offering", 0, isServiceOffering: true,
                "Product identity stays on the Service Register. Name, description and URL are not duplicated as census questions.",
                Field("product-name", "Service / product name", ServiceDataModelFieldType.Text, "TEXT",
                    "Canonical name from the Service Register — do not capture a competing value.", mandatory: false, sort: 1),
                Field("product-description", "Service description", ServiceDataModelFieldType.MultilineText, "TEXTAREA",
                    "Canonical description from the Service Register.", sort: 2),
                Field("product-url", "Public URL", ServiceDataModelFieldType.Url, "URL",
                    "Canonical URL from the Service Register.", sort: 3)),

            Theme("estate", "Service estate", 10,
                "How this service relates to other services and service lines.",
                Field("linked-services", "Linked services", ServiceDataModelFieldType.Services, "COLLECTION",
                    "Search and add services from the Service Register. You can add several and remove one. Saving stores stable register ids.", sort: 1),
                Field("linked-service-lines", "Linked service lines", ServiceDataModelFieldType.ServiceLines, "COLLECTION",
                    "Search and add service lines from the register. You can add several and remove one. Saving stores stable service-line ids.", sort: 2),
                SectionStatus("estate", 3)),

            Theme("purpose", "Purpose and value", 20,
                "Outcomes, benefits and the intended change this service supports.",
                Field("outcomes", "Outcomes", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Intended change. A link here is not proof the outcome has been achieved. Measurements belong in Performance.", sort: 1),
                Field("benefits", "Benefits", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Keep original business-case commitments identifiable even after later revisions.", sort: 2),
                Field("policy-intents", "Policy intents", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Link policy references this service supports. Canonical policy records may live elsewhere.", sort: 3),
                Field("strategic-objectives", "Strategic objectives", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Named objectives from an agreed strategy. Do not invent a second strategy register.", sort: 4),
                Field("business-cases", "Business cases", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Reference and version of the case. Store a link, not a copy of the approved document.", sort: 5),
                SectionStatus("purpose", 6)),

            Theme("users", "Users and needs", 30,
                "Who is served, which needs are met, and which journeys this service supports.",
                Field("user-groups", "User groups and relationships", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "The same group can have more than one relationship type. Do not put systems in this taxonomy.", sort: 1),
                Lookup("primary-user-group-type", "Primary user group type", "lookup_ref",
                    "Classification from the shared user-group taxonomy (admin FIPS user groups lookup).", false, 2,
                    CensusAdminLookupOptions.FipsUserGroups, allowMultiple: false),
                Field("user-needs", "Needs", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Express the need independently of an assumed solution.", sort: 3),
                Field("journeys", "Journeys", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "A journey may cross service and product boundaries.", sort: 4),
                Field("journey-stages", "Journey stages supported", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Repeatable stages. Name the stage and the user goal at that point.", sort: 5),
                Field("need-fulfilment", "Need fulfilment assessments", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Evidence-based assessment of how well a need is met. Do not overwrite history — add another dated assessment.", sort: 6),
                Field("user-evidence", "Research and other user evidence", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Link research or data and its limitations. An assumption must not be presented as validated research.", sort: 7),
                SectionStatus("users", 8)),

            Theme("ownership", "Ownership", 40,
                "Roles and contacts from the Service Register. Additional census responsibilities only for operational roles not on that list.",
                Field("additional-responsibilities", "Additional census responsibilities", ServiceDataModelFieldType.PersonOrTeamReference, "COLLECTION",
                    "Use only for operational roles that are not on the service-record contact list.", sort: 1),
                Field("decision-authority", "Decision authority", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Who is authorised to decide, and any threshold or delegation reference.", sort: 2),
                Field("engagement-relationships", "Engagement relationships", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Goes beyond a static RACI: initiator, performer, consulter, decision-maker, recipient.", sort: 3),
                SectionStatus("ownership", 4)),

            Theme("capability", "Capability and process", 50,
                "Level 1 capabilities from the catalogue, and processes this service provides, supports or depends on.",
                Lookup("capabilities", "Capabilities", "lookup_ref",
                    "Choose a level 1 capability from the catalogue. Do not type a new name on the product record.",
                    false, 1, CensusAdminLookupOptions.Capabilities, allowMultiple: true),
                Field("business-processes", "Business processes", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Reference the process ID, version and map. Do not duplicate the BA process catalogue.", sort: 2),
                Field("lifecycle-activities", "Lifecycle activities", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Idea, Understand, Design and Test, Build, Run, Improve, Decommission. Position is BEFORE, DURING, AFTER or GATE.", sort: 3),
                SectionStatus("capability", 4)),

            Theme("functionality", "Functionality and reuse", 60,
                "Features implemented by this service and whether they are reused.",
                Field("features", "Features", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Features this service implements. Reuse recorded here is a candidate, not an approval.", sort: 1),
                Field("patterns-components-apis", "Patterns, components and APIs", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Shared building blocks. Record availability and constraints; this is not a technical assessment.", sort: 2),
                Field("reuse-assessments", "Reuse assessments", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "A candidate does not mean approved for reuse.", sort: 3),
                SectionStatus("functionality", 4)),

            Theme("technology", "Technology", 70,
                "Technology, platforms and architecture references.",
                Field("technologies", "Technologies and components", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Link or narrate the technology. Verified inventory belongs in the architecture source.", sort: 1),
                Field("stacks-platforms", "Stacks and platforms", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Named stack or platform this service runs on or contributes to.", sort: 2),
                Field("integrations", "Integrations", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Source, destination, direction, purpose and protocol. External endpoints are allowed.", sort: 3),
                Field("dependencies", "Dependencies", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Initial associations only. Detailed mapping lives with architecture.", sort: 4),
                SectionStatus("technology", 5)),

            Theme("data", "Data and information", 80,
                "Information assets, models and data use.",
                Field("information-assets", "Information assets and data entities", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Name, definition and canonical identifier. This is not a competing data catalogue.", sort: 1),
                Field("information-flows", "Information flows", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "What flows, between whom, for what purpose, and how it is transferred.", sort: 2),
                Field("data-use", "Data use", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Creates, reads, updates, publishes or consumes. Authoritative source may vary by field.", sort: 3),
                SectionStatus("data", 4)),

            Theme("performance", "Performance", 90,
                "Measures and evidence of performance, not a substitute for reporting systems.",
                Field("measures", "Measures", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Name, definition, method and cadence. This is not a reporting platform.", sort: 1),
                Field("measurements", "Measurements", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Link an external observation. Do not replace the reporting source.", sort: 2),
                Field("benefit-realisation", "Benefit realisation", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Baseline, approved target and actual evidence. Distinct from the original benefit statement.", sort: 3),
                SectionStatus("performance", 4)),

            Theme("governance", "Governance and assurance", 100,
                "Obligations, standards and assurance activity.",
                Field("obligations", "Obligations, standards and requirements", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Authority, version, applicability and canonical link. Do not create a competing obligation register.", sort: 1),
                Field("assurance-activity", "Assurance activity", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Assessment identity, date, outcome and findings URL. Prefer COMPASS or another authoritative source.", sort: 2),
                SectionStatus("governance", 3)),

            Theme("risk", "Risk and service management", 110,
                "Referenced risks and issues. This is not a live risk register.",
                Field("risks-issues", "Referenced risks and issues", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Record a title and summary with a source link where possible. This is not a live risk register.", sort: 1),
                Field("incidents-problems", "Issues, incidents and problems", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Canonical source ID, title and link. Do not duplicate live service-management records.", sort: 2),
                Field("operating-arrangements", "Operating arrangements", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Delivery model, support model, providers, hours and hand-over. These are distinct dimensions.", sort: 3),
                Choice("delivery-model", "Principal delivery model", "lookup_ref",
                    "Who organises and carries out delivery/change.", false, 4,
                    ("IN_HOUSE", "In house"),
                    ("SUPPLIER_LED", "Supplier led"),
                    ("JOINT_DFE_SUPPLIER", "Joint DfE / supplier"),
                    ("CROSS_GOVERNMENT", "Cross government"),
                    ("PARTNER_LED", "Partner led"),
                    ("OTHER", "Other"),
                    ("UNKNOWN", "Unknown")),
                Choice("support-model", "Principal support model", "lookup_ref",
                    "How operational support is organised.", false, 5,
                    ("PRODUCT_TEAM", "Product team"),
                    ("DEDICATED_INTERNAL", "Dedicated internal"),
                    ("SHARED_INTERNAL", "Shared internal"),
                    ("SUPPLIER_MANAGED", "Supplier managed"),
                    ("HYBRID", "Hybrid"),
                    ("CROSS_GOVERNMENT", "Cross government"),
                    ("PARTNER_PROVIDED", "Partner provided"),
                    ("UNDEFINED", "Undefined"),
                    ("UNKNOWN", "Unknown")),
                SectionStatus("risk", 6)),

            Theme("findings", "Findings and future", 120,
                "Current-state findings, opportunities and proposed direction.",
                Field("findings", "Current-state findings", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Record an observation and type. This is a census of findings, not a live issue tracker.", sort: 1),
                Field("opportunities", "Opportunities", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Proposed improvement and the outcome sought. This is not an approved change.", sort: 2),
                Field("roadmaps", "Roadmaps, plans and backlogs", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Name, canonical source, owner and period. Avoid duplicating delivery backlogs.", sort: 3),
                Field("future-requirements", "Future requirements", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "Testable requirement sourced from a need, obligation or finding.", sort: 4),
                Field("interventions", "Proposed interventions", ServiceDataModelFieldType.MultilineText, "COLLECTION",
                    "A proposal is distinct from an approved decision.", sort: 5),
                SectionStatus("findings", 6)),

            Theme("evidence", "Evidence", 130,
                "Links to supporting resources. Empty means not recorded.",
                Field("evidence-links", "Evidence and resources", ServiceDataModelFieldType.Url, "URL",
                    "Links only. This application does not fetch or preview restricted documents.", sort: 1),
                Field("evidence-summary", "Why this evidence is relevant", ServiceDataModelFieldType.MultilineText, "TEXTAREA",
                    "Relevance and what the resource supports.", sort: 2),
                SectionStatus("evidence", 3))
        };

        return themes;
    }

    private static CoreThemeDefinition Theme(
        string key,
        string name,
        int sort,
        string guidance,
        params CoreFieldDefinition[] fields) =>
        Theme(key, name, sort, isServiceOffering: false, guidance, fields);

    private static CoreThemeDefinition Theme(
        string key,
        string name,
        int sort,
        bool isServiceOffering,
        string guidance,
        params CoreFieldDefinition[] fields) =>
        new(key, name, guidance, sort, isServiceOffering, fields);

    private static CoreFieldDefinition Field(
        string key,
        string label,
        ServiceDataModelFieldType type,
        string schemaSourceType,
        string guidance,
        bool mandatory = false,
        int sort = 1) =>
        new(key, label, guidance, type, schemaSourceType, mandatory, CountsTowardsCompletion: true, sort, null,
            Array.Empty<CoreOptionDefinition>(), OptionsLookupKey: null, AllowMultiple: false);

    private static CoreFieldDefinition Lookup(
        string key,
        string label,
        string schemaSourceType,
        string guidance,
        bool mandatory,
        int sort,
        string optionsLookupKey,
        bool allowMultiple = false) =>
        new(key, label, guidance, ServiceDataModelFieldType.Lookup, schemaSourceType, mandatory, true, sort, null,
            Array.Empty<CoreOptionDefinition>(), optionsLookupKey, allowMultiple);

    private static CoreFieldDefinition Choice(
        string key,
        string label,
        string schemaSourceType,
        string guidance,
        bool mandatory,
        int sort,
        params (string ValueKey, string Label)[] options) =>
        Choice(key, label, schemaSourceType, guidance, mandatory, sort, optionsLookupKey: null, options);

    private static CoreFieldDefinition Choice(
        string key,
        string label,
        string schemaSourceType,
        string guidance,
        bool mandatory,
        int sort,
        string optionsLookupKey) =>
        Choice(key, label, schemaSourceType, guidance, mandatory, sort, optionsLookupKey,
            Array.Empty<(string ValueKey, string Label)>());

    private static CoreFieldDefinition Choice(
        string key,
        string label,
        string schemaSourceType,
        string guidance,
        bool mandatory,
        int sort,
        string? optionsLookupKey,
        params (string ValueKey, string Label)[] options) =>
        new(key, label, guidance, ServiceDataModelFieldType.SingleChoice, schemaSourceType, mandatory, true, sort, null,
            options.Select((o, i) => new CoreOptionDefinition(o.ValueKey, o.Label, i + 1)).ToArray(),
            optionsLookupKey, AllowMultiple: false);

    /// <summary>
    /// Per-theme section status. Stable keys must be unique across the shared catalogue
    /// (answers and prefill index by <see cref="CoreFieldDefinition.StableKey"/>).
    /// </summary>
    public static string SectionStatusStableKey(string themeStableKey) =>
        $"{themeStableKey.Trim().ToLowerInvariant()}-section-status";

    private static CoreFieldDefinition SectionStatus(string themeStableKey, int sort) =>
        Choice(SectionStatusStableKey(themeStableKey), "Section information status", "lookup_ref",
            SectionInformationStatusGuidance,
            false, sort,
            MissingInformationStatusOptions.Select(o => (o.ValueKey, o.Label)).ToArray());
}

public sealed record CoreThemeDefinition(
    string StableKey,
    string Name,
    string? Guidance,
    int SortOrder,
    bool IsServiceOffering,
    IReadOnlyList<CoreFieldDefinition> Fields);

public sealed record CoreFieldDefinition(
    string StableKey,
    string Label,
    string? Guidance,
    ServiceDataModelFieldType FieldType,
    string? SchemaSourceType,
    bool IsMandatory,
    bool CountsTowardsCompletion,
    int SortOrder,
    string? VisibilityRuleJson,
    IReadOnlyList<CoreOptionDefinition> Options,
    string? OptionsLookupKey = null,
    bool AllowMultiple = false);

public sealed record CoreOptionDefinition(string ValueKey, string Label, int SortOrder);
