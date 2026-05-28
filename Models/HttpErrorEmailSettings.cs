using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

/// <summary>Singleton configuration for emailing HTTP 405/500 errors via GOV.UK Notify.</summary>
public class HttpErrorEmailSettings
{
    public const int SingletonId = 1;

    public int Id { get; set; } = SingletonId;

    public bool IsEnabled { get; set; }

    [MaxLength(256)]
    [EmailAddress]
    public string? ContactEmail { get; set; }

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(256)]
    public string? UpdatedByEmail { get; set; }
}
