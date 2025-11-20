namespace Compass.Models;

public class DuplicatePhasesViewModel
{
    public List<ProductWithDuplicatePhases> Products { get; set; } = new();
    public int TotalProductsWithDuplicatePhases { get; set; }
    public int TotalProducts { get; set; }
}

public class ProductWithDuplicatePhases
{
    public string FipsId { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    public string State { get; set; } = "New";
    public List<PhaseInfo> Phases { get; set; } = new();
    public int PhaseCount { get; set; }
    public int ContactsCount { get; set; }
    public string? ProductUrl { get; set; }
}

public class PhaseInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? SortOrder { get; set; }
}

