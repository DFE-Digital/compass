using Compass.ViewModels.Modern;

namespace Compass.Services.ServiceSchema;

public sealed class ServiceSchemaTopic
{
    public required string Key { get; init; }
    public required string Heading { get; init; }
    public required string Help { get; init; }
    public string? HelpPanel { get; init; }
    public required string TitleLabel { get; init; }
    public string NarrativeLabel { get; init; } = "How this applies";
    /// <summary>text, lookup, catalogue, choice, or products.</summary>
    public string Mode { get; init; } = "text";
    public string? CatalogueKind { get; init; }
    public string? LookupSource { get; init; }
    public string? LookupLabel { get; init; }
    public bool CapturePerson { get; init; }
    public bool CaptureUrl { get; init; }
    public IReadOnlyList<ServiceSchemaChoice> ChoiceOptions { get; init; } = [];
    public string NavLabel { get; init; } = "";
}

public sealed class ServiceSchemaArea
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Group { get; init; }
    public required string Summary { get; init; }
    public string? HelpPanel { get; init; }
    public string NavLabel { get; init; } = "";
    public IReadOnlyList<ServiceSchemaTopic> Topics { get; init; } = [];
}

public static class ServiceSchemaAreas
{
    public const string ApiCatalogueKind = "api";
    public const string ApiProvideKey = "api-provide";
    public const string ApiUseKey = "api-use";
    public const string EnablingProductKey = "enabling-product";
    public const string ServiceResponsibilityKey = "service-responsibility";
    public const string AdditionalResponsibilityKey = "responsibility";
    public const string ProductDetailsKey = "product-details";
    public const string AuditKey = "audit";

    public static bool IsServiceResponsibility(string? key) =>
        string.Equals(key, ServiceResponsibilityKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsAdditionalResponsibility(string? key) =>
        string.Equals(key, AdditionalResponsibilityKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsProductDetails(string? key) =>
        string.Equals(key, ProductDetailsKey, StringComparison.OrdinalIgnoreCase);

    public static bool IsAudit(string? key) =>
        string.Equals(key, AuditKey, StringComparison.OrdinalIgnoreCase);
    public static IReadOnlyList<ServiceSchemaArea> All { get; } =
    [
        new()
        {
            Key = "overview",
            Name = "Task summary",
            NavLabel = "Overview",
            Group = "Sections",
            Summary = "How much of the schema has been recorded for this product."
        },
        new()
        {
            Key = "purpose",
            Name = "Purpose and value",
            NavLabel = "Purpose",
            Group = "Sections",
            Summary = "Outcomes, benefits, strategic objectives, and whether this product provides enabling functionality.",
            Topics =
            [
                Topic("outcome", "Outcomes", "Intended change this product contributes to.", "Outcome", "How this product contributes"),
                Topic("benefit", "Benefits", "Expected value of this product.", "Benefit", "Who benefits and what is expected to change"),
                Lookup("strategic-objective", "Strategic objectives", "Named objectives this product supports.", "Objective", "Contribution", "priority-outcomes", "Strategic objective", nav: "Objectives"),
                new ServiceSchemaTopic
                {
                    Key = EnablingProductKey,
                    Heading = "Does this product provide enabling functionality into DDT?",
                    Help = "If it does, record the kind of functionality and a short description.",
                    TitleLabel = "Answer",
                    NarrativeLabel = "What enabling functionality does it provide?",
                    Mode = "yes-choice",
                    NavLabel = "Enabling product",
                    ChoiceOptions =
                    [
                        new ServiceSchemaChoice { Value = "governance", Label = "Governance", Hint = "Decisions, oversight, and how work is directed." },
                        new ServiceSchemaChoice { Value = "assurance", Label = "Assurance", Hint = "Checks that work meets the expected standard." },
                        new ServiceSchemaChoice { Value = "compliance", Label = "Compliance", Hint = "Meeting legal, policy, or security obligations." },
                        new ServiceSchemaChoice { Value = "guidance", Label = "Guidance", Hint = "Advice, standards, or instructions for other services." },
                        new ServiceSchemaChoice { Value = "case-management", Label = "Case management", Hint = "Handling cases, requests, or workflow." },
                        new ServiceSchemaChoice { Value = "other", Label = "Other", Hint = "Something else this product provides for DDT." }
                    ]
                }
            ]
        },
        new()
        {
            Key = "users",
            Name = "Users and needs",
            NavLabel = "Users",
            Group = "Sections",
            Summary = "Who is served, which needs are met, and which journeys this product supports.",
            Topics =
            [
                Lookup("user-group", "User groups", "Groups of people this product serves.", "User group", "How this group relates to the product", "fips-user-groups", "User group"),
                Topic("user-need", "User needs", "Needs this product meets or is intended to meet.", "Need", "How this product supports the need"),
                Topic("journey", "Journeys", "End-to-end journeys this product supports.", "Journey", "Which part of the journey this product supports")
            ]
        },
        new()
        {
            Key = "ownership",
            Name = "Ownership",
            Group = "Sections",
            Summary = "Service offering contacts on the register, plus any further roles and responsibilities.",
            Topics =
            [
                new ServiceSchemaTopic
                {
                    Key = ServiceResponsibilityKey,
                    Heading = "Service offering roles and responsibilities",
                    Help = "These contacts come from the CMDB and cannot be changed here. Use the update service offering form in ServiceNow if they need changing. Add further contacts in Additional roles and responsibilities.",
                    HelpPanel = "Service offering contacts are maintained in the CMDB, not in Compass.\n\nIf you need to update these contacts, complete the update service offering form in ServiceNow.\n\nYou can add additional contacts such as architects, designers and analysts in the next section, Additional roles and responsibilities.",
                    TitleLabel = "Role",
                    NarrativeLabel = "What this person is accountable for",
                    NavLabel = "Service offering"
                },
                new ServiceSchemaTopic
                {
                    Key = AdditionalResponsibilityKey,
                    Heading = "Additional roles and responsibilities",
                    Help = "Further people with a role or responsibility for this product who are not already named as service offering contacts.",
                    TitleLabel = "Role or responsibility",
                    NarrativeLabel = "What this person is accountable for",
                    CapturePerson = true,
                    NavLabel = "Additional"
                }
            ]
        },
        new()
        {
            Key = "functionality",
            Name = "Functionality and reuse",
            NavLabel = "Functionality",
            Group = "Sections",
            Summary = "Features this product implements, patterns and components it provides, and APIs other services can use.",
            Topics =
            [
                Topic("feature", "Features", "Functionality implemented on this product.", "Feature", "How it is implemented"),
                Catalogue("pattern", "Patterns and components", "Patterns and components this product provides to other services.", "Pattern or component", "How other services can use this", "pattern", nav: "Patterns"),
                Catalogue(ApiProvideKey, "What APIs does this product provide for others to use", "APIs this product offers for other services to use. Record APIs this product calls under Technology.", "API", "How other services can use this", ApiCatalogueKind, url: true, nav: "APIs provided")
            ]
        },
        new()
        {
            Key = "technology",
            Name = "Technology",
            Group = "Sections",
            Summary = "Technology, components, platforms, integrations, dependencies, and APIs this product uses.",
            Topics =
            [
                Catalogue("technology", "Technologies and components", "Technology and components this product uses.", "Technology or component", "Version, type and how it is used", "technology", url: true, nav: "Technologies"),
                Catalogue(ApiUseKey, "What APIs does this product make use of (that are not its own)", "APIs this product calls that it does not provide itself. Choose an API another product provides, or add one from outside the register.", "API", "How this product uses it", ApiCatalogueKind, url: true, nav: "APIs used"),
                Catalogue("stack", "Stacks and platforms", "Stacks and platforms this product runs on or contributes to.", "Stack or platform", "How it is used", "stack", nav: "Stacks"),
                Catalogue("integration", "Integrations", "Integrations this product uses, including source, destination and purpose.", "Integration", "Direction, purpose and protocol", "integration", url: true),
                Catalogue("dependency", "Other product dependencies", "Products and services this product depends on. Internal dependencies are on the service register, including Enterprise services. External dependencies are outside DfE.", "Dependency", "Why it matters", "dependency", nav: "Other dependencies")
            ]
        },
        new()
        {
            Key = "data",
            Name = "Data and information",
            NavLabel = "Data",
            Group = "Sections",
            Summary = "Information assets this product creates, reads, updates or publishes.",
            Topics = [Topic("data", "Information assets", "Information this product creates, reads, updates or publishes.", "Information asset", "How the data is used", url: true, nav: "Information")]
        },
        new()
        {
            Key = "performance",
            Name = "Performance",
            Group = "Sections",
            Summary = "Measures and what they say about this product.",
            Topics = [Topic("measure", "Measures", "Measures for this product.", "Measure", "What this measure tells us", url: true)]
        },
        new()
        {
            Key = "governance",
            Name = "Governance and assurance",
            NavLabel = "Governance",
            Group = "Sections",
            Summary = "Assurance activity for this product.",
            Topics = [Topic("assurance", "Assurance", "Assurance activity for this product.", "Assurance activity", "What it covers", url: true)]
        },
        new()
        {
            Key = "risk",
            Name = "Risk and service management",
            NavLabel = "Risk",
            Group = "Sections",
            Summary = "Risks and issues for this product.",
            Topics = [Topic("risk", "Risks and issues", "Risks and issues for this product.", "Risk or issue", "How it relates to this product", url: true, nav: "Risks")]
        },
        new()
        {
            Key = "findings",
            Name = "Findings and future",
            NavLabel = "Findings",
            Group = "Sections",
            Summary = "Current-state observations and proposed improvements.",
            Topics =
            [
                Topic("finding", "Findings", "Observations, with evidence.", "Finding", "Consequence and evidence"),
                Topic("opportunity", "Opportunities", "Proposed improvements.", "Opportunity", "Outcome sought")
            ]
        },
        new()
        {
            Key = "evidence",
            Name = "Evidence",
            Group = "Sections",
            Summary = "Links to evidence, guidance or research.",
            Topics = [Topic("resource", "Evidence and resources", "Evidence, guidance or research for this product.", "Resource", "What this resource supports", url: true, nav: "Resources")]
        }
    ];

    public static IReadOnlyList<ServiceSchemaTopic> Topics { get; } = All.SelectMany(a => a.Topics).ToList();

    public static ServiceSchemaArea Find(string? key) =>
        All.FirstOrDefault(a => string.Equals(a.Key, key, StringComparison.OrdinalIgnoreCase)) ?? All[0];

    public static ServiceSchemaTopic? FindTopic(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
            return null;
        return Topics.FirstOrDefault(t => DomainMatches(t, key));
    }

    public static bool DomainMatches(ServiceSchemaTopic topic, string? domainKey)
    {
        if (string.IsNullOrWhiteSpace(domainKey))
            return false;
        if (string.Equals(topic.Key, domainKey, StringComparison.OrdinalIgnoreCase))
            return true;
        return LegacyDomainKeys.TryGetValue(topic.Key, out var aliases)
            && aliases.Any(alias => string.Equals(alias, domainKey, StringComparison.OrdinalIgnoreCase));
    }

    private static readonly Dictionary<string, string[]> LegacyDomainKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        ["user-group"] = ["user_group"],
        ["user-need"] = ["user_need"]
    };

    public static string ShortAreaName(ServiceSchemaArea area) =>
        string.IsNullOrWhiteSpace(area.Name) ? area.Key : area.Name.Trim();

    public static string ShortTopicName(ServiceSchemaTopic topic) =>
        string.IsNullOrWhiteSpace(topic.NavLabel) ? topic.Heading : topic.NavLabel;

    private static ServiceSchemaTopic Topic(string key, string heading, string help, string title, string narrative, bool url = false, string? nav = null) =>
        new() { Key = key, Heading = heading, Help = help, TitleLabel = title, NarrativeLabel = narrative, CaptureUrl = url, NavLabel = nav ?? heading };

    private static ServiceSchemaTopic Lookup(string key, string heading, string help, string title, string narrative, string source, string label, string? nav = null) =>
        new()
        {
            Key = key, Heading = heading, Help = help, TitleLabel = title, NarrativeLabel = narrative,
            Mode = "lookup", LookupSource = source, LookupLabel = label, NavLabel = nav ?? heading
        };

    private static ServiceSchemaTopic Catalogue(string key, string heading, string help, string title, string narrative, string kind, bool url = false, string? nav = null) =>
        new()
        {
            Key = key, Heading = heading, Help = help, TitleLabel = title, NarrativeLabel = narrative,
            Mode = "catalogue", CatalogueKind = kind, CaptureUrl = url, NavLabel = nav ?? heading
        };
}
