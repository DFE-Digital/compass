namespace Compass.Services.ServiceSchema;

public sealed class ServiceSchemaTopic
{
    public required string Key { get; init; }
    public required string Heading { get; init; }
    public required string Help { get; init; }
    public required string TitleLabel { get; init; }
    public string NarrativeLabel { get; init; } = "How this applies";
    /// <summary>text, catalogue, or lookup.</summary>
    public string Mode { get; init; } = "text";
    public string? CatalogueKind { get; init; }
    public string? LookupSource { get; init; }
    public string? LookupLabel { get; init; }
    public bool CapturePerson { get; init; }
    public bool CaptureUrl { get; init; }
}

public sealed class ServiceSchemaArea
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Group { get; init; }
    public required string Summary { get; init; }
    public IReadOnlyList<ServiceSchemaTopic> Topics { get; init; } = [];
}

public static class ServiceSchemaAreas
{
    public static IReadOnlyList<ServiceSchemaArea> All { get; } =
    [
        new()
        {
            Key = "overview",
            Name = "Completion",
            Group = "This record",
            Summary = "How much of the schema has been recorded for this product."
        },
        new()
        {
            Key = "purpose",
            Name = "Purpose and value",
            Group = "Schema",
            Summary = "Outcomes, benefits and the strategic objectives this product supports.",
            Topics =
            [
                Topic("outcome", "Outcomes", "Intended change this product contributes to.", "Outcome", "How this product contributes"),
                Topic("benefit", "Benefits", "Expected value of this product.", "Benefit", "Who benefits and what is expected to change"),
                Lookup("strategic-objective", "Strategic objectives", "Named objectives this product supports.", "Objective", "Contribution", "priority-outcomes", "Strategic objective")
            ]
        },
        new()
        {
            Key = "users",
            Name = "Users and needs",
            Group = "Schema",
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
            Group = "Schema",
            Summary = "Service owner and the other contacts on the register, plus any further responsibilities.",
            Topics =
            [
                new ServiceSchemaTopic
                {
                    Key = "responsibility",
                    Heading = "Responsibilities",
                    Help = "People already named on the service register, and any further responsibilities.",
                    TitleLabel = "Responsibility",
                    NarrativeLabel = "What this person is accountable for",
                    CapturePerson = true
                }
            ]
        },
        new()
        {
            Key = "functionality",
            Name = "Functionality and reuse",
            Group = "Schema",
            Summary = "Features this product implements, and the patterns, components and APIs it provides to other services.",
            Topics =
            [
                Topic("feature", "Features", "Functionality implemented on this product.", "Feature", "How it is implemented"),
                Catalogue("pattern", "Patterns, components and APIs", "Patterns, components, or APIs this product provides to other services. Record ones this product uses under Technology.", "Name", "How other services can use this", "pattern")
            ]
        },
        new()
        {
            Key = "technology",
            Name = "Technology",
            Group = "Schema",
            Summary = "Technology, components, APIs, platforms, integrations and dependencies this product uses.",
            Topics =
            [
                Catalogue("technology", "Technologies and components", "Technology, components, and APIs this product uses.", "Technology or component", "Version, type and how it is used", "technology", url: true),
                Catalogue("stack", "Stacks and platforms", "Stacks and platforms this product runs on or contributes to.", "Stack or platform", "How it is used", "stack"),
                Catalogue("integration", "Integrations", "Integrations this product uses, including source, destination and purpose.", "Integration", "Direction, purpose and protocol", "integration", url: true),
                Catalogue("dependency", "Dependencies", "What this product depends on.", "Dependency", "Why it matters", "dependency")
            ]
        },
        new()
        {
            Key = "data",
            Name = "Data and information",
            Group = "Schema",
            Summary = "Information assets this product creates, reads, updates or publishes.",
            Topics = [Topic("data", "Information assets", "Information this product creates, reads, updates or publishes.", "Information asset", "How the data is used", url: true)]
        },
        new()
        {
            Key = "performance",
            Name = "Performance",
            Group = "Schema",
            Summary = "Measures and what they say about this product.",
            Topics = [Topic("measure", "Measures", "Measures for this product.", "Measure", "What this measure tells us", url: true)]
        },
        new()
        {
            Key = "governance",
            Name = "Governance and assurance",
            Group = "Schema",
            Summary = "Assurance activity for this product.",
            Topics = [Topic("assurance", "Assurance", "Assurance activity for this product.", "Assurance activity", "What it covers", url: true)]
        },
        new()
        {
            Key = "risk",
            Name = "Risk and service management",
            Group = "Schema",
            Summary = "Risks and issues for this product.",
            Topics = [Topic("risk", "Risks and issues", "Risks and issues for this product.", "Risk or issue", "How it relates to this product", url: true)]
        },
        new()
        {
            Key = "findings",
            Name = "Findings and future",
            Group = "Schema",
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
            Group = "Evidence and review",
            Summary = "Links to evidence, guidance or research.",
            Topics = [Topic("resource", "Evidence and resources", "Evidence, guidance or research for this product.", "Resource", "What this resource supports", url: true)]
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

    private static ServiceSchemaTopic Topic(string key, string heading, string help, string title, string narrative, bool url = false) =>
        new() { Key = key, Heading = heading, Help = help, TitleLabel = title, NarrativeLabel = narrative, CaptureUrl = url };

    private static ServiceSchemaTopic Lookup(string key, string heading, string help, string title, string narrative, string source, string label) =>
        new()
        {
            Key = key, Heading = heading, Help = help, TitleLabel = title, NarrativeLabel = narrative,
            Mode = "lookup", LookupSource = source, LookupLabel = label
        };

    private static ServiceSchemaTopic Catalogue(string key, string heading, string help, string title, string narrative, string kind, bool url = false) =>
        new()
        {
            Key = key, Heading = heading, Help = help, TitleLabel = title, NarrativeLabel = narrative,
            Mode = "catalogue", CatalogueKind = kind, CaptureUrl = url
        };
}
