namespace Compass.ViewModels;

/// <summary>Headcount (FTE) fields for the monthly update create/edit pages.</summary>
public class MonthlyFteCardViewModel
{
    public int ProjectId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public bool CanEdit { get; set; }
    public decimal? MonthlyPermFte { get; set; }
    public decimal? MonthlyMspFte { get; set; }
    /// <summary>Redirect target after save: CreateUpdate or EditUpdate.</summary>
    public string ReturnAction { get; set; } = "CreateUpdate";
}
