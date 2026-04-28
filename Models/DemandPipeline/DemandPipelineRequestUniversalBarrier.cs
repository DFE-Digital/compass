using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models.DemandPipeline;

/// <summary>Junction: demand explore form — universal barriers impacted (multi-select).</summary>
[Table("DemandPipelineRequestUniversalBarriers")]
public class DemandPipelineRequestUniversalBarrier
{
    public Guid DemandPipelineRequestId { get; set; }

    public int UniversalBarrierLookupId { get; set; }

    [ForeignKey(nameof(DemandPipelineRequestId))]
    public DemandPipelineRequest? DemandPipelineRequest { get; set; }

    [ForeignKey(nameof(UniversalBarrierLookupId))]
    public UniversalBarrierLookup? UniversalBarrierLookup { get; set; }
}
