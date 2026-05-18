using System.ComponentModel.DataAnnotations;
using Compass.Models;

namespace Compass.ViewModels.Modern;

public class ServiceLineListViewModel
{
    public IReadOnlyList<ServiceLineListRow> Rows { get; init; } = Array.Empty<ServiceLineListRow>();
    public bool CanEdit { get; init; }
}

public class ServiceLineListRow
{
    public Guid Id { get; init; }
    public string Slug { get; init; } = "";
    public string Name { get; init; } = "";
    public int Directorates { get; init; }
    public int BusinessAreas { get; init; }
    public int Products { get; init; }
    public int WorkItems { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public class ServiceLineFormInput
{
    [Required(ErrorMessage = "Enter a name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }
    public int[]? DivisionIds { get; set; }
    public int[]? BusinessAreaLookupIds { get; set; }
    public Guid[]? ProductIds { get; set; }
    public int[]? ProjectIds { get; set; }
}

public class ServiceLineFormViewModel
{
    public bool IsNew { get; init; }
    public Guid? Id { get; init; }
    public string? Slug { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<int> SelectedDivisionIds { get; init; } = Array.Empty<int>();
    public IReadOnlyList<int> SelectedBusinessAreaLookupIds { get; init; } = Array.Empty<int>();
    public IReadOnlyList<Guid> SelectedProductIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<int> SelectedProjectIds { get; init; } = Array.Empty<int>();

    // Options for the form
    public IReadOnlyList<ServiceLineFormOption> DivisionOptions { get; init; } = Array.Empty<ServiceLineFormOption>();
    public IReadOnlyList<ServiceLineFormOption> BusinessAreaOptions { get; init; } = Array.Empty<ServiceLineFormOption>();

    /// <summary>Pre-selected FIPS products (id + label) for the removable list; search uses an API, not a full &lt;select&gt;.</summary>
    public IReadOnlyList<ServiceLinePickedItem> InitialProducts { get; init; } = Array.Empty<ServiceLinePickedItem>();

    /// <summary>Pre-selected work items for the removable list.</summary>
    public IReadOnlyList<ServiceLinePickedItem> InitialWork { get; init; } = Array.Empty<ServiceLinePickedItem>();
}

public class ServiceLinePickedItem
{
    public string Id { get; init; } = "";
    public string Label { get; init; } = "";
    /// <summary>Optional secondary line (e.g. WI-00000123 for work items).</summary>
    public string? Subtitle { get; init; }
}

public class ServiceLineFormOption
{
    public string Value { get; init; } = "";
    public string Text { get; init; } = "";
}

public class ServiceLineDetailViewModel
{
    public ServiceLine ServiceLine { get; init; } = null!;
    public IReadOnlyList<Division> Divisions { get; init; } = Array.Empty<Division>();
    public IReadOnlyList<BusinessAreaLookup> BusinessAreas { get; init; } = Array.Empty<BusinessAreaLookup>();
    public IReadOnlyList<Compass.Models.Fips.CMDBProduct> Products { get; init; } = Array.Empty<Compass.Models.Fips.CMDBProduct>();
    public IReadOnlyList<Project> Projects { get; init; } = Array.Empty<Project>();
    public bool CanEdit { get; init; }
}
