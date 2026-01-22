namespace Compass.ViewModels;

public class MonthlyReportItem
{
    public int ProjectId { get; set; }
    public string ProjectCode { get; set; } = string.Empty;
    public string ProjectTitle { get; set; } = string.Empty;
    public string BusinessArea { get; set; } = string.Empty;
    public string PrimaryContact { get; set; } = string.Empty;
    public string ServiceOwner { get; set; } = string.Empty;
    public string RagStatus { get; set; } = string.Empty;
    public List<MonthlyReportNarrative> UpdateNarratives { get; set; } = new();
    public DateTime SubmittedAt { get; set; }
    public string SubmittedBy { get; set; } = string.Empty;
    public string SubmittedByEmail { get; set; } = string.Empty;
}

public class MonthlyReportNarrative
{
    public string Narrative { get; set; } = string.Empty;
    public string SubmittedBy { get; set; } = string.Empty;
    public string SubmittedByEmail { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
}
