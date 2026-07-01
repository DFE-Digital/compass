namespace Compass.Services.Fips;

public static class FipsProductInformationFieldKeys
{
    public const string Description = "description";
    public const string Phase = "phase";
    public const string Directorate = "directorate";
    public const string BusinessArea = "business-area";
    public const string Channel = "channel";
    public const string UserGroup = "user-group";
    public const string Type = "type";
    public const string EnterpriseService = "enterprise-service";
    public const string ProductUrl = "product-url";

    public static string Categorisation(int groupId) => $"categorisation-{groupId}";

    public static string CategorisationElementId(int groupId) => $"edit-categorisation-{groupId}";

    public static bool TryParseCategorisationGroupId(string field, out int groupId)
    {
        groupId = 0;
        if (!field.StartsWith("categorisation-", StringComparison.OrdinalIgnoreCase))
            return false;
        return int.TryParse(field["categorisation-".Length..], out groupId) && groupId > 0;
    }

    public static string HashForField(string field) => "edit-" + field;

    public static string TitleForField(string field, string? groupName = null) => field switch
    {
        Description => "Change description",
        Phase => "Change phase",
        Directorate => "Change directorate",
        BusinessArea => "Change business area",
        Channel => "Change channel",
        UserGroup => "Change user group",
        Type => "Change type",
        EnterpriseService => "Change enterprise service",
        ProductUrl => "Change product URL",
        _ when TryParseCategorisationGroupId(field, out _) =>
            string.IsNullOrWhiteSpace(groupName) ? "Change categorisation" : $"Change {groupName}",
        _ => "Change"
    };
}
