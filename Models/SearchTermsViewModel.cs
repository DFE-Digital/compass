namespace Compass.Models;

public class SearchTermsViewModel
{
    public List<SearchTerm> SearchTerms { get; set; } = new();
    public List<DistinctSearchTerm> DistinctSearchTerms { get; set; } = new();
    public int TotalSearchTerms { get; set; }
    public int DistinctSearchTermsCount { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalSearchTerms / (double)PageSize) : 1;
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public class DistinctSearchTerm
{
    public string SearchTerm { get; set; } = string.Empty;
    public int Count { get; set; }
    public double AverageResultCount { get; set; }
}
