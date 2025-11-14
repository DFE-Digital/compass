using System;
using System.Collections.Generic;
using Compass.Models;

namespace Compass.ViewModels.DemandManagement;

public class SectionStatusPanelViewModel
{
    public int DemandRequestId { get; set; }

    public string SectionKey { get; set; } = string.Empty;

    public string SectionName { get; set; } = string.Empty;

    public string Status { get; set; } = "ToDo";

    public DemandRequestSectionCompletion? Completion { get; set; }

    public bool IsAssessment { get; set; }

    public string? EditUrl { get; set; }

    public bool CanMarkComplete { get; set; }

    public IReadOnlyCollection<string> MissingFields { get; set; } = Array.Empty<string>();

    public string? SuccessMessage { get; set; }

    public string? ErrorMessage { get; set; }

    public string? CompletionNotes => Completion?.CompletionNotes;

    public string? ReturnUrl { get; set; }
}

