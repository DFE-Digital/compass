namespace Compass.ViewModels.Modern;

public class ServiceSchemaChoice
{
    public string Value { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Group { get; set; }
}

public class ServiceSchemaEntryRow
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string? Narrative { get; set; }
    public string? Meta { get; set; }
    public string? Url { get; set; }
    public bool FromRegister { get; set; }
    public string VerificationLabel { get; set; } = "Unverified";
}

public class ServiceSchemaLinkedService
{
    public Guid LinkId { get; set; }
    public string ServiceName { get; set; } = "";
    public string RelationshipLabel { get; set; } = "";
    public string? Narrative { get; set; }
    public string Lines { get; set; } = "—";
}

public class ServiceSchemaSectionPage
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Help { get; set; } = "";
    public string DomainKey { get; set; } = "";
    public string? LookupSetKey { get; set; }
    public string? LookupLabel { get; set; }
    public string? SecondaryLookupSetKey { get; set; }
    public string? SecondaryLookupLabel { get; set; }
    public string? CatalogueKind { get; set; }
    public bool CapturePerson { get; set; }
    public bool CaptureStaffRole { get; set; }
    public bool CaptureUrl { get; set; }
    public string TitleLabel { get; set; } = "Title";
    public string NarrativeLabel { get; set; } = "Narrative";
    public string DeclarationStatus { get; set; } = "";
    public string DeclarationLabel { get; set; } = "Not recorded";
    public string? DeclarationExplanation { get; set; }
    public List<ServiceSchemaEntryRow> Entries { get; set; } = new();
}

public class ServiceSchemaProductPage
{
    public bool CanEdit { get; set; }
    public List<ServiceSchemaLinkedService> LinkedServices { get; set; } = new();
    public List<ServiceSchemaChoice> Services { get; set; } = new();
    public List<ServiceSchemaChoice> RelationshipTypes { get; set; } = new();
    public List<ServiceSchemaChoice> StaffRoles { get; set; } = new();
    public List<ServiceSchemaChoice> DeclarationStatuses { get; set; } = new();
    public List<ServiceSchemaChoice> VerificationStatuses { get; set; } = new();
    public Dictionary<string, List<ServiceSchemaChoice>> Lookups { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, List<ServiceSchemaChoice>> Catalogues { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<ServiceSchemaSectionPage> Sections { get; set; } = new();
}

public class ServiceSchemaLookupAdminPage
{
    public List<ServiceSchemaLookupSetRow> Sets { get; set; } = new();
}

public class ServiceSchemaLookupSetRow
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int ActiveCount { get; set; }
    public int TotalCount { get; set; }
}

public class ServiceSchemaLookupDetailPage
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public List<ServiceSchemaLookupValueRow> Values { get; set; } = new();
}

public class ServiceSchemaLookupValueRow
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class CensusRecordRow
{
    public Guid Id { get; set; }
    public string Reference { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Summary { get; set; }
    public string StatusCode { get; set; } = "";
}

public class CatalogueAdminPage
{
    public string Kind { get; set; } = "";
    public string Label { get; set; } = "";
    public string LookupSetKey { get; set; } = "";
    public List<ServiceSchemaChoice> LookupValues { get; set; } = new();
    public List<CensusRecordRow> Items { get; set; } = new();
}

public class ServiceSchemaAreaDqRow
{
    public string Name { get; set; } = "";
    public int ProductCount { get; set; }
    public int AverageDqPercent { get; set; }
    public int InProgress { get; set; }
    public int NotStarted { get; set; }
    public int Complete { get; set; }
}

public class ServiceSchemaProductCard
{
    public Guid Id { get; set; }
    public string Title { get; set; } = "";
    public string BusinessAreas { get; set; } = "";
    public int DqPercent { get; set; }
    public int SchemaPercent { get; set; }
    public int SectionsAddressed { get; set; }
    public int SectionCount { get; set; }
}

public class ServiceSchemaOverviewPage
{
    public int SectionCount { get; set; }
    public int ProductCount { get; set; }
    public int InProgressCount { get; set; }
    public int NotStartedCount { get; set; }
    public int CompleteCount { get; set; }
    public string? AreaFilter { get; set; }
    public int ProgressPage { get; set; } = 1;
    public int NotStartedPage { get; set; } = 1;
    public int CompletePage { get; set; } = 1;
    public int PageSize { get; set; } = 24;
    public int InProgressFiltered { get; set; }
    public int NotStartedFiltered { get; set; }
    public int CompleteFiltered { get; set; }
    public List<ServiceSchemaAreaDqRow> Areas { get; set; } = new();
    public List<ServiceSchemaProductCard> InProgress { get; set; } = new();
    public List<ServiceSchemaProductCard> NotStarted { get; set; } = new();
    public List<ServiceSchemaProductCard> Complete { get; set; } = new();
}

public class ServiceSchemaNavItem
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public int EntryCount { get; set; }
    public string StatusLabel { get; set; } = "Not started";
    public bool Addressed { get; set; }
}

public class ServiceSchemaSectionWorkspace
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Help { get; set; } = "";
    public string TitleLabel { get; set; } = "Title";
    public string NarrativeLabel { get; set; } = "Narrative";
    public bool CapturePerson { get; set; }
    public bool CaptureStaffRole { get; set; }
    public bool CaptureUrl { get; set; }
    public string? LookupLabel { get; set; }
    public string? SecondaryLookupLabel { get; set; }
    public List<ServiceSchemaChoice> Lookups { get; set; } = new();
    public List<ServiceSchemaChoice> SecondaryLookups { get; set; } = new();
    public List<ServiceSchemaEntryRow> Entries { get; set; } = new();
    public string DeclarationStatus { get; set; } = "";
    public string DeclarationLabel { get; set; } = "Not recorded";
    public string? DeclarationExplanation { get; set; }
}

public class ServiceSchemaDirectoryRow
{
    public Guid Id { get; set; }
    public int UniqueId { get; set; }
    public string Title { get; set; } = "";
    public string BusinessAreas { get; set; } = "";
    public string Phase { get; set; } = "";
    public string Status { get; set; } = "";
    public string CensusState { get; set; } = "";
    public int SchemaPercent { get; set; }
    public int SectionsAddressed { get; set; }
    public int SectionCount { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class ServiceSchemaDirectoryPage
{
    public string? Query { get; set; }
    public string Tab { get; set; } = "active";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public int Total { get; set; }
    public int TotalPages { get; set; } = 1;
    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int NotStarted { get; set; }
    public int InProgress { get; set; }
    public int Complete { get; set; }
    public int QueueTotal { get; set; }
    public List<ServiceSchemaDirectoryRow> MyProducts { get; set; } = new();
    public List<ServiceSchemaDirectoryRow> Queue { get; set; } = new();
    public List<ServiceSchemaDirectoryRow> Items { get; set; } = new();
}

public class ServiceSchemaAreaNav
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Group { get; set; } = "";
    public string State { get; set; } = "empty";
    public string StateLabel { get; set; } = "Not recorded";
}

public class ServiceSchemaTopicBlock
{
    public string Key { get; set; } = "";
    public string Heading { get; set; } = "";
    public string Help { get; set; } = "";
    public string TitleLabel { get; set; } = "Title";
    public string NarrativeLabel { get; set; } = "Narrative";
    public string Mode { get; set; } = "text";
    public string? CatalogueKind { get; set; }
    public string? LookupLabel { get; set; }
    public string? LookupAdminPanel { get; set; }
    public bool CapturePerson { get; set; }
    public bool CaptureUrl { get; set; }
    public string State { get; set; } = "empty";
    public string StateLabel { get; set; } = "Not recorded";
    public bool NothingToRecord { get; set; }
    public List<ServiceSchemaChoice> Choices { get; set; } = new();
    public List<ServiceSchemaEntryRow> Entries { get; set; } = new();
}

public class ServiceSchemaWorkspacePage
{
    public Guid ProductId { get; set; }
    public string ProductTitle { get; set; } = "";
    public string BusinessAreas { get; set; } = "";
    public int DqPercent { get; set; }
    public int DqScore { get; set; }
    public int SchemaPercent { get; set; }
    public int SectionsAddressed { get; set; }
    public int SectionCount { get; set; }
    public bool CanEdit { get; set; }
    public int UniqueId { get; set; }
    public string Phase { get; set; } = "";
    public string Status { get; set; } = "";
    public string ActiveArea { get; set; } = "overview";
    public string ActiveTopic { get; set; } = "";
    public string AreaName { get; set; } = "";
    public string AreaGroup { get; set; } = "";
    public string AreaSummary { get; set; } = "";
    public List<ServiceSchemaNavItem> Nav { get; set; } = new();
    public List<ServiceSchemaAreaNav> Areas { get; set; } = new();
    public List<ServiceSchemaTopicBlock> Topics { get; set; } = new();
    public ServiceSchemaSectionWorkspace Section { get; set; } = new();
    public List<ServiceSchemaChoice> StaffRoles { get; set; } = new();
}

public class ServiceSchemaQuestionsAdminPage
{
    public List<ServiceSchemaAreaAdminRow> Areas { get; set; } = new();
}

public class ServiceSchemaAreaAdminRow
{
    public string Key { get; set; } = "";
    public string Name { get; set; } = "";
    public string Summary { get; set; } = "";
    public int SortOrder { get; set; }
    public List<ServiceSchemaQuestionAdminRow> Questions { get; set; } = new();
}

public class ServiceSchemaQuestionAdminRow
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string Heading { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class ServiceSchemaQuestionEditPage
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string AreaKey { get; set; } = "";
    public string Heading { get; set; } = "";
    public string Help { get; set; } = "";
    public string TitleLabel { get; set; } = "";
    public string NarrativeLabel { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsBuiltIn { get; set; }
    public bool HasResponses { get; set; }
    public List<ServiceSchemaChoice> Areas { get; set; } = new();
}
