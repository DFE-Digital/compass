namespace Compass.Services.ServiceSchema;

/// <summary>Roles from the Government Digital and Data Profession Capability Framework.</summary>
public static class DdatStaffRoleCatalog
{
    public static IReadOnlyList<(string Family, string Name)> Roles { get; } =
    [
        Role("Architecture", "Business architect"),
        Role("Architecture", "Data architect"),
        Role("Architecture", "Enterprise architect"),
        Role("Architecture", "Network architect"),
        Role("Architecture", "Security architect"),
        Role("Architecture", "Solution architect"),
        Role("Architecture", "Technical architect"),
        Role("Chief digital and data", "Chief data officer"),
        Role("Chief digital and data", "Chief digital and information officer"),
        Role("Chief digital and data", "Chief information security officer"),
        Role("Chief digital and data", "Chief technology officer"),
        Role("Data", "Analytics engineer"),
        Role("Data", "Data analyst"),
        Role("Data", "Data and artificial intelligence (AI) ethicist"),
        Role("Data", "Data engineer"),
        Role("Data", "Data governance manager"),
        Role("Data", "Data scientist"),
        Role("Data", "Digital evaluator"),
        Role("Data", "Machine learning engineer"),
        Role("Data", "Performance analyst"),
        Role("IT operations", "Application operations engineer"),
        Role("IT operations", "Business relationship manager"),
        Role("IT operations", "Change and release manager"),
        Role("IT operations", "Command and control centre manager"),
        Role("IT operations", "End user computing engineer"),
        Role("IT operations", "Incident manager"),
        Role("IT operations", "Infrastructure engineer"),
        Role("IT operations", "Infrastructure operations engineer"),
        Role("IT operations", "IT service manager"),
        Role("IT operations", "Problem manager"),
        Role("IT operations", "Service desk manager"),
        Role("IT operations", "Service transition manager"),
        Role("Product and delivery", "Agile coach"),
        Role("Product and delivery", "Business analyst"),
        Role("Product and delivery", "Delivery manager"),
        Role("Product and delivery", "Digital portfolio manager"),
        Role("Product and delivery", "Product manager"),
        Role("Product and delivery", "Programme delivery manager"),
        Role("Product and delivery", "Service owner"),
        Role("Quality assurance testing", "Quality assurance test analyst"),
        Role("Quality assurance testing", "Test engineer"),
        Role("Quality assurance testing", "Test manager"),
        Role("Software development", "Development operations (DevOps) engineer"),
        Role("Software development", "Frontend developer"),
        Role("Software development", "Software developer"),
        Role("User-centred design", "Accessibility specialist"),
        Role("User-centred design", "Content designer"),
        Role("User-centred design", "Content strategist"),
        Role("User-centred design", "Graphic designer"),
        Role("User-centred design", "Interaction designer"),
        Role("User-centred design", "Service designer"),
        Role("User-centred design", "Technical writer"),
        Role("User-centred design", "User researcher")
    ];

    public static string CodeFor(string family, string name)
    {
        var raw = $"{family}-{name}".ToLowerInvariant();
        var chars = raw.Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray();
        var slug = new string(chars);
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        return slug.Trim('-');
    }

    private static (string Family, string Name) Role(string family, string name) => (family, name);
}
