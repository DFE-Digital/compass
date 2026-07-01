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

    /// <summary>Chase when a monthly return is due tomorrow and has not been submitted.</summary>
    public const string WorkReportingMonthlyDueReminder = "compass.work-reporting.monthly-due-reminder";

    /// <summary>Chase when a monthly return is overdue (day after due date) and has not been submitted.</summary>
    public const string WorkReportingMonthlyOverdue = "compass.work-reporting.monthly-overdue";

    /// <summary>When a new work item is created in COMPASS.</summary>
    public const string WorkItemCreated = "compass.work-item.created";

    public static IReadOnlyList<string> All { get; } =
    [
        RiskIssueCreated,
        RiskIssueEscalated,
        RiskIssueDeescalated,
        RiskIssueClosed,
        WorkReportingMonthlyOpen,
        WorkReportingMonthlyDueReminder,
        WorkReportingMonthlyOverdue,
        WorkItemCreated,
    ];

    public static string GetDisplayName(string eventKey) => eventKey switch
    {
        RiskIssueCreated => "Created",
        RiskIssueEscalated => "Escalated",
        RiskIssueDeescalated => "De-escalated",
        RiskIssueClosed => "Closed",
        WorkReportingMonthlyOpen => "Monthly reporting open",
        WorkReportingMonthlyDueReminder => "Monthly return due reminder",
        WorkReportingMonthlyOverdue => "Monthly return overdue",
        WorkItemCreated => "Work item created",
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
        WorkReportingMonthlyDueReminder => "Work reporting — Monthly return due reminder",
        WorkReportingMonthlyOverdue => "Work reporting — Monthly return overdue",
        WorkItemCreated => "Work items — Work item created",
        _ => eventKey,
    };
}
