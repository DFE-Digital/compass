namespace Compass.Services.EnvironmentSync;

public enum EnvironmentSyncDirection
{
  /// <summary>Development → production: service register data only.</summary>
  DevToProdServiceRegister = 1,

  /// <summary>Production → development: work register, risks, and issues.</summary>
  ProdToDevWorkRaid = 2
}

public sealed class EnvironmentSyncConnectionInfo
{
  public required string CurrentCatalog { get; init; }
  public required string PeerCatalog { get; init; }
  public required bool CurrentIsProduction { get; init; }
  public required bool PeerIsProduction { get; init; }
  public required bool IsEnabled { get; init; }
  public string? DisabledReason { get; init; }
}

public sealed class EnvironmentSyncPreview
{
  public EnvironmentSyncDirection Direction { get; init; }
  public required string SourceCatalog { get; init; }
  public required string TargetCatalog { get; init; }
  public required IReadOnlyList<EnvironmentSyncCountLine> Counts { get; init; }
  public required string ConfirmationPhrase { get; init; }
}

public sealed class EnvironmentSyncCountLine
{
  public required string Label { get; init; }
  public int SourceCount { get; init; }
  public int TargetCount { get; init; }
  public int WouldCreate { get; init; }
  public int WouldUpdate { get; init; }
}

public sealed class EnvironmentSyncResult
{
  public bool Success { get; init; }
  public bool DryRun { get; init; }
  public EnvironmentSyncDirection Direction { get; init; }
  public required string SourceCatalog { get; init; }
  public required string TargetCatalog { get; init; }
  public required IReadOnlyList<string> Messages { get; init; }
  public required IReadOnlyList<string> Errors { get; init; }
  public int Created { get; init; }
  public int Updated { get; init; }
  public int Skipped { get; init; }

  public static EnvironmentSyncResult Fail(string error) => new()
  {
    Success = false,
    DryRun = false,
    Direction = EnvironmentSyncDirection.DevToProdServiceRegister,
    SourceCatalog = "",
    TargetCatalog = "",
    Messages = [],
    Errors = [error]
  };
}
