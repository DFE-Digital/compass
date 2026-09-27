namespace Compass.Models.ServiceDataModels;

public enum ServiceDataModelLifecycleStatus
{
    Draft = 0,
    Published = 1,
    Retired = 2
}

public enum ServiceDataModelFieldType
{
    Text = 0,
    MultilineText = 1,
    Number = 2,
    Date = 3,
    YesNo = 4,
    SingleChoice = 5,
    MultipleChoice = 6,
    Url = 7,
    PersonOrTeamReference = 8,
    ServiceRelationship = 9,
    /// <summary>Ordered list of Service Register product ids.</summary>
    Services = 10,
    /// <summary>Ordered list of Service Line ids from the register.</summary>
    ServiceLines = 11,
    /// <summary>
    /// Options loaded from an admin lookup. Use <c>AllowMultiple</c> for single vs multiple select.
    /// </summary>
    Lookup = 12
}

/// <summary>Workflow status for an assignment. Overdue is derived, not a status.</summary>
public enum ServiceDataModelAssignmentStatus
{
    NotStarted = 0,
    InProgress = 1,
    Submitted = 2,
    ChangesRequested = 3,
    Reviewed = 4,
    Withdrawn = 5,
    NotApplicable = 6,
    ReviewDue = 7
}

public enum ServiceDataModelApplicabilityMode
{
    /// <summary>All active Service Register products unless explicitly excluded.</summary>
    AllActive = 0,
    /// <summary>Match controlled attributes (status/phase/type/business area) plus explicit includes.</summary>
    ByAttributes = 1,
    /// <summary>Only explicitly included products.</summary>
    ExplicitOnly = 2
}

public enum ServiceDataModelExplicitServiceMode
{
    Include = 0,
    Exclude = 1
}

public enum ServiceDataModelProposedChangeStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}
