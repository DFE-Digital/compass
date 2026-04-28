namespace Compass.ViewModels.Modern;

public sealed class CompassNotificationSettingsPageViewModel
{
    public List<CompassNotificationSettingRowViewModel> Rows { get; set; } = new();
}

public sealed class CompassNotificationSettingRowViewModel
{
    public string Category { get; set; } = string.Empty;
    public string EventKey { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Help { get; set; } = string.Empty;

    public bool ShowFipsServiceOwner { get; set; }
    public bool ShowPrimaryWorkContact { get; set; }
    public bool ShowCentralOps { get; set; }
    public bool ShowRiskIssueOwnerOrCreator { get; set; }

    public bool IsEnabled { get; set; }
    public bool SendToFipsServiceOwner { get; set; }
    public bool SendToPrimaryWorkContact { get; set; }
    public bool SendToCentralOps { get; set; }
    public bool SendToRiskIssueOwnerOrCreator { get; set; }
}

public sealed class CompassNotificationEmailLogPageViewModel
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 40;
    public int TotalCount { get; init; }
    public int TotalPages => TotalCount <= 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)Math.Max(PageSize, 1));

    public IReadOnlyList<CompassNotificationEmailLogRowViewModel> Rows { get; init; } =
        Array.Empty<CompassNotificationEmailLogRowViewModel>();
}

public sealed class CompassNotificationEmailLogRowViewModel
{
    public DateTime SentAtUtc { get; init; }
    public string EventKey { get; init; } = "";
    public string EventDisplayName { get; init; } = "";
    public string RecipientEmail { get; init; } = "";
    public string? RecipientName { get; init; }
    public string Subject { get; init; } = "";
    public bool SendSucceeded { get; init; }
    public string? ContextReference { get; init; }
}
