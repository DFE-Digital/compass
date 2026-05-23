namespace Compass.ViewModels.Modern;

/// <summary>Service register reporting summary built from CMDB product data.</summary>
public sealed class ModernServiceRegisterReportViewModel
{
    /// <summary>Summary metrics are calculated for <strong>active</strong> products only.</summary>
    public int ActiveTotalProducts { get; set; }
    public decimal ActiveOverallCompletionPercent { get; set; }
    public int ActiveFullyCompleteCount { get; set; }
    public int ActiveProductsWithoutUrlCount { get; set; }
    public int ActiveProductsWithoutServiceOwnerOrSroCount { get; set; }

    public int ActiveCount { get; set; }
    public int RejectedCount { get; set; }
    public int RetiredCount { get; set; }
    public int NewCount { get; set; }
    public int EnterpriseCount { get; set; }

    public List<ServiceRegisterAreaSummaryRow> DirectorateRows { get; set; } = new();
    public List<ServiceRegisterAreaSummaryRow> BusinessAreaRows { get; set; } = new();

    public List<ServiceRegisterProductCompletionRow> ActiveCompletionRows { get; set; } = new();
    public List<ServiceRegisterProductCompletionRow> EnterpriseCompletionRows { get; set; } = new();
    public List<ServiceRegisterProductCompletionRow> RetiredCompletionRows { get; set; } = new();
    public List<ServiceRegisterProductCompletionRow> NewCompletionRows { get; set; } = new();
    public List<ServiceRegisterProductCompletionRow> RejectedCompletionRows { get; set; } = new();
}

public sealed class ServiceRegisterAreaSummaryRow
{
    public string Name { get; set; } = "Not set";
    public int ProductCount { get; set; }
    public int ActiveCount { get; set; }
    public int RejectedCount { get; set; }
    public int RetiredCount { get; set; }
    public int NewCount { get; set; }
    public int EnterpriseCount { get; set; }
    public decimal AverageCompletionPercent { get; set; }
}

public sealed class ServiceRegisterProductCompletionRow
{
    public Guid ProductId { get; set; }
    public int UniqueId { get; set; }
    public string Title { get; set; } = "";
    public string StatusLabel { get; set; } = "No status";
    public bool IsEnterprise { get; set; }

    public int CompletionPercent { get; set; }
    public string MissingFields { get; set; } = "None";

    public bool HasProductUrl { get; set; }
    public string? ProductUrl { get; set; }
    public int ContactCount { get; set; }
    public bool HasPhase { get; set; }
    public bool HasBusinessArea { get; set; }
    public bool HasChannel { get; set; }
    public bool HasUserGroup { get; set; }
    public bool HasType { get; set; }
    public bool HasServiceOwner { get; set; }
    public bool HasSeniorResponsibleOfficer { get; set; }
    public string MissingOwnerOrSro { get; set; } = "None";

    public string PhaseDisplay { get; set; } = "Not set";
    public string ChannelsDisplay { get; set; } = "Not set";
    public string TypesDisplay { get; set; } = "Not set";

    public string BusinessAreasDisplay { get; set; } = "Not set";
    public string DirectoratesDisplay { get; set; } = "Not set";
    public List<string> BusinessAreaNames { get; set; } = new();
    public List<string> DirectorateNames { get; set; } = new();
}
