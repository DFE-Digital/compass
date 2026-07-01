using Compass.Services.EnvironmentSync;

namespace Compass.ViewModels.Modern;

public sealed class EnvironmentSyncPanelViewModel
{
  public EnvironmentSyncConnectionInfo? Connection { get; init; }
  public EnvironmentSyncPreview? ServiceRegisterPreview { get; init; }
  public EnvironmentSyncPreview? WorkRaidPreview { get; init; }
  public EnvironmentSyncResult? LastResult { get; init; }
}
