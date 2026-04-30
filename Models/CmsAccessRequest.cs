using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Compass.Models;

/// <summary>Request for access to a CMS (e.g. Design histories, DDT manual), processed in Operations.</summary>
public class CmsAccessRequest
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string CmsName { get; set; } = "";

    [Required]
    [MaxLength(2000)]
    public string SignInPageUrl { get; set; } = "";

    [Required]
    [MaxLength(256)]
    public string RequestorEmail { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string RequestorFirstName { get; set; } = "";

    [Required]
    [MaxLength(100)]
    public string RequestorLastName { get; set; } = "";

    [Required]
    public DateTime DateRequested { get; set; }

    /// <summary>Granted or Rejected — set when Operations completes the request.</summary>
    [MaxLength(20)]
    public string? Outcome { get; set; }

    public bool PublisherAccessRequired { get; set; }

    [MaxLength(4000)]
    public string? Comments { get; set; }

    public int? ActionedByUserId { get; set; }

    [ForeignKey(nameof(ActionedByUserId))]
    public User? ActionedByUser { get; set; }

    /// <summary>Registration / password-setup token or full URL for the requester email.</summary>
    [MaxLength(2000)]
    public string? RegistrationToken { get; set; }

    /// <summary>New, Completed, or Rejected.</summary>
    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "New";
}
