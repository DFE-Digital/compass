using Compass.Models.DemandPipeline;

namespace Compass.ViewModels.Modern;

public class DemandTriageHubViewModel
{
    public List<DemandPipelineRequest> TriageQueue { get; set; } = new();

    public List<TriageMeetingRowViewModel> Meetings { get; set; } = new();

    public Guid? SelectedMeetingId { get; set; }

    public DemandPipelineTriageMeeting? SelectedMeeting { get; set; }

    public List<DemandPipelineRequest>? MeetingDemands { get; set; }

    public Guid? SelectedDemandId { get; set; }
}

public class TriageMeetingRowViewModel
{
    public DemandPipelineTriageMeeting Meeting { get; set; } = null!;

    public int DemandCount { get; set; }
}

public class TriageMeetingPackViewModel
{
    public DemandPipelineTriageMeeting Meeting { get; set; } = null!;

    public List<DemandPipelineRequest> Demands { get; set; } = new();
}
