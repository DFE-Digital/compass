namespace Compass.Models;

/// <summary>
/// View model for portfolio/business area reports.
/// Provides a consistent data structure for all portfolio report views.
/// </summary>
public class PortfolioReportViewModel
{
    /// <summary>
    /// The name of the portfolio/business area.
    /// </summary>
    public string PortfolioName { get; set; } = string.Empty;

    /// <summary>
    /// Description of the portfolio/business area.
    /// </summary>
    public string PortfolioDescription { get; set; } = "Current state of projects and milestones";

    /// <summary>
    /// List of projects in this portfolio.
    /// </summary>
    public List<Project> Projects { get; set; } = new List<Project>();
}

