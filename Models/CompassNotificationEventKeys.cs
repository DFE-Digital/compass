namespace Compass.Models;

/// <summary>Stable keys for <see cref="CompassNotificationSetting"/> rows and email audit.</summary>
public static class CompassNotificationEventKeys
{
    public const string RiskIssueCreated = "compass.risk-issue.created";
    public const string RiskIssueEscalated = "compass.risk-issue.escalated";
    public const string RiskIssueDeescalated = "compass.risk-issue.deescalated";
    public const string RiskIssueClosed = "compass.risk-issue.closed";

    /// <summary>Reminder when monthly work reporting submission is open for a period.</summary>
    public const string WorkReportingMonthlyOpen = "compass.work-reporting.monthly-open";

    public static IReadOnlyList<string> All { get; } =
    [
        RiskIssueCreated,
        RiskIssueEscalated,
        RiskIssueDeescalated,
        RiskIssueClosed,
        WorkReportingMonthlyOpen,
    ];

    public static string GetDisplayName(string eventKey) => eventKey switch
    {
        RiskIssueCreated => "Created",
        RiskIssueEscalated => "Escalated",
        RiskIssueDeescalated => "De-escalated",
        RiskIssueClosed => "Closed",
        WorkReportingMonthlyOpen => "Monthly reporting open",
        _ => eventKey,
    };

    /// <summary>Longer label for admin email log and history.</summary>
    public static string GetAdminDisplayName(string eventKey) => eventKey switch
    {
        RiskIssueCreated => "Risk and issues — Created",
        RiskIssueEscalated => "Risk and issues — Escalated",
        RiskIssueDeescalated => "Risk and issues — De-escalated",
        RiskIssueClosed => "Risk and issues — Closed",
        WorkReportingMonthlyOpen => "Work reporting — Monthly reporting open",
        _ => eventKey,
    };
}
