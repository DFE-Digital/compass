namespace Compass.Models;

#region Work item chrome (Modern partials)

/// <summary>RAG snapshot for badges; <see cref="Modern.Work.WorkItemRagHistory"/> inherits this for chrome.</summary>
public class ChromeRagSnapshot
{
    public RagStatus? RagStatus { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class RagStatus
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? BackgroundColourKey { get; set; }
    public string? TextColourKey { get; set; }
}

/// <summary>Placeholder type for ViewBag casts in work chrome (optional linked demand).</summary>
public class DemandRequest
{
    public Guid Id { get; set; }
    public string? Reference { get; set; }
    public string? Title { get; set; }
}

#endregion

#region Reporting / capacity scope bars

public class Directorate
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
}

public class Portfolio
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

#endregion

#region Admin lookups sidebar

public class AdminLookupGroup
{
    public string Title { get; set; } = "";
    public List<AdminLookupSectionItem> Sections { get; set; } = new();
}

public class AdminLookupSectionItem
{
    public string Key { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public int Count { get; set; }
    public string Url { get; set; } = "";
}

#endregion

#region Search and filter

public class SearchAndFilterViewModel
{
    public string IdPrefix { get; set; } = string.Empty;
    public string SearchPlaceholder { get; set; } = string.Empty;
    public string? SearchValue { get; set; }
    public string FormActionUrl { get; set; } = string.Empty;
    public string FormMethod { get; set; } = "get";
    public string ClearUrl { get; set; } = string.Empty;
    public IList<SearchAndFilterFieldViewModel> Fields { get; set; } = new List<SearchAndFilterFieldViewModel>();
    public string? SecondaryActionUrl { get; set; }
    public string? SecondaryActionLabel { get; set; }
    public IList<KeyValuePair<string, string>> HiddenFields { get; set; } = new List<KeyValuePair<string, string>>();
}

public class SearchAndFilterFieldViewModel
{
    public string Label { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Id { get; set; }
    public IList<SearchAndFilterOption> Options { get; set; } = new List<SearchAndFilterOption>();
    public string? SelectedValue { get; set; }
}

public class SearchAndFilterOption
{
    public string Value { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
}

#endregion

#region User picker

public class UserPickerViewModel
{
    public string FieldName { get; set; } = "UserId";
    public string Label { get; set; } = "Select user";
    public string? Hint { get; set; }
    public int? DefaultUserId { get; set; }
    public string? DefaultName { get; set; }
    public string? DefaultEmail { get; set; }
    public string Placeholder { get; set; } = "e.g. Alex Smith or alex.smith@education.gov.uk";
    public string DefaultSummaryText { get; set; } = "No user selected.";
    public string? InputIdSuffix { get; set; }
}

#endregion

#region Pipeline tracker

public class PipelineTrackerViewModel
{
    public List<PipelineStage> Stages { get; set; } = new();
    public int[] StageCounts { get; set; } = Array.Empty<int>();
    public List<PipelineTrackerGroupSpan> GroupSpans { get; set; } = new();
    public int? StageIndex { get; set; }
    public string ViewMode { get; set; } = "columns";
    public int? DirectorateId { get; set; }
    public string? Search { get; set; }
    public string? Requestor { get; set; }
    public int? PortfolioId { get; set; }
    public string? StatusFilter { get; set; }
    public bool? StatutoryDriver { get; set; }
}

public class PipelineTrackerGroupSpan
{
    public string? Key { get; set; }
    public int SpanCount { get; set; }
    public int ItemCount { get; set; }
}

public class PipelineStage
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Grouping { get; set; }
}

#endregion
