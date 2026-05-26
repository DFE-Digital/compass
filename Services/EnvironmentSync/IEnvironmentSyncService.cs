namespace Compass.Services.EnvironmentSync;

public interface IEnvironmentSyncService
{
  Task<EnvironmentSyncConnectionInfo> GetConnectionInfoAsync(CancellationToken cancellationToken = default);

  Task<EnvironmentSyncResult> ValidateDirectionAsync(
    EnvironmentSyncDirection direction,
    CancellationToken cancellationToken = default);

  Task<EnvironmentSyncPreview> PreviewAsync(
    EnvironmentSyncDirection direction,
    CancellationToken cancellationToken = default);

  Task<EnvironmentSyncResult> ExecuteAsync(
    EnvironmentSyncDirection direction,
    string confirmationPhrase,
    string actorEmail,
    bool dryRun = false,
    CancellationToken cancellationToken = default);
}
