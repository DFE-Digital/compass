using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels.Modern;

public sealed class HttpErrorEmailSettingsViewModel
{
    public bool IsEnabled { get; set; }

    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    [MaxLength(256)]
    [Display(Name = "Contact email to send to")]
    public string? ContactEmail { get; set; }
}
