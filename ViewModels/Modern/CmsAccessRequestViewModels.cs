namespace Compass.ViewModels.Modern;

public sealed class CmsAccessRequestListViewModel
{
    public string ActiveTab { get; init; } = "new";

    public int NewCount { get; init; }
    public int CompletedCount { get; init; }
    public int RejectedCount { get; init; }

    public IReadOnlyList<CmsAccessRequestRowViewModel> Rows { get; init; } = Array.Empty<CmsAccessRequestRowViewModel>();
}

public sealed class CmsAccessRequestRowViewModel
{
    public int Id { get; init; }
    public string RequestorDisplayName { get; init; } = "";
    public string CmsName { get; init; } = "";
    public DateTime DateRequested { get; init; }
    public string Status { get; init; } = "";
    public string? Outcome { get; init; }
}

public sealed class CmsAccessRequestDetailViewModel
{
    public int Id { get; init; }
    public string CmsName { get; init; } = "";
    public string SignInPageUrl { get; init; } = "";
    public string RequestorEmail { get; init; } = "";
    public string RequestorFirstName { get; init; } = "";
    public string RequestorLastName { get; init; } = "";
    public string RequestorDisplayName { get; init; } = "";
    public DateTime DateRequested { get; init; }
    public bool PublisherAccessRequired { get; init; }
    public string? Comments { get; init; }
    public string Status { get; init; } = "New";
    public string? Outcome { get; init; }
    public string? RegistrationToken { get; init; }
    public bool CanProcess { get; init; }
    public string? ReturnTab { get; init; }

    /// <summary>Repopulated when the process form validation fails.</summary>
    public string? DraftOutcome { get; init; }

    public string? DraftRegistrationToken { get; init; }
}

public sealed class CmsAccessRequestProcessForm
{
    public string Outcome { get; set; } = "";

    /// <summary>Password setup link or token text inserted into the granted email.</summary>
    public string? RegistrationToken { get; set; }
}
