namespace Compass.Models;

public class FipsCompletionViewModel
{
    public List<ProductCompletionItem> Products { get; set; } = new();
    public double AverageCompletionPercentage { get; set; }
    public List<FipsBusinessAreaCompletion> BusinessAreaCompletions { get; set; } = new();
    public int ZeroCompletionCount { get; set; }
    public int ProductsWithContactCount { get; set; }
    public int CompletedPhaseCount { get; set; }
    public int CompletedBusinessAreaCount { get; set; }
    public int CompletedUrlCount { get; set; }
    public int CompletedSroCount { get; set; }
    public int CompletedServiceOwnerCount { get; set; }
    public int CompletedTypeCount { get; set; }
}

public class ProductCompletionItem
{
    public string FipsId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string? CmdbSysId { get; set; }
    public string ProductTitle { get; set; } = string.Empty;
    public string BusinessArea { get; set; } = string.Empty;
    public string? PhaseName { get; set; }
    public string State { get; set; } = "New";
    public string? SeniorResponsibleOfficer { get; set; }
    public string? InformationAssetOwner { get; set; }
    public string? DeliveryManager { get; set; }
    public string? ServiceOwner { get; set; }
    public string? ProductUrl { get; set; }
    public List<string> SeniorResponsibleOfficerContacts { get; set; } = new();
    public List<string> InformationAssetOwnerContacts { get; set; } = new();
    public List<string> DeliveryManagerContacts { get; set; } = new();
    public List<string> ServiceOwnerContacts { get; set; } = new();
    public List<string> ProductManagerContacts { get; set; } = new();
    public List<string> ContactDetails { get; set; } = new();
    public List<string> UserGroupNames { get; set; } = new();
    public List<int> UserGroupCategoryValueIds { get; set; } = new();
    public List<string> ChannelNames { get; set; } = new();
    public List<string> TypeNames { get; set; } = new();
    
    // Completion criteria
    public bool HasPhase { get; set; }
    public bool HasBusinessArea { get; set; }
    public int ContactsCount { get; set; }
    public bool HasProductUrl { get; set; }
    public int UserGroupsCount { get; set; }
    
    // Calculated completion percentage (0-100)
    public double CompletionPercentage { get; set; }
}

public class FipsBusinessAreaCompletion
{
    public string BusinessArea { get; set; } = string.Empty;
    public int ProductCount { get; set; }
    public double AverageCompletionPercentage { get; set; }
}

