namespace Compass.Services;

/// <summary>Explicit monthly work reporting period configured in Admin (commission-style dates).</summary>
public sealed record MonthlyReportingPeriodInfo(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    DateTime SubmissionOpens,
    DateTime SubmissionCloses,
    string PeriodLabel);
