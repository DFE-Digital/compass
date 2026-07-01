using Compass.Models.Fips;

namespace Compass.ViewModels.Modern;

/// <summary>POST body for Service register bulk update (New and All tabs).</summary>
public sealed class ServiceRegisterBulkNewForm
{
    /// <summary>Submitting view: <c>new</c> (only <see cref="Models.Fips.CMDBProductStatus.New" /> in scope) or <c>all</c> (any listed product id).</summary>
    public string? SourceTab { get; set; }

    public List<Guid> ProductIds { get; set; } = new();

    public bool ApplyStatus { get; set; }
    /// <summary>Maps to <see cref="CMDBProductStatus"/> when <see cref="ApplyStatus"/> is set.</summary>
    public int? TargetStatus { get; set; }

    public bool ApplyPhase { get; set; }
    public int? BulkPhaseId { get; set; }

    public bool ApplyBusinessArea { get; set; }
    public int[]? BusinessAreaLookupIds { get; set; }

    public bool ApplyDirectorate { get; set; }
    public int[]? DirectorateLookupIds { get; set; }

    public bool ApplyChannel { get; set; }
    public int[]? BulkChannelIds { get; set; }

    public bool ApplyType { get; set; }
    public int[]? BulkTypeIds { get; set; }

    /// <summary><c>true</c> or <c>false</c> to set enterprise flag; empty or any other value leaves it unchanged.</summary>
    public string? BulkEnterpriseAction { get; set; }

    // Preserve list filters on redirect
    public string? RSearch { get; set; }
    public int? RBusinessAreaId { get; set; }
    public int? RChannelId { get; set; }
    public int? RUserGroupId { get; set; }
    public int? RTypeId { get; set; }
    public int? RPhaseId { get; set; }
}
