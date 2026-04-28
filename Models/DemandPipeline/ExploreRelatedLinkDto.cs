namespace Compass.Models.DemandPipeline;

/// <summary>Serialized in <see cref="DemandPipelineRequest.ExploreRelatedLinksJson"/> for structured “links to existing work”.</summary>
public class ExploreRelatedLinkDto
{
    /// <summary><c>Work</c> (Compass project) or <c>LiveService</c> (FIPS service catalogue entry).</summary>
    public string Kind { get; set; } = "";

    public int? ProjectId { get; set; }

    /// <summary>Optional Compass project code for display (e.g. ABC123).</summary>
    public string? ProjectCode { get; set; }

    /// <summary>Legacy CMS <see cref="Compass.Models.FipsService.ServiceId"/>.</summary>
    public int? ServiceId { get; set; }

    /// <summary><see cref="Compass.Models.Fips.CMDBProduct.Id"/> when <see cref="Kind"/> is LiveService.</summary>
    public Guid? CmdbProductId { get; set; }

    public string? FipsId { get; set; }

    public string Label { get; set; } = "";
}
