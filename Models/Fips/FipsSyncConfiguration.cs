namespace Compass.Models.Fips;

/// <summary>
/// Configuration for FIPS sync operations
/// </summary>
public class FipsSyncConfiguration
{
    public CmdbConfiguration Cmdb { get; set; } = new();
    public StrapiEnvironments Strapi { get; set; } = new();
    public SasConfiguration Sas { get; set; } = new();
    public AissConfiguration Aiss { get; set; } = new();
}

public class CmdbConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class StrapiEnvironments
{
    public StrapiEnvironmentConfig Development { get; set; } = new();
    public StrapiEnvironmentConfig Test { get; set; } = new();
    public StrapiEnvironmentConfig Production { get; set; } = new();
}

public class StrapiEnvironmentConfig
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}

public class SasConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
}

public class AissConfiguration
{
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
}
