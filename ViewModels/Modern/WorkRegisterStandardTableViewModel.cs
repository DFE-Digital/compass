using Compass.Models.Modern.Work;

namespace Compass.ViewModels.Modern;

/// <summary>Standard work register table (All work columns).</summary>
public sealed class WorkRegisterStandardTableViewModel
{
    public string TableId { get; init; } = "work-register-table";

    public string Caption { get; init; } = "Work items";

    public string MonthlyColumnHeader { get; init; } = "Monthly update";

    public bool ShowMonthlyColumn { get; init; } = true;

    public IReadOnlyList<WorkRegisterRow> Rows { get; init; } = Array.Empty<WorkRegisterRow>();
}
