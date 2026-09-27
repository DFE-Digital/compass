using System.ComponentModel.DataAnnotations;
using Compass.Models;

namespace Compass.ViewModels.Modern;

public class CapabilityListViewModel
{
    public bool CanManage { get; set; }
    public IReadOnlyList<CapabilityRowViewModel> Items { get; set; } =
        Array.Empty<CapabilityRowViewModel>();
}

public class CapabilityRowViewModel
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Reference { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
    public string DisplayLabel => $"{Title} ({Reference})";
}

public class CapabilityEditViewModel
{
    public Guid? Id { get; set; }
    public bool IsCreate { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, MaxLength(100)]
    public string Reference { get; set; } = string.Empty;

    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

public static class CapabilityDisplay
{
    public static string Format(string title, string reference) =>
        $"{title.Trim()} ({reference.Trim()})";

    public static string Format(CapabilityLookup entity) =>
        Format(entity.Title, entity.Reference);
}
