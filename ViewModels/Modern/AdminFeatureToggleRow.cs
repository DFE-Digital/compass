using Compass.Models;

namespace Compass.ViewModels.Modern;

/// <summary>One row on the Feature settings admin panel.</summary>
public class AdminFeatureToggleRow
{
    public string Code { get; set; } = "";

    public string Label { get; set; } = "";

    public string? Hint { get; set; }

    public FeatureAccessMode AccessMode { get; set; } = FeatureAccessMode.OnForAll;

    /// <summary>Slots for <see cref="FeatureAccessMode.OnForSome"/>; user ids &gt; 0 are kept on save.</summary>
    public List<AdminFeatureAllowUserSlot> AllowSlots { get; set; } = new();

    /// <summary>Group slots; ids &gt; 0 are kept. Members of any listed group (see Admin → Groups) can access the feature when mode is on for some.</summary>
    public List<AdminFeatureAllowGroupSlot> GroupSlots { get; set; } = new();
}

public class AdminFeatureAllowUserSlot
{
    public int? UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
}

public class AdminFeatureAllowGroupSlot
{
    public int? GroupId { get; set; }
    public string? Name { get; set; }
}

/// <summary>Options for the feature-settings “group” dropdowns (active directory groups from Admin → Groups).</summary>
public class AdminFeatureSettingsGroupOption
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}
