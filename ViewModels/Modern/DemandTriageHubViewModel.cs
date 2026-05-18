using Compass.Models.DemandPipeline;

namespace Compass.ViewModels.Modern;

public class DemandTriageHubViewModel
{
    public List<DemandPipelineRequest> TriageQueue { get; set; } = new();

    public List<TriageMeetingRowViewModel> Meetings { get; set; } = new();

    public Guid? SelectedMeetingId { get; set; }

    public List<DemandPipelineRequest>? MeetingDemands { get; set; }
}

public class TriageMeetingRowViewModel
{
    public DemandPipelineTriageMeeting Meeting { get; set; } = null!;

    public int DemandCount { get; set; }
}
