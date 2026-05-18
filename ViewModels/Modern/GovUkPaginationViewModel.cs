using System.Linq;

namespace Compass.ViewModels.Modern;

/// <summary>Parameters for GOV.UK Design System pagination (<c>govuk-pagination</c>).</summary>
public sealed class GovUkPaginationViewModel
{
    public required int CurrentPage { get; init; }
    public required int TotalPages { get; init; }
    /// <summary>URL for the given 1-based page index.</summary>
    public required Func<int, string> PageUrl { get; init; }
    public string NavigationAriaLabel { get; init; } = "Pagination";
}

/// <summary>Builds page numbers and ellipsis gaps for GOV.UK pagination lists.</summary>
public static class GovUkPagination
{
    /// <returns><see langword="null"/> entries represent ellipsis between page numbers.</returns>
    public static IReadOnlyList<int?> PageSequence(int currentPage, int totalPages)
    {
        if (totalPages < 2)
            return Array.Empty<int?>();
        if (totalPages <= 7)
            return Enumerable.Range(1, totalPages).Select(i => (int?)i).ToList();

        var set = new SortedSet<int> { 1, totalPages };
        for (var i = currentPage - 1; i <= currentPage + 1; i++)
        {
            if (i >= 1 && i <= totalPages)
                set.Add(i);
        }

        var list = new List<int?>();
        var prev = 0;
        foreach (var p in set)
        {
            if (prev > 0 && p - prev > 1)
                list.Add(null);
            list.Add(p);
            prev = p;
        }

        return list;
    }
}
