namespace Compass.Services.Census;

public enum CensusTaskKind
{
    LongText,
    ItemList,
    YesChoice,
    Contacts,
    Accessibility,
    Lookup,
    Form
}

public enum CensusFormFieldKind
{
    Radio,
    Text,
    LongText
}

public enum CensusLookupSource
{
    None,
    Phase,
    BusinessArea,
    Directorate,
    Channel,
    Type,
    UserGroup,
    MissionPillar,
    StrategicObjective,
    RiskAppetite,
    Technology,
    Integration
}

public enum CensusLookupControl
{
    Select,
    Radio,
    Checkbox
}

public sealed class CensusChoice
{
    public required string Value { get; init; }
    public required string Label { get; init; }
    public string? Hint { get; init; }
    public bool DividerBefore { get; init; }
}

public sealed class CensusTask
{
    public required string Key { get; init; }
    public required string GroupKey { get; init; }
    public required string GroupName { get; init; }
    public required string Heading { get; init; }
    public string NavHeading { get; init; } = "";
    public required string Help { get; init; }
    public string TitleLabel { get; init; } = "Title";
    public string NarrativeLabel { get; init; } = "Details";
    public string ItemNoun { get; init; } = "";
    public string ItemNounPlural { get; init; } = "";
    public string AddLabel { get; init; } = "";
    public string EmptyLabel { get; init; } = "";
    public bool CaptureUrl { get; init; }
    public string UrlLabel { get; init; } = "Link to reference materials";
    public string UrlHint { get; init; } = "Optional. Add a URL for guidance, a dashboard, or other material relating to this question.";
    public bool PickFromRegister { get; init; }
    public CensusTaskKind Kind { get; init; } = CensusTaskKind.ItemList;
    public string? RegisterDomainKey { get; init; }
    public CensusLookupSource LookupSource { get; init; }
    public CensusLookupControl LookupControl { get; init; }
    public IReadOnlyList<CensusChoice> ChoiceOptions { get; init; } = [];
    public IReadOnlyList<CensusFormField> FormFields { get; init; } = [];

    public string NavLabel => string.IsNullOrWhiteSpace(NavHeading) ? Heading : NavHeading;
}

public sealed class CensusFormField
{
    public required string Key { get; init; }
    public required string Label { get; init; }
    public string? Hint { get; init; }
    public CensusFormFieldKind Kind { get; init; } = CensusFormFieldKind.Radio;
    public IReadOnlyList<CensusChoice> Options { get; init; } = [];
    public string? RevealNarrativeFor { get; init; }
    public string? NarrativeLabel { get; init; }
    public IReadOnlyList<CensusConditionalChoices> ConditionalChoices { get; init; } = [];
}

public sealed class CensusConditionalChoices
{
    public required string WhenValue { get; init; }
    public required IReadOnlyList<CensusChoice> Choices { get; init; }
    public string? Legend { get; init; }
    public string? Hint { get; init; }
    public int Minimum { get; init; } = 1;
    public string AnswerTitle { get; init; } = "choice";
}

public sealed class CensusContactRole
{
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required string Hint { get; init; }
    public IReadOnlyList<string> NameAliases { get; init; } = [];
}

public static class CensusDefinition
{
    public const string AccessibilityEnrolUrl =
        "https://accessibility-statements.education.gov.uk/services?tab=not-onboarded&utm_source=census&utm_medium=referral&utm_campaign=service_onboarding";

    public const string GroupEnabling = "enabling";
    public const string GroupCrossCutting = "cross-cutting";
    public const string GroupFunding = "funding";
    public const string GroupSpanAnswerTitle = "span";
    public const string GroupEnablingKindAnswerTitle = "enabling-kind";
    public const string EnablingProductTaskKey = "enabling-product";

    private static readonly IReadOnlyList<CensusChoice> GroupSubgroupOptions =
    [
        new() { Value = "families-csc", Label = "Families: CSC", Hint = "Children's Social Care." },
        new() { Value = "families-ey", Label = "Families: EY", Hint = "Early Years." },
        new() { Value = "schools-send", Label = "Schools: SEND" },
        new() { Value = "schools-14-16-technical", Label = "Schools: 14-16 Technical" },
        new() { Value = "schools-other", Label = "Schools: Other" },
        new() { Value = "skills", Label = "Skills" },
        new() { Value = "regions", Label = "Regions" }
    ];

    private static readonly IReadOnlyList<CensusChoice> GroupSpanOptions =
    [
        ..GroupSubgroupOptions,
        new()
        {
            Value = GroupFunding,
            Label = "Strategy and Operations: Funding",
            Hint = "Cross-cutting work whose primary outcome is funding, rather than a single policy group."
        }
    ];

    private static readonly IReadOnlyList<CensusChoice> EnablingFunctionalityOptions =
    [
        new() { Value = "governance", Label = "Governance", Hint = "Decisions, oversight, and how work is directed." },
        new() { Value = "assurance", Label = "Assurance", Hint = "Checks that work meets the expected standard." },
        new() { Value = "compliance", Label = "Compliance", Hint = "Meeting legal, policy, or security obligations." },
        new() { Value = "guidance", Label = "Guidance", Hint = "Advice, standards, or instructions for other services." },
        new() { Value = "case-management", Label = "Case management", Hint = "Handling cases, requests, or workflow." },
        new() { Value = "other", Label = "Other", Hint = "Something else this product provides for DDT." }
    ];

    private static readonly IReadOnlyList<CensusChoice> GroupPrimaryOptions =
    [
        ..GroupSubgroupOptions,
        new()
        {
            Value = GroupEnabling,
            Label = "Enabling",
            Hint = "The primary outcome is improving DfE's corporate capability to operate and deliver.",
            DividerBefore = true
        },
        new()
        {
            Value = GroupCrossCutting,
            Label = "Cross-cutting",
            Hint = "The work spans more than one group and cannot be attributed to one of them."
        }
    ];

    public static IReadOnlyList<CensusContactRole> ContactRoles { get; } =
    [
        new()
        {
            Key = "policy-sro",
            Name = "Policy SRO (SCS1 or above)",
            Hint = "Senior responsible owner in the policy area. This person must be SCS1 or above.",
            NameAliases = []
        },
        new()
        {
            Key = "ddt-sro",
            Name = "DDT SRO (SCS1 or above)",
            Hint = "Senior responsible owner in Digital, Data and Technology. This person must be SCS1 or above.",
            NameAliases =
            [
                "ddt sro",
                "digital sro",
                "ddat sro",
                "deputy director sro",
                "dd sro",
                "senior responsible officer",
                "senior responsible owner"
            ]
        },
        new()
        {
            Key = "ddt-service-owner",
            Name = "DDT Service Owner",
            Hint = "Service owner for this product in DDT.",
            NameAliases = ["ddt service owner", "service owner", "serviceowner"]
        },
        new()
        {
            Key = "out-of-hours",
            Name = "Out of hours emergency contact",
            Hint = "Who to contact for a serious incident such as a data breach or major outage.",
            NameAliases = ["out of hours", "emergency", "ooh", "out-of-hours"]
        },
        new()
        {
            Key = "product-manager",
            Name = "Product Manager",
            Hint = "If the product has a product manager.",
            NameAliases = ["product manager", "product owner"]
        },
        new()
        {
            Key = "delivery-manager",
            Name = "Delivery Manager",
            Hint = "If the product has a delivery manager.",
            NameAliases = ["delivery manager"]
        },
        new()
        {
            Key = "information-asset-owner",
            Name = "Information Asset Owner",
            Hint = "Owner of the information assets this product uses or creates.",
            NameAliases = ["information asset owner", "iao"]
        },
        new()
        {
            Key = "reporting-contact",
            Name = "Reporting contact",
            Hint = "Point of contact for operational service commissions or performance reporting requests.",
            NameAliases = ["reporting contact"]
        }
    ];

    private static readonly IReadOnlyList<CensusChoice> ServicePatterns =
    [
        new() { Value = "apply", Label = "Apply for something", Hint = "Submit an application and get a decision." },
        new() { Value = "book", Label = "Book something", Hint = "Reserve an appointment, slot, or place." },
        new() { Value = "check", Label = "Check eligibility or status", Hint = "Find out if something applies, or where a case has got to." },
        new() { Value = "find", Label = "Find something", Hint = "Search for or look up information, records, or services." },
        new() { Value = "pay", Label = "Pay for something", Hint = "Make a payment, fee, or fine." },
        new() { Value = "report", Label = "Report something", Hint = "Tell government about an event, incident, or change." },
        new() { Value = "register", Label = "Register or sign up", Hint = "Create an account or enrol for a service." },
        new() { Value = "authenticate", Label = "Sign in or authenticate", Hint = "Prove who you are to access a service." },
        new() { Value = "update-details", Label = "Update personal or organisation details", Hint = "Change name, address, or other recorded information." },
        new() { Value = "prove-identity", Label = "Prove identity", Hint = "Confirm identity to use a service." },
        new() { Value = "submit-evidence", Label = "Upload or submit evidence", Hint = "Provide documents or information to support a case." },
        new() { Value = "appeal", Label = "Appeal or challenge a decision", Hint = "Ask for a decision to be reviewed." },
        new() { Value = "claim", Label = "Claim something", Hint = "Request a payment, entitlement, or refund." },
        new() { Value = "consent", Label = "Give consent or permission", Hint = "Agree to sharing data or taking an action." },
        new() { Value = "decision", Label = "Get a decision or outcome", Hint = "Receive the result of an application or assessment." },
        new() { Value = "notify", Label = "Notify or subscribe", Hint = "Get updates, alerts, or messages." },
        new() { Value = "contact", Label = "Contact or get help", Hint = "Ask a question or get support." },
        new() { Value = "track", Label = "Track progress", Hint = "Follow a case, application, or delivery." },
        new() { Value = "manage-account", Label = "Manage an account", Hint = "Change settings, users, or preferences." },
        new() { Value = "get-document", Label = "Download or get a document", Hint = "Receive a letter, certificate, or statement." },
        new() { Value = "renew", Label = "Renew or extend something", Hint = "Continue an existing permission, licence, or entitlement." },
        new() { Value = "tell-us", Label = "Tell us about a change", Hint = "Notify a change of circumstances." },
        new() { Value = "other", Label = "Something else", Hint = "A pattern not listed here." }
    ];

    public static IReadOnlyList<CensusTask> Tasks { get; } =
    [
        Form("service", "context", "Service and service line",
            "Service",
            "Which service does this product belong to? A service is a collection of products and activities that are managed together within DDT to deliver a common purpose or capability.",
            "service",
            new CensusFormField
            {
                Key = "name",
                Label = "Service name",
                Hint = "Give the name of the service, not a description of what it does.",
                Kind = CensusFormFieldKind.Text
            }),
        Form("service-line", "context", "Service and service line",
            "Service line",
            "Which service line does this product belong to? A service line is a collection of products, services and activities that are owned by a policy area or achieve a policy intent. Not all of those products, services and activities are digital or data.",
            "service-line",
            new CensusFormField
            {
                Key = "name",
                Label = "Service line name",
                Hint = "Give the name of the service line.",
                Kind = CensusFormFieldKind.Text
            },
            new CensusFormField
            {
                Key = "description",
                Label = "Service line description",
                Hint = "Describe the policy intent or outcomes this service line exists to achieve. Do not list every product, service or activity.",
                Kind = CensusFormFieldKind.LongText
            }),
        Lookup("phase", "context", "Service and service line",
            "Phase",
            "Select the current phase for this product.",
            CensusLookupSource.Phase, CensusLookupControl.Radio, "phase"),
        Lookup("business-area", "context", "Service and service line",
            "Business area",
            "Select the business area this product belongs to.",
            CensusLookupSource.BusinessArea, CensusLookupControl.Radio, "business-area"),
        Lookup("directorate", "context", "Service and service line",
            "Directorate",
            "Select the directorate this product belongs to.",
            CensusLookupSource.Directorate, CensusLookupControl.Radio, "directorate"),
        Lookup("type", "context", "Service and service line",
            "Type",
            "Select all types that apply to this product.",
            CensusLookupSource.Type, CensusLookupControl.Checkbox, "type"),
        Lookup("channel", "context", "Service and service line",
            "Channels",
            "Select all channels this product uses.",
            CensusLookupSource.Channel, CensusLookupControl.Checkbox, "channel"),
        new()
        {
            Key = "contacts",
            GroupKey = "contacts",
            GroupName = "Contacts",
            Heading = "Contacts",
            Help = "Review the people who are in defined roles, and provide information where there is no one in a role currently. This information will be updated against the service offering within a few working days.",
            Kind = CensusTaskKind.Contacts,
            RegisterDomainKey = "contact-review"
        },
        new()
        {
            Key = "accessibility",
            GroupKey = "accessibility",
            GroupName = "Accessibility",
            Heading = "Accessibility issues and statements",
            NavHeading = "Accessibility",
            Help = "Check whether this product is enrolled in the Accessibility Issues and Statement Service, and record any open issues.",
            Kind = CensusTaskKind.Accessibility,
            RegisterDomainKey = "accessibility"
        },
        LongText("aim", "purpose", "Purpose and value",
            "Aim",
            "Review the aim for this product. Explain what you are delivering, how it will be used, and what outcome it will enable. Be specific enough that someone unfamiliar with the work can understand its purpose.",
            "Aim", "aim"),
        Item("outcome", "purpose", "Purpose and value", "Outcomes",
            "Record the intended change this product contributes to. Add one entry per outcome.",
            "outcome", "outcomes", "outcome"),
        Item("benefit", "purpose", "Purpose and value", "Benefits",
            "Record the expected value of this product. Add one entry per benefit.",
            "benefit", "benefits", "benefit"),
        new()
        {
            Key = "group",
            GroupKey = "purpose",
            GroupName = "Purpose and value",
            Heading = "Primary contribution",
            Help = "Which group's outcomes does this product primarily contribute to? A group or subgroup is for work whose primary outcome is delivering or supporting a particular policy or sector outcome. Choose enabling if the primary outcome is improving DfE's corporate capability to operate and deliver, and then select the kind of enabling functionality. Choose cross-cutting if the work spans more than one group and cannot be attributed to one of them, and then select all relevant groups.",
            Kind = CensusTaskKind.Form,
            RegisterDomainKey = "group",
            FormFields =
            [
                new()
                {
                    Key = "group",
                    Label = "Primary contribution",
                    Hint = "Select one option. Enabling and cross-cutting then let you add the options that apply.",
                    Kind = CensusFormFieldKind.Radio,
                    Options = GroupPrimaryOptions,
                    ConditionalChoices =
                    [
                        new()
                        {
                            WhenValue = GroupEnabling,
                            Choices = EnablingFunctionalityOptions,
                            Legend = "What enabling functionality does this product provide into DDT?",
                            Hint = "Select all that apply.",
                            Minimum = 1,
                            AnswerTitle = GroupEnablingKindAnswerTitle
                        },
                        new()
                        {
                            WhenValue = GroupCrossCutting,
                            Choices = GroupSpanOptions,
                            Legend = "Which groups does this work span?",
                            Hint = "Select every group this work covers. Choose at least two.",
                            Minimum = 2,
                            AnswerTitle = GroupSpanAnswerTitle
                        }
                    ]
                }
            ]
        },
        Lookup("strategic-objective", "purpose", "Purpose and value",
            "Strategic objectives",
            "Select the strategic objectives this product supports.",
            CensusLookupSource.StrategicObjective, CensusLookupControl.Checkbox, "strategic-objective"),
        Lookup("user-group", "users", "Users and needs",
            "User groups",
            "Select the level 1 user groups this product serves. More detailed subgroups are not needed for the census.",
            CensusLookupSource.UserGroup, CensusLookupControl.Checkbox, "user-group"),
        Item("user-need", "users", "Users and needs", "User needs",
            "Add high-level user needs only. We do not require a need for every feature of the product. Add one entry per need.",
            "user need", "user needs", "user-need"),
        new()
        {
            Key = "pattern",
            GroupKey = "functionality",
            GroupName = "Functionality and reuse",
            Heading = "Patterns",
            Help = "Select the common government service patterns this product supports.",
            Kind = CensusTaskKind.Lookup,
            LookupControl = CensusLookupControl.Checkbox,
            RegisterDomainKey = "pattern",
            ChoiceOptions = ServicePatterns
        },
        Item("component", "functionality", "Functionality and reuse", "Components",
            "Record reusable components this product provides. Add one entry per component.",
            "component", "components", "component"),
        Item("api-provide", "functionality", "Functionality and reuse", "APIs this product provides",
            "Record APIs this product offers for other services to use. Add one entry per API. If you have documentation for the API, add a link to it.",
            "API", "APIs", "api-provide",
            url: true,
            urlLabel: "Link to API documentation",
            urlHint: "Optional. Add a link to the documentation for this API if you have it."),
        new()
        {
            Key = "source-code",
            GroupKey = "technology",
            GroupName = "Technology",
            Heading = "Source code",
            Help = "Tell us where the source code for this product is held, and add links to every repository that applies.",
            Kind = CensusTaskKind.Form,
            RegisterDomainKey = "source-code",
            TitleLabel = "Repository URL",
            ItemNoun = "repository link",
            ItemNounPlural = "repository links",
            AddLabel = "Add a repository link",
            EmptyLabel = "No repository links added",
            FormFields =
            [
                new()
                {
                    Key = "location",
                    Label = "Where is the source code for this product?",
                    Kind = CensusFormFieldKind.Radio,
                    RevealNarrativeFor = "somewhere-else",
                    NarrativeLabel = "Explain where the source code is held",
                    Options =
                    [
                        new() { Value = "azure-devops", Label = "Azure DevOps" },
                        new() { Value = "github", Label = "GitHub" },
                        new() { Value = "somewhere-else", Label = "Somewhere else (explain)" }
                    ]
                }
            ]
        },
        new()
        {
            Key = "technology",
            GroupKey = "technology",
            GroupName = "Technology",
            Heading = "What technologies is this product built using?",
            NavHeading = "Technologies",
            Help = "Select all that apply. Use the box below to tell us more about the tech stack.",
            NarrativeLabel = "Give us more information about the tech stack",
            Kind = CensusTaskKind.Lookup,
            LookupSource = CensusLookupSource.Technology,
            LookupControl = CensusLookupControl.Checkbox,
            RegisterDomainKey = "technology"
        },
        new()
        {
            Key = "ai",
            GroupKey = "technology",
            GroupName = "Technology",
            Heading = "AI functionality",
            Help = "Select Yes where AI is embedded in the service or its operation. Do not select Yes simply because AI tools were used to design, develop, test or manage the service.",
            Kind = CensusTaskKind.Form,
            RegisterDomainKey = "ai",
            FormFields =
            [
                new()
                {
                    Key = "ai",
                    Label = "Does this product include AI functionality?",
                    Kind = CensusFormFieldKind.Radio,
                    Options =
                    [
                        new() { Value = "yes", Label = "Yes", Hint = "AI is embedded in the service or how it operates." },
                        new() { Value = "no", Label = "No" }
                    ]
                }
            ]
        },
        Item("api-use", "technology", "Technology",
            "What APIs does this product make use of (this is often called consuming an API)",
            "Add one entry per API this product consumes. Do not include APIs this product provides. If you have documentation for the API, add a link to it.",
            "API", "APIs", "api-use",
            url: true,
            urlLabel: "Link to API documentation",
            urlHint: "Optional. Add a link to the documentation for this API if you have it."),
        new()
        {
            Key = "stack",
            GroupKey = "technology",
            GroupName = "Technology",
            Heading = "Hosting provision",
            Help = "Where is this product hosted?",
            Kind = CensusTaskKind.Form,
            RegisterDomainKey = "stack",
            FormFields =
            [
                new()
                {
                    Key = "hosting",
                    Label = "Hosting provision",
                    Kind = CensusFormFieldKind.Radio,
                    RevealNarrativeFor = "other",
                    NarrativeLabel = "Tell us more",
                    Options =
                    [
                        new() { Value = "azure", Label = "Azure (CIP / ELZ)" },
                        new() { Value = "aws", Label = "AWS" },
                        new() { Value = "other", Label = "Other (tell us more)" }
                    ]
                }
            ]
        },
        new()
        {
            Key = "integration",
            GroupKey = "technology",
            GroupName = "Technology",
            Heading = "What integrations does this product use?",
            NavHeading = "Integrations",
            Help = "Select all that apply.",
            Kind = CensusTaskKind.Lookup,
            LookupSource = CensusLookupSource.Integration,
            LookupControl = CensusLookupControl.Checkbox,
            RegisterDomainKey = "integration"
        },
        Item("dependency", "technology", "Technology", "Other product dependencies",
            "Search for a product or service this product depends on. If it is not listed, provide details. Add one entry per dependency.",
            "dependency", "dependencies", "dependency", pickFromRegister: true),
        Item("data", "data", "Data and information", "Information assets",
            "Record information this product creates, reads, updates or publishes. Add one entry per information asset.",
            "information asset", "information assets", "data"),
        Item("measure", "performance", "Performance", "Metrics and KPIs",
            "Record the key KPIs for this product. Add one entry per KPI. Include a link to any reference materials relating to this question.",
            "KPI", "KPIs", "measure", url: true),
        Lookup("risk-appetite", "governance", "Governance and assurance",
            "Risk appetite",
            "Select the risk appetite for this product.",
            CensusLookupSource.RiskAppetite, CensusLookupControl.Radio, "risk-appetite"),
        Item("assurance", "governance", "Governance and assurance", "Assurance",
            "Record assurance activity for this product. Add one entry per assurance activity.",
            "assurance activity", "assurance activities", "assurance"),
        new()
        {
            Key = "risk",
            GroupKey = "risk",
            GroupName = "Risk and service management",
            Heading = "Risks, issues, actions and decisions",
            NavHeading = "RAID",
            Help = "Tell us where RAID is recorded for this product, and how often it is reviewed.",
            Kind = CensusTaskKind.Form,
            RegisterDomainKey = "risk",
            FormFields =
            [
                new()
                {
                    Key = "store",
                    Label = "Where do you record risks, issues, actions and decisions for this product?",
                    Kind = CensusFormFieldKind.Radio,
                    RevealNarrativeFor = "somewhere-else",
                    NarrativeLabel = "Where do you record them?",
                    Options =
                    [
                        new() { Value = "compass", Label = "COMPASS" },
                        new() { Value = "excel", Label = "Excel" },
                        new() { Value = "we-dont", Label = "We don’t" },
                        new() { Value = "somewhere-else", Label = "Somewhere else" }
                    ]
                },
                new()
                {
                    Key = "review",
                    Label = "How often do you review these for this product?",
                    Kind = CensusFormFieldKind.Radio,
                    Options =
                    [
                        new() { Value = "daily", Label = "Daily" },
                        new() { Value = "weekly", Label = "Weekly" },
                        new() { Value = "fortnightly", Label = "Fortnightly" },
                        new() { Value = "monthly", Label = "Monthly" },
                        new() { Value = "quarterly", Label = "Quarterly" },
                        new() { Value = "ad-hoc", Label = "Ad-hoc" },
                        new() { Value = "never", Label = "Never" }
                    ]
                }
            ]
        },
        new()
        {
            Key = "incidents",
            GroupKey = "risk",
            GroupName = "Risk and service management",
            Heading = "Incidents",
            Help = "Tell us how incidents for this product are managed.",
            Kind = CensusTaskKind.Form,
            RegisterDomainKey = "incidents",
            FormFields =
            [
                new()
                {
                    Key = "process",
                    Label = "Do you have an incident management process?",
                    Kind = CensusFormFieldKind.Radio,
                    Options =
                    [
                        new() { Value = "yes", Label = "Yes" },
                        new() { Value = "no", Label = "No" }
                    ]
                },
                new()
                {
                    Key = "findout",
                    Label = "How do you find out about incidents?",
                    Kind = CensusFormFieldKind.LongText
                }
            ]
        },
        Item("resource", "evidence", "Evidence", "Evidence and resources",
            "Record evidence, guidance or research for this product. Add one entry per resource.",
            "resource", "resources", "resource")
    ];

    public static CensusTask? Find(string? key) =>
        Tasks.FirstOrDefault(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));

    public static CensusTask? Next(string key)
    {
        var i = IndexOf(key);
        return i >= 0 && i < Tasks.Count - 1 ? Tasks[i + 1] : null;
    }

    public static CensusTask? Previous(string key)
    {
        var i = IndexOf(key);
        return i > 0 ? Tasks[i - 1] : null;
    }

    public static IReadOnlyList<IGrouping<string, CensusTask>> Groups() =>
        Tasks.GroupBy(t => t.GroupKey).ToList();

    private static int IndexOf(string key) =>
        Tasks.ToList().FindIndex(t => string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase));

    private static CensusTask LongText(string key, string groupKey, string groupName, string heading, string help, string label, string domain) =>
        new()
        {
            Key = key,
            GroupKey = groupKey,
            GroupName = groupName,
            Heading = heading,
            Help = help,
            TitleLabel = label,
            NarrativeLabel = label,
            Kind = CensusTaskKind.LongText,
            RegisterDomainKey = domain
        };

    private static CensusTask Form(
        string key, string groupKey, string groupName, string heading, string help, string domain,
        params CensusFormField[] fields) =>
        new()
        {
            Key = key,
            GroupKey = groupKey,
            GroupName = groupName,
            Heading = heading,
            Help = help,
            Kind = CensusTaskKind.Form,
            RegisterDomainKey = domain,
            FormFields = fields
        };

    private static CensusTask Item(
        string key, string groupKey, string groupName, string heading, string help,
        string noun, string plural, string domain, bool url = false, bool pickFromRegister = false,
        string? urlLabel = null, string? urlHint = null)
    {
        var first = noun.Trim().Split(' ')[0];
        var an = first.Equals("API", StringComparison.OrdinalIgnoreCase)
            || (!first.Equals("user", StringComparison.OrdinalIgnoreCase)
                && "aeiou".Contains(char.ToLowerInvariant(first[0])));
        return new CensusTask
        {
            Key = key,
            GroupKey = groupKey,
            GroupName = groupName,
            Heading = heading,
            Help = help,
            TitleLabel = char.ToUpperInvariant(noun[0]) + noun[1..],
            NarrativeLabel = char.ToUpperInvariant(noun[0]) + noun[1..],
            ItemNoun = noun,
            ItemNounPlural = plural,
            AddLabel = an ? "Add an " + noun : "Add a " + noun,
            EmptyLabel = "No " + plural + " added",
            CaptureUrl = url,
            UrlLabel = urlLabel ?? "Link to reference materials",
            UrlHint = urlHint ?? "Optional. Add a URL for guidance, a dashboard, or other material relating to this question.",
            PickFromRegister = pickFromRegister,
            Kind = CensusTaskKind.ItemList,
            RegisterDomainKey = domain
        };
    }

    private static CensusTask Lookup(
        string key, string groupKey, string groupName, string heading, string help,
        CensusLookupSource source, CensusLookupControl control, string domain) =>
        new()
        {
            Key = key,
            GroupKey = groupKey,
            GroupName = groupName,
            Heading = heading,
            Help = help,
            Kind = CensusTaskKind.Lookup,
            LookupSource = source,
            LookupControl = control,
            RegisterDomainKey = domain
        };
}
