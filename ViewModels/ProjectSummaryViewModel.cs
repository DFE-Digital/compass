using System;

namespace Compass.ViewModels;

public class ProjectSummaryViewModel
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string ProjectCode { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public string? RagStatus { get; set; }

    public string? Phase { get; set; }

    public string? BusinessArea { get; set; }

    public DateTime? StartDate { get; set; }

    public DateTime? TargetDeliveryDate { get; set; }

    public string? PrimaryContactName { get; set; }

    public string? PrimaryContactEmail { get; set; }
}

