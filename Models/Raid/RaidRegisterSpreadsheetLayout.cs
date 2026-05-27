using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>Admin-configured default column order for a RAID register spreadsheet entity tab.</summary>
public class RaidRegisterSpreadsheetLayout
{
    public int Id { get; set; }

    [Required]
    [StringLength(32)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    public string ColumnOrderJson { get; set; } = "[]";

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedByUserId { get; set; }

    public User? UpdatedByUser { get; set; }
}
