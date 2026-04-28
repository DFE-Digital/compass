namespace Compass.ViewModels.Modern;

public sealed class RaidGovUkDateFieldVm
{
    /// <summary>Prefix for bound properties: e.g. <c>Identified</c> → IdentifiedDay, IdentifiedMonth, IdentifiedYear.</summary>
    public required string NamePrefix { get; init; }

    public required string Legend { get; init; }

    public string? Hint { get; init; }

    public int? Day { get; init; }
    public int? Month { get; init; }
    public int? Year { get; init; }

    public string? FieldIdPrefix { get; init; }

    /// <summary>Id on the outer form group for error-summary anchors (defaults to <see cref="NamePrefix"/>).</summary>
    public string? FormGroupId { get; init; }

    public bool HasError { get; init; }
    public string? ErrorMessage { get; init; }
}
