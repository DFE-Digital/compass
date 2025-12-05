using System;
using System.Collections.Generic;
using Compass.Models;

namespace Compass.ViewModels.DemandManagement;

public class TriageViewModel
{
    public IReadOnlyCollection<TriageMeetingSummaryViewModel> Meetings { get; set; } = Array.Empty<TriageMeetingSummaryViewModel>();
    public IReadOnlyCollection<TriageMeetingSummaryViewModel> AllMeetings { get; set; } = Array.Empty<TriageMeetingSummaryViewModel>();
    public IReadOnlyCollection<TriageMonthSummaryViewModel> Months { get; set; } = Array.Empty<TriageMonthSummaryViewModel>();
    public string? SelectedMonthKey { get; set; }
    public int? SelectedMeetingId { get; set; }
    public TriageMeetingSummaryViewModel? SelectedMeeting { get; set; }
    public TriageMeetingSummaryViewModel? PreviousMeeting { get; set; }
    public TriageMeetingSummaryViewModel? NextMeeting { get; set; }
    public IReadOnlyCollection<DemandRequest> MeetingRequests { get; set; } = Array.Empty<DemandRequest>();
    public IReadOnlyCollection<DemandRequest> AwaitingScheduling { get; set; } = Array.Empty<DemandRequest>();
    public Dictionary<int, IReadOnlyCollection<DemandRequest>> MeetingRequestsByMeetingId { get; set; } = new Dictionary<int, IReadOnlyCollection<DemandRequest>>();
}

public class TriageMeetingSummaryViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime StartAt { get; set; }
    public DateTime EndAt { get; set; }
    public string? Location { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsUpcoming { get; set; }
    public int TotalRequests { get; set; }
    public int SubmittedCount { get; set; }
    public int PrioritisationCount { get; set; }
    public int TriageCount { get; set; }
    public int DeliveryCount { get; set; }
    public int DeferredCount { get; set; }
    public int RejectedCount { get; set; }
    public int ConvertedCount { get; set; }
    public int TierOneCount { get; set; }
    public int TierTwoCount { get; set; }
    public int TierThreeCount { get; set; }
    public int FundingConfirmedCount { get; set; }
    public int HeadcountConfirmedCount { get; set; }
    public decimal? AverageScore { get; set; }
    public IReadOnlyDictionary<string, int> StatusCounts { get; set; } = new Dictionary<string, int>();
}

public class TriageMonthSummaryViewModel
{
    public string Key { get; set; } = string.Empty;
    public DateTime Month { get; set; }
    public string Label { get; set; } = string.Empty;
    public int MeetingCount { get; set; }
    public int RequestCount { get; set; }
    public bool IsCurrent { get; set; }
    public bool IsSelected { get; set; }
}
