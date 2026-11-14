namespace Compass.Services.Fips;

/// <summary>FIPS product pages that manage census-submitted register information after the census is complete.</summary>
public static class FipsCensusManageTabs
{
    public static readonly FipsCensusManageTab[] Items =
    [
        new("purpose", "purpose", "Purpose"),
        new("users", "users", "Users"),
        new("ownership", "ownership", "Additional contacts"),
        new("functionality", "functionality", "Functionality"),
        new("technology", "technology", "Technology"),
        new("data", "data", "Data"),
        new("metrics", "performance", "Metrics and KPIs"),
        new("evidence", "evidence", "Evidence")
    ];

    public static bool IsManageTab(string? tab) =>
        Items.Any(i => string.Equals(i.Tab, tab, StringComparison.OrdinalIgnoreCase));

    public static string? SectionKey(string? tab) =>
        Items.FirstOrDefault(i => string.Equals(i.Tab, tab, StringComparison.OrdinalIgnoreCase))?.SectionKey;

    public static string TabForSection(string? sectionKey)
    {
        if (string.IsNullOrWhiteSpace(sectionKey))
            return "purpose";
        var match = Items.FirstOrDefault(i =>
            string.Equals(i.SectionKey, sectionKey, StringComparison.OrdinalIgnoreCase)
            || string.Equals(i.Tab, sectionKey, StringComparison.OrdinalIgnoreCase));
        return match?.Tab ?? "purpose";
    }
}

public sealed record FipsCensusManageTab(string Tab, string SectionKey, string NavLabel);
