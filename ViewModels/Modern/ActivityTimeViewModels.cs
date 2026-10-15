using Microsoft.AspNetCore.Mvc.Rendering;

namespace Compass.ViewModels.Modern;

public class ActivityTimeIndexViewModel
{
    public List<ActivityTimeEntryRowViewModel> Entries { get; set; } = new();
}

public class ActivityTimeEntryRowViewModel
{
    public int Id { get; set; }
    public DateTime ActivityDate { get; set; }
    public int DurationMinutes { get; set; }
    public string ActivityTypeName { get; set; } = "";
    public string? Narrative { get; set; }
    public string ThemesDisplay { get; set; } = "";
    public string? WorkItemTitle { get; set; }
    public string? BusinessAreaName { get; set; }
    public string CreatedByDisplay { get; set; } = "";

    public string DurationDisplay
    {
        get
        {
            var h = DurationMinutes / 60;
            var m = DurationMinutes % 60;
            if (h > 0 && m > 0) return $"{h}h {m}m";
            if (h > 0) return $"{h}h";
            return $"{m}m";
        }
    }
}

public class ActivityTimeFormViewModel
{
    public int? Id { get; set; }

    /// <summary>Posted as <c>yyyy-MM-dd</c> from <c>input type="date"</c>; prefer this over culture-sensitive DateTime binding.</summary>
    public string? ActivityDate { get; set; }

    public int Hours { get; set; }
    public int Minutes { get; set; }
    public string? Narrative { get; set; }
    public int ActivityTimeTypeLookupId { get; set; }
    public List<int> SelectedThemeIds { get; set; } = new();
    public bool RelatesToWorkItem { get; set; }
    public int? ProjectId { get; set; }
    public bool SupportsBusinessArea { get; set; }
    public int? BusinessAreaLookupId { get; set; }

    public List<SelectListItem> ActivityTypes { get; set; } = new();
    public List<SelectListItem> Themes { get; set; } = new();
    public List<SelectListItem> WorkItems { get; set; } = new();
    public List<SelectListItem> BusinessAreas { get; set; } = new();
}

public class ActivityTimeReportViewModel
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int? FilterTypeId { get; set; }
    public int? FilterThemeId { get; set; }
    public int? FilterBusinessAreaId { get; set; }

    public decimal TotalHours { get; set; }
    public int EntryCount { get; set; }

    public List<ActivityTimeReportSliceViewModel> ByActivityType { get; set; } = new();
    public List<ActivityTimeReportSliceViewModel> ByTheme { get; set; } = new();
    public List<ActivityTimeReportSliceViewModel> ByBusinessArea { get; set; } = new();
    public List<ActivityTimeReportSliceViewModel> ByPerson { get; set; } = new();
    public List<ActivityTimeReportDayViewModel> ByDay { get; set; } = new();
    public List<ActivityTimeEntryRowViewModel> Entries { get; set; } = new();

    public List<SelectListItem> ActivityTypes { get; set; } = new();
    public List<SelectListItem> Themes { get; set; } = new();
    public List<SelectListItem> BusinessAreas { get; set; } = new();
}

public class ActivityTimeReportSliceViewModel
{
    public string Label { get; set; } = "";
    public decimal Hours { get; set; }
    public int EntryCount { get; set; }
}

public class ActivityTimeReportDayViewModel
{
    public DateTime Date { get; set; }
    public decimal Hours { get; set; }
}
