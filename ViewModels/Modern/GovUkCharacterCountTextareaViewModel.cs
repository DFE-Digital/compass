namespace Compass.ViewModels.Modern;

/// <summary>Shared template for GOV.UK Frontend <see href="https://design-system.service.gov.uk/components/character-count/">character count</see> textareas.</summary>
public sealed class GovUkCharacterCountTextareaViewModel
{
    public required string FieldId { get; init; }
    public required string Name { get; init; }
    public required string Label { get; init; }
    public string? Hint { get; init; }

    /// <summary>Key used with <c>ViewData.ModelState</c> for server validation errors.</summary>
    public required string ModelStateKey { get; init; }

    public string? Value { get; init; }
    public int MaxLength { get; init; } = 4000;
    public int Rows { get; init; } = 8;

    public string LabelClass { get; init; } = "govuk-label govuk-label--s";
    public bool VisuallyHiddenLabel { get; init; }
}
