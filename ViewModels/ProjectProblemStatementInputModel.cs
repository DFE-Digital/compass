using System.ComponentModel.DataAnnotations;

namespace Compass.ViewModels;

public class ProjectProblemStatementInputModel
{
    [Required]
    public int ProjectId { get; set; }

    public int? ProblemStatementId { get; set; }

    [Required]
    public string ProblemStatement { get; set; } = string.Empty;
}

