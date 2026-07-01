namespace Compass.Configuration;

public sealed class EnvironmentSyncOptions
{
    public const string SectionName = "EnvironmentSync";

    /// <summary>When false, the admin sync UI and API are disabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Connection string for the peer database. When empty, Development loads
    /// <c>appsettings.Production.json</c> and Production loads <c>appsettings.Development.json</c>.
    /// </summary>
    public string? PeerConnectionString { get; set; }

    /// <summary>Catalog names treated as production targets (writes restricted to service register only).</summary>
    public string[] ProductionCatalogNames { get; set; } = ["compass_v2", "compass"];
}
