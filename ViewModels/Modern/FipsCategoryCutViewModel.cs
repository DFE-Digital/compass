namespace Compass.ViewModels.Modern;

/// <summary>Active or Enterprise services sliced by one catalogue dimension.</summary>
public sealed class FipsCategoryCutViewModel
{
    public string Scope { get; set; } = "active";
    public string Cut { get; set; } = "business-area";
    public string ScopeLabel { get; set; } = "Active";
    public string CutLabel { get; set; } = "Business area";
    public string CutNoun { get; set; } = "business area";

    public int ServiceCount { get; set; }
    public int ValueCount { get; set; }
    public int UnassignedCount { get; set; }
    public decimal DataQualityPercent { get; set; }
    public string WeakestField { get; set; } = "";
    public decimal WeakestFieldPercent { get; set; }

    public List<FipsCategoryFieldRow> Fields { get; set; } = new();
    public List<FipsCategorySliceRow> Slices { get; set; } = new();

    public FipsCategorySliceRow? Selected { get; set; }
    public List<FipsCategoryFieldRow> SelectedFields { get; set; } = new();
    public List<FipsCategoryServiceRow> Services { get; set; } = new();
}

public sealed class FipsCategoryFieldRow
{
    public string Field { get; set; } = "";
    public string Hint { get; set; } = "";
    public int Complete { get; set; }
    public int Missing { get; set; }
    public decimal CompletionPercent { get; set; }
}

public sealed class FipsCategorySliceRow
{
    /// <summary><c>none</c> for services with no value, otherwise the lookup id.</summary>
    public string ValueKey { get; set; } = "";
    public string Name { get; set; } = "";
    public bool IsUnassigned { get; set; }
    public int ServiceCount { get; set; }
    public decimal SharePercent { get; set; }
    public decimal DataQualityPercent { get; set; }
    public int MissingServiceOwner { get; set; }
    public int MissingInformationAssetOwner { get; set; }
    public int MissingPhase { get; set; }
    public string MainGap { get; set; } = "";
}

public sealed class FipsCategoryServiceRow
{
    public Guid ProductId { get; set; }
    public int UniqueId { get; set; }
    public string Title { get; set; } = "";
    public string Phase { get; set; } = "";
    public int CompletionPercent { get; set; }
    public string MissingFields { get; set; } = "";
}
