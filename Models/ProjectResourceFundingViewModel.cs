using System.ComponentModel.DataAnnotations;

namespace Compass.Models;

public class ProjectResourceFundingViewModel
{
    public int ProjectId { get; set; }
    
    // Permanent FTE funding breakdown
    [Display(Name = "Permanent FTE - Programme Funded %")]
    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
    public decimal PermanentProgrammePercentage { get; set; }
    
    [Display(Name = "Permanent FTE - Admin Funded %")]
    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
    public decimal PermanentAdminPercentage { get; set; }
    
    [Display(Name = "Permanent FTE Notes")]
    [MaxLength(500)]
    public string? PermanentNotes { get; set; }
    
    // MSP FTE funding breakdown
    [Display(Name = "MSP FTE - Programme Funded %")]
    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
    public decimal MspProgrammePercentage { get; set; }
    
    [Display(Name = "MSP FTE - Admin Funded %")]
    [Range(0, 100, ErrorMessage = "Percentage must be between 0 and 100")]
    public decimal MspAdminPercentage { get; set; }
    
    [Display(Name = "MSP FTE Notes")]
    [MaxLength(500)]
    public string? MspNotes { get; set; }
    
    // Validation method to ensure percentages add up to 100%
    public bool IsValid()
    {
        return (PermanentProgrammePercentage + PermanentAdminPercentage == 100) &&
               (MspProgrammePercentage + MspAdminPercentage == 100);
    }
    
    public string GetValidationMessage()
    {
        if (PermanentProgrammePercentage + PermanentAdminPercentage != 100)
            return "Permanent FTE funding percentages must total 100%";
        
        if (MspProgrammePercentage + MspAdminPercentage != 100)
            return "MSP FTE funding percentages must total 100%";
        
        return string.Empty;
    }
}
