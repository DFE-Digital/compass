using System.Collections.Generic;
using Compass.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using ActionModel = Compass.Models.Action;

namespace Compass.ViewModels;

public class ActionDetailsViewModel
{
    public ActionModel Action { get; set; } = new();

    public ActionDetailsUpdateInputModel Input { get; set; } = new();

    public IReadOnlyList<Decision> DecisionAssociations { get; set; } = new List<Decision>();

    public IEnumerable<SelectListItem> StatusOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> PriorityOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> BusinessAreaOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ObjectiveOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ActionSourceOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ParentActionOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> ProductOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> RiskOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> IssueOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> MilestoneOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> DecisionOptions { get; set; } = new List<SelectListItem>();

    public IEnumerable<SelectListItem> SourceTypeOptions { get; set; } = new List<SelectListItem>();

    public IReadOnlyDictionary<string, ProductDto> ProductLookup { get; set; } = new Dictionary<string, ProductDto>();
}
