namespace Compass.Models.Modern;

/// <summary>
/// URL segments for the modern UI shell. Controllers use attribute routing under <c>/modern/...</c>.
/// </summary>
public static class ModernShellRoutes
{
    public const string Prefix = "modern";

    public const string Dashboard = $"{Prefix}/dashboard";
}
