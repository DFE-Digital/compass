using System.Data.Common;
using System.Text.RegularExpressions;
using Compass.Configuration;
using Compass.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Compass.Services.EnvironmentSync;

public sealed partial class EnvironmentSyncService : IEnvironmentSyncService
{
  private static readonly SemaphoreSlim SyncLock = new(1, 1);

  private readonly CompassDbContext _currentDb;
  private readonly IConfiguration _configuration;
  private readonly IHostEnvironment _hostEnvironment;
  private readonly EnvironmentSyncOptions _options;
  private readonly ILogger<EnvironmentSyncService> _logger;

  public EnvironmentSyncService(
    CompassDbContext currentDb,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    IOptions<EnvironmentSyncOptions> options,
    ILogger<EnvironmentSyncService> logger)
  {
    _currentDb = currentDb;
    _configuration = configuration;
    _hostEnvironment = hostEnvironment;
    _options = options.Value;
    _logger = logger;
  }

  public async Task<EnvironmentSyncConnectionInfo> GetConnectionInfoAsync(CancellationToken cancellationToken = default)
  {
    var currentConn = _configuration.GetConnectionString("DefaultConnection")
      ?? throw new InvalidOperationException("DefaultConnection is not configured.");
    var peerConn = ResolvePeerConnectionString();
    var currentCatalog = ParseCatalogName(currentConn);
    var peerCatalog = ParseCatalogName(peerConn);
    var currentIsProd = IsProductionCatalog(currentCatalog);
    var peerIsProd = IsProductionCatalog(peerCatalog);
    var enabled = IsFeatureEnabled(currentIsProd, out var reason);

    _ = await _currentDb.Database.CanConnectAsync(cancellationToken);
    await using var peerDb = CreatePeerContext(peerConn);
    _ = await peerDb.Database.CanConnectAsync(cancellationToken);

    return new EnvironmentSyncConnectionInfo
    {
      CurrentCatalog = currentCatalog,
      PeerCatalog = peerCatalog,
      CurrentIsProduction = currentIsProd,
      PeerIsProduction = peerIsProd,
      IsEnabled = enabled,
      DisabledReason = reason
    };
  }

  public async Task<EnvironmentSyncResult> ValidateDirectionAsync(
    EnvironmentSyncDirection direction,
    CancellationToken cancellationToken = default)
  {
    CompassDbContext? peerDb = null;
    try
    {
      var (_, _, peerContext, sourceCatalog, targetCatalog) = CreateSyncContexts(direction);
      peerDb = peerContext;
      return new EnvironmentSyncResult
      {
        Success = true,
        Direction = direction,
        SourceCatalog = sourceCatalog,
        TargetCatalog = targetCatalog,
        Messages = ["Direction is allowed."],
        Errors = []
      };
    }
    catch (Exception ex)
    {
      return EnvironmentSyncResult.Fail(ex.Message);
    }
    finally
    {
      if (peerDb != null)
        await peerDb.DisposeAsync();
    }
  }

  public async Task<EnvironmentSyncPreview> PreviewAsync(
    EnvironmentSyncDirection direction,
    CancellationToken cancellationToken = default)
  {
    var (sourceDb, targetDb, peerDb, sourceCatalog, targetCatalog) = CreateSyncContexts(direction);
    try
    {
      var counts = direction switch
      {
        EnvironmentSyncDirection.DevToProdServiceRegister =>
          await PreviewServiceRegisterAsync(sourceDb, targetDb, cancellationToken),
        EnvironmentSyncDirection.ProdToDevWorkRaid =>
          await PreviewWorkRaidAsync(sourceDb, targetDb, cancellationToken),
        _ => throw new InvalidOperationException("Unsupported sync direction.")
      };

      return new EnvironmentSyncPreview
      {
        Direction = direction,
        SourceCatalog = sourceCatalog,
        TargetCatalog = targetCatalog,
        Counts = counts,
        ConfirmationPhrase = GetConfirmationPhrase(direction, targetCatalog)
      };
    }
    finally
    {
      if (peerDb != null)
        await peerDb.DisposeAsync();
    }
  }

  public async Task<EnvironmentSyncResult> ExecuteAsync(
    EnvironmentSyncDirection direction,
    string confirmationPhrase,
    string actorEmail,
    bool dryRun = false,
    CancellationToken cancellationToken = default)
  {
    if (!await SyncLock.WaitAsync(0, cancellationToken))
      return EnvironmentSyncResult.Fail("Another environment sync is already running. Try again shortly.");

    CompassDbContext? peerDb = null;
    try
    {
      var (sourceDb, targetDb, peerContext, sourceCatalog, targetCatalog) = CreateSyncContexts(direction);
      peerDb = peerContext;
      var expectedPhrase = GetConfirmationPhrase(direction, targetCatalog);
      if (!string.Equals(confirmationPhrase.Trim(), expectedPhrase, StringComparison.Ordinal))
      {
        return EnvironmentSyncResult.Fail(
          $"Confirmation phrase did not match. Type exactly: {expectedPhrase}");
      }

      _logger.LogWarning(
        "Environment sync {Direction} started by {Actor} ({Mode}) from {Source} to {Target}",
        direction, actorEmail, dryRun ? "dry-run" : "execute", sourceCatalog, targetCatalog);

      return direction switch
      {
        EnvironmentSyncDirection.DevToProdServiceRegister =>
          await SyncServiceRegisterAsync(sourceDb, targetDb, sourceCatalog, targetCatalog, dryRun, cancellationToken),
        EnvironmentSyncDirection.ProdToDevWorkRaid =>
          await SyncWorkRaidAsync(sourceDb, targetDb, sourceCatalog, targetCatalog, dryRun, cancellationToken),
        _ => EnvironmentSyncResult.Fail("Unsupported sync direction.")
      };
    }
    finally
    {
      SyncLock.Release();
      if (peerDb != null)
        await peerDb.DisposeAsync();
    }
  }

  private (CompassDbContext Source, CompassDbContext Target, CompassDbContext? PeerToDispose, string SourceCatalog, string TargetCatalog)
    CreateSyncContexts(EnvironmentSyncDirection direction)
  {
    var info = GetConnectionInfoAsync().GetAwaiter().GetResult();
    if (!info.IsEnabled)
      throw new InvalidOperationException(info.DisabledReason ?? "Environment sync is disabled.");

    var peerConn = ResolvePeerConnectionString();
    var peerDb = CreatePeerContext(peerConn);

    switch (direction)
    {
      case EnvironmentSyncDirection.DevToProdServiceRegister:
        if (info.CurrentIsProduction)
        {
          peerDb.Dispose();
          throw new InvalidOperationException("Service register can only be pushed to production from the development instance.");
        }
        if (!info.PeerIsProduction)
        {
          peerDb.Dispose();
          throw new InvalidOperationException("The peer database must be production for service register sync.");
        }
        return (_currentDb, peerDb, peerDb, info.CurrentCatalog, info.PeerCatalog);

      case EnvironmentSyncDirection.ProdToDevWorkRaid:
        if (info.CurrentIsProduction)
        {
          peerDb.Dispose();
          throw new InvalidOperationException("Work and RAID data can only be pulled into development from the development instance.");
        }
        if (!info.PeerIsProduction)
        {
          peerDb.Dispose();
          throw new InvalidOperationException("The peer database must be production when pulling work and RAID into development.");
        }
        if (IsProductionCatalog(info.CurrentCatalog))
        {
          peerDb.Dispose();
          throw new InvalidOperationException("Writing work or RAID data to production is not permitted.");
        }
        return (peerDb, _currentDb, peerDb, info.PeerCatalog, info.CurrentCatalog);

      default:
        peerDb.Dispose();
        throw new InvalidOperationException("Unsupported sync direction.");
    }
  }

  private string ResolvePeerConnectionString()
  {
    if (!string.IsNullOrWhiteSpace(_options.PeerConnectionString))
      return _options.PeerConnectionString;

    var peerEnvironment = _hostEnvironment.IsDevelopment() ? "Production" : "Development";
    var peerConfig = new ConfigurationBuilder()
      .SetBasePath(_hostEnvironment.ContentRootPath)
      .AddJsonFile("appsettings.json", optional: false)
      .AddJsonFile($"appsettings.{peerEnvironment}.json", optional: true)
      .Build();

    var conn = peerConfig.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(conn))
      throw new InvalidOperationException(
        $"Peer connection string is not configured. Set EnvironmentSync:PeerConnectionString or appsettings.{peerEnvironment}.json.");

    return conn;
  }

  private CompassDbContext CreatePeerContext(string connectionString)
  {
    var options = new DbContextOptionsBuilder<CompassDbContext>()
      .UseSqlServer(connectionString)
      .Options;
    return new CompassDbContext(options);
  }

  private bool IsFeatureEnabled(bool currentIsProduction, out string? reason)
  {
    if (_options.Enabled)
    {
      reason = null;
      return true;
    }

    if (_configuration.GetValue("isDev", false) || _hostEnvironment.IsDevelopment())
    {
      reason = null;
      return true;
    }

    reason = "Environment sync is only available from the development instance.";
    if (currentIsProduction)
      reason = "Environment sync is disabled on production. Use the development instance to sync data.";

    return false;
  }

  private bool IsProductionCatalog(string catalog) =>
    _options.ProductionCatalogNames.Any(n =>
      string.Equals(n, catalog, StringComparison.OrdinalIgnoreCase));

  internal static string ParseCatalogName(string connectionString)
  {
    var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
    if (builder.TryGetValue("Initial Catalog", out var catalog) && catalog != null)
      return catalog.ToString() ?? "unknown";
    if (builder.TryGetValue("Database", out catalog) && catalog != null)
      return catalog.ToString() ?? "unknown";

    var match = Regex.Match(connectionString, @"Initial Catalog=([^;]+)", RegexOptions.IgnoreCase);
    return match.Success ? match.Groups[1].Value : "unknown";
  }

  private static string GetConfirmationPhrase(EnvironmentSyncDirection direction, string targetCatalog) =>
    direction switch
    {
      EnvironmentSyncDirection.DevToProdServiceRegister => $"SYNC SERVICE REGISTER TO {targetCatalog.ToUpperInvariant()}",
      EnvironmentSyncDirection.ProdToDevWorkRaid => $"SYNC WORK RAID FROM PRODUCTION TO {targetCatalog.ToUpperInvariant()}",
      _ => throw new InvalidOperationException("Unsupported sync direction.")
    };
}
