namespace Compass.ViewModels.Modern;

public class AdminRaidRegisterTableViewPanel
{
    public string ActiveEntityType { get; set; } = "risk";
    public IReadOnlyList<AdminRaidRegisterTableEntityTab> Tabs { get; set; } = Array.Empty<AdminRaidRegisterTableEntityTab>();
    public IReadOnlyList<AdminRaidRegisterTableColumnRow> Columns { get; set; } = Array.Empty<AdminRaidRegisterTableColumnRow>();
    public bool HasCustomLayout { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedByName { get; set; }
}

public class AdminRaidRegisterTableEntityTab
{
    public string EntityType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public bool HasCustomLayout { get; set; }
}

public class AdminRaidRegisterTableColumnRow
{
    public string Key { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}
