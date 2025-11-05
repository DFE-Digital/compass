namespace Compass.Models;

public class MultipleBusinessAreasViewModel
{
    public List<ProductWithMultipleBusinessAreas> Products { get; set; } = new();
    public int TotalProductsWithMultipleBusinessAreas { get; set; }
    public int TotalProducts { get; set; }
}

public class ProductWithMultipleBusinessAreas
{
    public string FipsId { get; set; } = string.Empty;
    public string ProductTitle { get; set; } = string.Empty;
    public string State { get; set; } = "New";
    public List<string> BusinessAreas { get; set; } = new();
    public int BusinessAreaCount { get; set; }
    public int ContactsCount { get; set; }
    public string? ProductUrl { get; set; }
}

