using Compass.Models;

namespace Compass.ViewModels;

/// <summary>
/// View model for the Manage Standards page showing standards by stage
/// </summary>
public class DdtStandardsManageViewModel
{
    /// <summary>
    /// Standards in draft stage created by the current user
    /// </summary>
    public List<DdtStandard> MyDrafts { get; set; } = new List<DdtStandard>();

    /// <summary>
    /// All standards in draft stage
    /// </summary>
    public List<DdtStandard> AllDrafts { get; set; } = new List<DdtStandard>();

    /// <summary>
    /// Standards in review stage created by the current user
    /// </summary>
    public List<DdtStandard> MyInReview { get; set; } = new List<DdtStandard>();

    /// <summary>
    /// All standards in review stage
    /// </summary>
    public List<DdtStandard> AllInReview { get; set; } = new List<DdtStandard>();

    /// <summary>
    /// Standards for approval created by the current user
    /// </summary>
    public List<DdtStandard> MyForApproval { get; set; } = new List<DdtStandard>();

    /// <summary>
    /// All standards for approval
    /// </summary>
    public List<DdtStandard> AllForApproval { get; set; } = new List<DdtStandard>();

    /// <summary>
    /// Published standards created by the current user
    /// </summary>
    public List<DdtStandard> MyPublished { get; set; } = new List<DdtStandard>();

    /// <summary>
    /// All published standards
    /// </summary>
    public List<DdtStandard> AllPublished { get; set; } = new List<DdtStandard>();

    /// <summary>
    /// Available filter stages
    /// </summary>
    public List<string> Stages { get; set; } = new List<string>();

    /// <summary>
    /// Available filter categories
    /// </summary>
    public List<string> Categories { get; set; } = new List<string>();

    /// <summary>
    /// Available creators for filtering
    /// </summary>
    public List<(int Id, string Name)> Creators { get; set; } = new List<(int, string)>();

    /// <summary>
    /// Available owners for filtering
    /// </summary>
    public List<(int Id, string Name)> Owners { get; set; } = new List<(int, string)>();

    /// <summary>
    /// Available contacts for filtering
    /// </summary>
    public List<(int Id, string Name)> Contacts { get; set; } = new List<(int, string)>();

    /// <summary>
    /// Current search term
    /// </summary>
    public string? CurrentSearch { get; set; }

    /// <summary>
    /// Current stage filter
    /// </summary>
    public string? CurrentStage { get; set; }

    /// <summary>
    /// Current category filter
    /// </summary>
    public string? CurrentCategory { get; set; }

    /// <summary>
    /// Current creator filter
    /// </summary>
    public int? CurrentCreator { get; set; }

    /// <summary>
    /// Current owner filter
    /// </summary>
    public int? CurrentOwner { get; set; }

    /// <summary>
    /// Current contact filter
    /// </summary>
    public int? CurrentContact { get; set; }

    /// <summary>
    /// Current legal standard filter
    /// </summary>
    public bool? CurrentLegalStandard { get; set; }

    /// <summary>
    /// Active navigation pill (view)
    /// </summary>
    public string ActiveView { get; set; } = "drafts";
}

