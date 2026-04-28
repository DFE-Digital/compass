using System.Text.Json.Serialization;

namespace Compass.Services.Aiss;

/// <summary>Deserialised response from AISS <c>GET /api/v1/summary</c>.</summary>
public sealed class AissPlatformSummary
{
    public DateTimeOffset? GeneratedAtUtc { get; set; }
    public string? Scope { get; set; }
    public AissIssuesBlock? Issues { get; set; }
    public AissServicesBlock? Services { get; set; }
    public AissCompassBlock? Compass { get; set; }
    public AissIssueCriteriaBlock? IssueCriteria { get; set; }
    public List<AissByBusinessAreaRow>? ByBusinessArea { get; set; }
    public List<AissByServiceRow>? ByService { get; set; }
}

public sealed class AissCompassBlock
{
    public string? Error { get; set; }
    public int ActiveProductCount { get; set; }
    public int ProductsMatchingOnboardedServices { get; set; }
    public int ProductsNotOnboardedInThisApp { get; set; }
    public int ServicesWithNoCompassProductMatch { get; set; }
}

public sealed class AissIssuesBlock
{
    public int Total { get; set; }
    public int Open { get; set; }
    public int Closed { get; set; }
    public int Overdue { get; set; }
}

public sealed class AissServicesBlock
{
    public int Onboarded { get; set; }
    public int Installed { get; set; }
    [JsonPropertyName("excludedOnboardedNotInCompassRegister")]
    public int ExcludedOnboardedNotInCompassRegister { get; set; }
}

public sealed class AissIssueCriteriaBlock
{
    [JsonPropertyName("a")] public AissCriterionTrio? A { get; set; }
    [JsonPropertyName("aa")] public AissCriterionTrio? Aa { get; set; }
    [JsonPropertyName("aaa")] public AissCriterionTrio? Aaa { get; set; }
    [JsonPropertyName("bp")] public AissCriterionTrio? Bp { get; set; }
    [JsonPropertyName("ux")] public AissCriterionTrio? Ux { get; set; }
    [JsonPropertyName("other")] public AissCriterionTrio? Other { get; set; }
}

public sealed class AissCriterionTrio
{
    public int Open { get; set; }
    public int Closed { get; set; }
    public int Overdue { get; set; }
}

public sealed class AissByServiceRow
{
    public int ServiceId { get; set; }
    public string? Name { get; set; }
    public string? DocumentId { get; set; }
    public int IssuesOpen { get; set; }
    public AissOpenByCriterion? OpenByCriterion { get; set; }
}

/// <summary>Aggregated accessibility issues for one FIPS business area label (from service register), from AISS summary.</summary>
public sealed class AissByBusinessAreaRow
{
    public string? BusinessArea { get; set; }
    public int Open { get; set; }
    public int Overdue { get; set; }
    public int Closed { get; set; }
    public int Total { get; set; }
    public AissIssueCriteriaBlock? IssueCriteria { get; set; }
}

public sealed class AissOpenByCriterion
{
    [JsonPropertyName("a")] public int A { get; set; }
    [JsonPropertyName("aa")] public int Aa { get; set; }
    [JsonPropertyName("aaa")] public int Aaa { get; set; }
    [JsonPropertyName("bp")] public int Bp { get; set; }
    [JsonPropertyName("ux")] public int Ux { get; set; }
    [JsonPropertyName("other")] public int Other { get; set; }
}

/// <summary>Deserialised response from AISS <c>GET /api/v1/summary/trends</c>.</summary>
public sealed class AissCriterionTrends
{
    public List<string> MonthLabels { get; set; } = new();
    public Dictionary<string, List<int>>? OpenAtMonthEnd { get; set; }
    public Dictionary<string, List<int>>? ClosedInMonth { get; set; }
}
