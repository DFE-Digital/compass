namespace Compass.Services.Api;

public static class ApiTokenResourceCatalog
{
    public static readonly string[] Resources =
    {
        "Risks", "Issues", "Milestones", "PerformanceMetrics",
        "EnterpriseMetrics", "FunctionalStandards", "DdtStandards",
        "ServiceRegister",
        "CmsAccessRequests",
        "AdminLookups"
    };

    public static Dictionary<string, (bool read, bool create, bool update, bool delete)> ReadOnlyAllData()
    {
        return Resources.ToDictionary(r => r, _ => (read: true, create: false, update: false, delete: false));
    }

    public static bool IsReadOnlyAllData(Dictionary<string, (bool read, bool create, bool update, bool delete)> permissions)
    {
        foreach (var resource in Resources)
        {
            if (!permissions.TryGetValue(resource, out var p))
                return false;
            if (!p.read || p.create || p.update || p.delete)
                return false;
        }
        return true;
    }

    public static bool HasDelete(Dictionary<string, (bool read, bool create, bool update, bool delete)> permissions) =>
        permissions.Values.Any(p => p.delete);

    public static bool HasWrite(Dictionary<string, (bool read, bool create, bool update, bool delete)> permissions) =>
        permissions.Values.Any(p => p.create || p.update);
}
