namespace Compass.ViewModels.Modern;

public sealed class RaidEntityCommentsPanelVm
{
    public required string EntityType { get; init; }
    public int EntityId { get; init; }
    public string PanelId { get; init; } = "raid-entity-comments-panel";
}
