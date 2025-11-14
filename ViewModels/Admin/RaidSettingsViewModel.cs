using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels.Admin;

public class RaidLookupListViewModel
{
    public string CurrentLookupKey { get; set; } = string.Empty;
    public string CurrentLookupLabel { get; set; } = string.Empty;
    public string? CurrentLookupDescription { get; set; }
    public IReadOnlyList<RaidLookupSelectorViewModel> Lookups { get; set; } = Array.Empty<RaidLookupSelectorViewModel>();
    public IReadOnlyList<RaidLookupListItemViewModel> Items { get; set; } = Array.Empty<RaidLookupListItemViewModel>();
    public RaidLookupEditInputModel NewEntry { get; set; } = new();
    public RaidLookupEditInputModel? EditEntry { get; set; }
}

public class RaidLookupSelectorViewModel
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class RaidLookupListItemViewModel
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; }
}

public class RaidLookupEditInputModel
{
    public int? Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string LookupKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Label { get; set; } = string.Empty;

    [MaxLength(450)]
    public string? Description { get; set; }

    public int SortOrder { get; set; }

    public bool IsActive { get; set; } = true;
}
