namespace Compass.Services.Raid;

public sealed record RaidSpreadsheetColumnInfo(string Key, string Label);

/// <summary>Built-in register spreadsheet columns (markup defaults) and display labels.</summary>
public static class RaidRegisterSpreadsheetColumnCatalog
{
    public const string EntityRisk = "risk";
    public const string EntityIssue = "issue";
    public const string EntityAssumption = "assumption";
    public const string EntityNearMiss = "nearmiss";

    public static readonly IReadOnlyList<string> EntityTypes =
        [EntityRisk, EntityIssue, EntityAssumption, EntityNearMiss];

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<RaidSpreadsheetColumnInfo>> ColumnsByEntity =
        new Dictionary<string, IReadOnlyList<RaidSpreadsheetColumnInfo>>(StringComparer.OrdinalIgnoreCase)
        {
            [EntityRisk] =
            [
                new("ref", "Ref"),
                new("title", "Title"),
                new("status", "Status"),
                new("tier", "Tier"),
                new("relation", "Relation"),
                new("category", "Category"),
                new("owner", "Owner"),
                new("description", "Description"),
                new("cause", "Cause"),
                new("impact", "Impact"),
                new("contingency", "Contingency"),
                new("assurance", "Assurance"),
                new("financialImpact", "Financial impact"),
                new("kris", "KRIs"),
                new("response", "Response strategy"),
                new("mitigations", "Mitigations"),
                new("origImpact", "Inherent impact"),
                new("origLikelihood", "Inherent likelihood"),
                new("origScore", "Inherent score"),
                new("currImpact", "Curr. impact"),
                new("currLikelihood", "Curr. likelihood"),
                new("currScore", "Curr. score"),
                new("residImpact", "Resid. impact"),
                new("residLikelihood", "Resid. likelihood"),
                new("residScore", "Resid. score"),
                new("tolImpact", "Tol. impact"),
                new("tolLikelihood", "Tol. likelihood"),
                new("tolScore", "Tol. score"),
                new("proximity", "Proximity"),
                new("createdDate", "Created date"),
                new("lastEdited", "Last edited"),
            ],
            [EntityIssue] =
            [
                new("ref", "Ref"),
                new("title", "Title"),
                new("status", "Status"),
                new("severity", "Severity"),
                new("priority", "Priority"),
                new("relation", "Relation"),
                new("category", "Category"),
                new("owner", "Owner"),
                new("description", "Description"),
                new("identified", "Identified"),
                new("targetDate", "Target date"),
                new("lastEdited", "Last edited"),
            ],
            [EntityAssumption] =
            [
                new("ref", "Ref"),
                new("description", "Description"),
                new("status", "Status"),
                new("criticality", "Criticality"),
                new("relation", "Relation"),
                new("owner", "Owner"),
                new("createdDate", "Created date"),
                new("lastEdited", "Last edited"),
            ],
            [EntityNearMiss] =
            [
                new("ref", "Ref"),
                new("impact", "Impact"),
                new("status", "Status"),
                new("seriousness", "Seriousness"),
                new("type", "Type"),
                new("scope", "Scope"),
                new("dateLogged", "Date logged"),
                new("lastEdited", "Last edited"),
            ],
        };

    public static string GetEntityLabel(string entityType) => entityType.ToLowerInvariant() switch
    {
        EntityRisk => "Risks",
        EntityIssue => "Issues",
        EntityAssumption => "Assumptions",
        EntityNearMiss => "Near misses",
        _ => entityType
    };

    public static IReadOnlyList<RaidSpreadsheetColumnInfo> GetColumns(string entityType)
    {
        if (ColumnsByEntity.TryGetValue(entityType, out var cols))
            return cols;
        return Array.Empty<RaidSpreadsheetColumnInfo>();
    }

    public static IReadOnlyList<string> GetBuiltInColumnOrder(string entityType) =>
        GetColumns(entityType).Select(c => c.Key).ToList();

    public static bool IsKnownEntityType(string entityType) =>
        ColumnsByEntity.ContainsKey(entityType);

    public static IReadOnlyList<string> NormalizeColumnOrder(string entityType, IEnumerable<string>? savedOrder)
    {
        var builtIn = GetBuiltInColumnOrder(entityType);
        var builtInSet = new HashSet<string>(builtIn, StringComparer.OrdinalIgnoreCase);
        var result = new List<string>();

        if (savedOrder != null)
        {
            foreach (var col in savedOrder)
            {
                var key = (col ?? "").Trim();
                if (key.Length == 0 || !builtInSet.Contains(key)) continue;
                if (result.Contains(key, StringComparer.OrdinalIgnoreCase)) continue;
                result.Add(builtIn.First(b => string.Equals(b, key, StringComparison.OrdinalIgnoreCase)));
            }
        }

        foreach (var col in builtIn)
        {
            if (!result.Contains(col, StringComparer.OrdinalIgnoreCase))
                result.Add(col);
        }

        return result;
    }
}
