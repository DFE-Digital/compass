using System.Globalization;
using System.Text.Json;
using Compass.Models.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Pure aggregation helpers for the Service Census analysis report.
/// Operates on already-scoped answer facts — no database access.
/// </summary>
public static class ServiceCensusAnalysisAggregation
{
    public const int DefaultPageSize = 25;
    public const string KindChoice = "choice";
    public const string KindNumber = "number";
    public const string KindText = "text";
    public const string KindLinked = "linked";

    public static string AggregationKind(ServiceDataModelFieldType fieldType, bool allowMultiple) =>
        fieldType switch
        {
            ServiceDataModelFieldType.Number => KindNumber,
            ServiceDataModelFieldType.YesNo
                or ServiceDataModelFieldType.SingleChoice
                or ServiceDataModelFieldType.MultipleChoice
                or ServiceDataModelFieldType.Lookup => KindChoice,
            ServiceDataModelFieldType.Services
                or ServiceDataModelFieldType.ServiceLines => KindLinked,
            ServiceDataModelFieldType.Text
                or ServiceDataModelFieldType.MultilineText when allowMultiple => KindText,
            ServiceDataModelFieldType.Text
                or ServiceDataModelFieldType.MultilineText
                or ServiceDataModelFieldType.Url
                or ServiceDataModelFieldType.Date
                or ServiceDataModelFieldType.PersonOrTeamReference
                or ServiceDataModelFieldType.ServiceRelationship => KindText,
            _ => KindText
        };

    public static bool HasMeaningfulAnswer(string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson))
            return false;

        var trimmed = valueJson.Trim();
        if (trimmed is "null" or "[]" or "\"\"" or "{}")
            return false;

        try
        {
            using var doc = JsonDocument.Parse(trimmed);
            var el = doc.RootElement;
            return el.ValueKind switch
            {
                JsonValueKind.Null or JsonValueKind.Undefined => false,
                JsonValueKind.Array => el.GetArrayLength() > 0,
                JsonValueKind.String => !string.IsNullOrWhiteSpace(el.GetString()),
                JsonValueKind.Object => el.EnumerateObject().Any(),
                _ => true
            };
        }
        catch (JsonException)
        {
            return trimmed.Length > 0;
        }
    }

    public static ServiceCensusParsedAnswer ParseAnswer(
        ServiceDataModelFieldType fieldType,
        bool allowMultiple,
        string? valueJson)
    {
        var parsed = new ServiceCensusParsedAnswer { HasAnswer = HasMeaningfulAnswer(valueJson) };
        if (!parsed.HasAnswer)
            return parsed;

        var kind = AggregationKind(fieldType, allowMultiple);
        switch (kind)
        {
            case KindNumber:
                parsed.NumberValue = TryParseNumber(valueJson);
                parsed.HasAnswer = parsed.NumberValue.HasValue;
                break;
            case KindChoice:
                parsed.ChoiceKeys = NormaliseChoiceKeys(fieldType, valueJson);
                parsed.HasAnswer = parsed.ChoiceKeys.Count > 0;
                break;
            case KindLinked:
                parsed.LinkedItemKeys = CensusAnswerValueNormalizer.ParseStringList(valueJson)
                    .Where(v => Guid.TryParse(v, out _))
                    .Select(v => Guid.Parse(v).ToString())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                parsed.HasAnswer = parsed.LinkedItemKeys.Count > 0;
                break;
            default:
                parsed.TextValues = CensusAnswerValueNormalizer.ParseStringList(valueJson)
                    .Where(v => !string.IsNullOrWhiteSpace(v))
                    .Select(v => v.Trim())
                    .ToList();
                if (parsed.TextValues.Count == 0 && !string.IsNullOrWhiteSpace(valueJson))
                {
                    var plain = StripJsonQuotes(valueJson);
                    if (!string.IsNullOrWhiteSpace(plain))
                        parsed.TextValues = new[] { plain.Trim() };
                }

                parsed.HasAnswer = parsed.TextValues.Count > 0;
                break;
        }

        return parsed;
    }

    public static IReadOnlyList<ServiceCensusAnalysisThemeSummaryViewModel> BuildThemeSummaries(
        IReadOnlyList<ServiceCensusThemeDefinition> themes,
        IReadOnlyList<ServiceCensusAnswerFact> facts)
    {
        var answeredByTheme = facts
            .Where(f => f.HasAnswer)
            .GroupBy(f => f.ThemeStableKey, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.ProductId).Distinct().Count(),
                StringComparer.OrdinalIgnoreCase);

        return themes
            .OrderBy(t => t.SortOrder)
            .ThenBy(t => t.Name, StringComparer.OrdinalIgnoreCase)
            .Select(t => new ServiceCensusAnalysisThemeSummaryViewModel
            {
                StableKey = t.StableKey,
                Name = t.Name,
                SortOrder = t.SortOrder,
                MetricCount = t.Fields.Count,
                AnsweredServiceCount = answeredByTheme.TryGetValue(t.StableKey, out var n) ? n : 0
            })
            .ToList();
    }

    public static ServiceCensusAnalysisThemeDetailViewModel BuildThemeDetail(
        ServiceCensusThemeDefinition theme,
        IReadOnlyList<ServiceCensusAnswerFact> themeFacts)
    {
        var answeredServices = themeFacts
            .Where(f => f.HasAnswer)
            .Select(f => f.ProductId)
            .Distinct()
            .Count();

        var metrics = theme.Fields
            .OrderBy(f => f.SortOrder)
            .ThenBy(f => f.Label, StringComparer.OrdinalIgnoreCase)
            .Select(field =>
            {
                var fieldFacts = themeFacts
                    .Where(f => string.Equals(f.FieldStableKey, field.StableKey, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                var answered = fieldFacts.Count(f => f.HasAnswer);
                var kind = AggregationKind(field.FieldType, field.AllowMultiple);
                return new ServiceCensusAnalysisMetricSummaryViewModel
                {
                    StableKey = field.StableKey,
                    Label = field.Label,
                    FieldType = field.FieldType,
                    AllowMultiple = field.AllowMultiple,
                    AggregationKind = kind,
                    AnsweredServiceCount = answered,
                    SummaryText = BuildMetricSummaryText(kind, answered, fieldFacts)
                };
            })
            .ToList();

        return new ServiceCensusAnalysisThemeDetailViewModel
        {
            StableKey = theme.StableKey,
            Name = theme.Name,
            AnsweredServiceCount = answeredServices,
            Metrics = metrics
        };
    }

    public static ServiceCensusAnalysisMetricDetailViewModel BuildMetricDetail(
        ServiceCensusThemeDefinition theme,
        ServiceCensusFieldDefinition field,
        IReadOnlyList<ServiceCensusAnswerFact> fieldFacts,
        IReadOnlyDictionary<string, string> optionLabels,
        int page,
        int pageSize)
    {
        var answeredFacts = fieldFacts.Where(f => f.HasAnswer).ToList();
        var kind = AggregationKind(field.FieldType, field.AllowMultiple);
        var detail = new ServiceCensusAnalysisMetricDetailViewModel
        {
            ThemeStableKey = theme.StableKey,
            ThemeName = theme.Name,
            StableKey = field.StableKey,
            Label = field.Label,
            FieldType = field.FieldType,
            AllowMultiple = field.AllowMultiple,
            AggregationKind = kind,
            AnsweredServiceCount = answeredFacts.Count
        };

        switch (kind)
        {
            case KindChoice:
                detail.Options = BuildChoiceOptions(answeredFacts, optionLabels, field);
                break;
            case KindNumber:
                var numbers = answeredFacts
                    .Where(f => f.NumberValue.HasValue)
                    .Select(f => f.NumberValue!.Value)
                    .ToList();
                detail.NumberCount = numbers.Count;
                if (numbers.Count > 0)
                {
                    detail.NumberAverage = Math.Round(numbers.Average(), 2, MidpointRounding.AwayFromZero);
                    detail.NumberMin = numbers.Min();
                    detail.NumberMax = numbers.Max();
                }

                var numberRows = answeredFacts
                    .OrderBy(f => f.ProductTitle, StringComparer.OrdinalIgnoreCase)
                    .Select(f => ToProductRow(f, FormatNumber(f.NumberValue)))
                    .ToList();
                ApplyProductPagination(detail, numberRows, page, pageSize);
                break;
            case KindLinked:
                detail.Options = BuildLinkedOptions(answeredFacts, optionLabels);
                break;
            default:
                detail.TextValues = BuildTextValues(answeredFacts);
                break;
        }

        return detail;
    }

    public static ServiceCensusAnalysisOptionDrillViewModel BuildOptionDrill(
        ServiceCensusThemeDefinition theme,
        ServiceCensusFieldDefinition field,
        string optionKey,
        string optionLabel,
        IReadOnlyList<ServiceCensusAnswerFact> fieldFacts,
        int page,
        int pageSize)
    {
        var kind = AggregationKind(field.FieldType, field.AllowMultiple);
        var matching = fieldFacts
            .Where(f => f.HasAnswer && FactMatchesOption(f, kind, optionKey))
            .OrderBy(f => f.ProductTitle, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var rows = matching
            .Select(f => ToProductRow(f, FormatFactAnswer(f, kind, optionLabels: null)))
            .ToList();

        var (pageRows, total, pageNumber, size, totalPages) = Paginate(rows, page, pageSize);
        return new ServiceCensusAnalysisOptionDrillViewModel
        {
            ThemeStableKey = theme.StableKey,
            ThemeName = theme.Name,
            FieldStableKey = field.StableKey,
            FieldLabel = field.Label,
            OptionKey = optionKey,
            OptionLabel = optionLabel,
            Products = pageRows,
            ProductTotalCount = total,
            ProductPage = pageNumber,
            ProductPageSize = size,
            ProductTotalPages = totalPages
        };
    }

    public static ServiceCensusAnalysisProductDetailViewModel BuildProductDetail(
        ServiceCensusAnswerFact productIdentity,
        IReadOnlyList<ServiceCensusThemeDefinition> themes,
        IReadOnlyList<ServiceCensusAnswerFact> productFacts,
        IReadOnlyDictionary<string, string> optionLabels)
    {
        var themesVm = themes
            .OrderBy(t => t.SortOrder)
            .Select(theme =>
            {
                var answers = theme.Fields
                    .OrderBy(f => f.SortOrder)
                    .Select(field =>
                    {
                        var fact = productFacts.FirstOrDefault(f =>
                            string.Equals(f.FieldStableKey, field.StableKey, StringComparison.OrdinalIgnoreCase));
                        var kind = AggregationKind(field.FieldType, field.AllowMultiple);
                        return new ServiceCensusAnalysisProductFieldAnswerViewModel
                        {
                            StableKey = field.StableKey,
                            Label = field.Label,
                            FieldType = field.FieldType,
                            HasAnswer = fact?.HasAnswer == true,
                            DisplayValue = fact?.HasAnswer == true
                                ? FormatFactAnswer(fact, kind, optionLabels)
                                : null
                        };
                    })
                    .ToList();

                return new ServiceCensusAnalysisProductThemeAnswersViewModel
                {
                    StableKey = theme.StableKey,
                    Name = theme.Name,
                    Answers = answers
                };
            })
            .ToList();

        return new ServiceCensusAnalysisProductDetailViewModel
        {
            ProductId = productIdentity.ProductId,
            AssignmentId = productIdentity.AssignmentId,
            ProductTitle = productIdentity.ProductTitle,
            PhaseName = productIdentity.PhaseName,
            BusinessAreasDisplay = JoinNames(productIdentity.BusinessAreaNames),
            DirectoratesDisplay = JoinNames(productIdentity.DirectorateNames),
            Themes = themesVm
        };
    }

    public static (IReadOnlyList<T> Items, int TotalCount, int PageNumber, int PageSize, int TotalPages)
        Paginate<T>(IReadOnlyList<T> ordered, int page, int pageSize)
    {
        if (pageSize < 1)
            pageSize = DefaultPageSize;
        if (page < 1)
            page = 1;

        var total = ordered.Count;
        var totalPages = total == 0 ? 1 : (int)Math.Ceiling(total / (double)pageSize);
        if (page > totalPages)
            page = totalPages;

        var slice = ordered.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        return (slice, total, page, pageSize, totalPages);
    }

    private static void ApplyProductPagination(
        ServiceCensusAnalysisMetricDetailViewModel detail,
        IReadOnlyList<ServiceCensusAnalysisProductAnswerRowViewModel> rows,
        int page,
        int pageSize)
    {
        var (pageRows, total, pageNumber, size, totalPages) = Paginate(rows, page, pageSize);
        detail.NumberProducts = pageRows;
        detail.ProductTotalCount = total;
        detail.ProductPage = pageNumber;
        detail.ProductPageSize = size;
        detail.ProductTotalPages = totalPages;
    }

    private static IReadOnlyList<ServiceCensusAnalysisOptionCountViewModel> BuildChoiceOptions(
        IReadOnlyList<ServiceCensusAnswerFact> answeredFacts,
        IReadOnlyDictionary<string, string> optionLabels,
        ServiceCensusFieldDefinition field)
    {
        var denominator = answeredFacts.Count;
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var fact in answeredFacts)
        {
            foreach (var key in fact.ChoiceKeys.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                counts.TryGetValue(key, out var n);
                counts[key] = n + 1;
            }
        }

        // Include declared options with zero counts so the breakdown is complete.
        foreach (var declared in field.OptionKeys)
        {
            if (!counts.ContainsKey(declared))
                counts[declared] = 0;
        }

        return counts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => ResolveLabel(kv.Key, optionLabels), StringComparer.OrdinalIgnoreCase)
            .Select(kv => new ServiceCensusAnalysisOptionCountViewModel
            {
                OptionKey = kv.Key,
                Label = ResolveLabel(kv.Key, optionLabels),
                Count = kv.Value,
                Percent = denominator <= 0
                    ? null
                    : Math.Round((decimal)kv.Value / denominator * 100m, 1, MidpointRounding.AwayFromZero)
            })
            .ToList();
    }

    private static IReadOnlyList<ServiceCensusAnalysisOptionCountViewModel> BuildLinkedOptions(
        IReadOnlyList<ServiceCensusAnswerFact> answeredFacts,
        IReadOnlyDictionary<string, string> optionLabels)
    {
        var servicesWithLinks = answeredFacts.Count;
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var fact in answeredFacts)
        {
            foreach (var key in fact.LinkedItemKeys.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                counts.TryGetValue(key, out var n);
                counts[key] = n + 1;
            }
        }

        return counts
            .OrderByDescending(kv => kv.Value)
            .ThenBy(kv => ResolveLabel(kv.Key, optionLabels), StringComparer.OrdinalIgnoreCase)
            .Select(kv => new ServiceCensusAnalysisOptionCountViewModel
            {
                OptionKey = kv.Key,
                Label = ResolveLabel(kv.Key, optionLabels),
                Count = kv.Value,
                Percent = servicesWithLinks <= 0
                    ? null
                    : Math.Round((decimal)kv.Value / servicesWithLinks * 100m, 1, MidpointRounding.AwayFromZero)
            })
            .ToList();
    }

    private static IReadOnlyList<ServiceCensusAnalysisTextValueViewModel> BuildTextValues(
        IReadOnlyList<ServiceCensusAnswerFact> answeredFacts)
    {
        var buckets = new Dictionary<string, HashSet<Guid>>(StringComparer.OrdinalIgnoreCase);
        foreach (var fact in answeredFacts)
        {
            foreach (var value in fact.TextValues.Where(v => !string.IsNullOrWhiteSpace(v)))
            {
                var key = value.Trim();
                if (!buckets.TryGetValue(key, out var set))
                {
                    set = new HashSet<Guid>();
                    buckets[key] = set;
                }

                set.Add(fact.ProductId);
            }
        }

        return buckets
            .OrderByDescending(kv => kv.Value.Count)
            .ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => new ServiceCensusAnalysisTextValueViewModel
            {
                ValueKey = kv.Key,
                DisplayValue = Truncate(kv.Key, 200),
                ServiceCount = kv.Value.Count
            })
            .ToList();
    }

    private static bool FactMatchesOption(ServiceCensusAnswerFact fact, string kind, string optionKey)
    {
        if (string.IsNullOrWhiteSpace(optionKey))
            return false;

        return kind switch
        {
            KindChoice => fact.ChoiceKeys.Any(k =>
                string.Equals(k, optionKey, StringComparison.OrdinalIgnoreCase)),
            KindLinked => fact.LinkedItemKeys.Any(k =>
                string.Equals(k, optionKey, StringComparison.OrdinalIgnoreCase)),
            KindText => fact.TextValues.Any(v =>
                string.Equals(v, optionKey, StringComparison.OrdinalIgnoreCase)),
            KindNumber => fact.NumberValue.HasValue &&
                          string.Equals(FormatNumber(fact.NumberValue), optionKey, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }

    private static string BuildMetricSummaryText(
        string kind,
        int answered,
        IReadOnlyList<ServiceCensusAnswerFact> fieldFacts)
    {
        if (answered == 0)
            return "No answers yet";

        return kind switch
        {
            KindNumber =>
                fieldFacts.Where(f => f.NumberValue.HasValue).Select(f => f.NumberValue!.Value).ToList() is { Count: > 0 } nums
                    ? $"{answered} answered · avg {FormatNumber(nums.Average())}"
                    : $"{answered} answered",
            KindLinked =>
                $"{answered} linked something · {fieldFacts.Where(f => f.HasAnswer).SelectMany(f => f.LinkedItemKeys).Distinct(StringComparer.OrdinalIgnoreCase).Count()} distinct items",
            KindText => $"{answered} answered",
            _ => $"{answered} answered"
        };
    }

    private static IReadOnlyList<string> NormaliseChoiceKeys(ServiceDataModelFieldType fieldType, string? valueJson)
    {
        if (fieldType == ServiceDataModelFieldType.YesNo)
        {
            var raw = CensusAnswerValueNormalizer.ParseStringList(valueJson);
            if (raw.Count == 0 && !string.IsNullOrWhiteSpace(valueJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(valueJson);
                    if (doc.RootElement.ValueKind == JsonValueKind.True)
                        return new[] { "Yes" };
                    if (doc.RootElement.ValueKind == JsonValueKind.False)
                        return new[] { "No" };
                }
                catch (JsonException)
                {
                    // fall through
                }
            }

            return raw
                .Select(NormaliseYesNo)
                .Where(v => v is not null)
                .Cast<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        return CensusAnswerValueNormalizer.ParseStringList(valueJson)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Select(v => v.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string? NormaliseYesNo(string value)
    {
        if (string.Equals(value, "Yes", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "true", StringComparison.OrdinalIgnoreCase))
            return "Yes";
        if (string.Equals(value, "No", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "false", StringComparison.OrdinalIgnoreCase))
            return "No";
        return value.Trim();
    }

    private static decimal? TryParseNumber(string? valueJson)
    {
        if (string.IsNullOrWhiteSpace(valueJson))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(valueJson);
            var el = doc.RootElement;
            if (el.ValueKind == JsonValueKind.Number)
                return el.GetDecimal();
            if (el.ValueKind == JsonValueKind.String &&
                decimal.TryParse(el.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var fromString))
                return fromString;
        }
        catch (JsonException)
        {
            if (decimal.TryParse(valueJson.Trim().Trim('"'), NumberStyles.Number, CultureInfo.InvariantCulture, out var plain))
                return plain;
        }

        return null;
    }

    private static string FormatNumber(decimal? value) =>
        value.HasValue ? value.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";

    private static string FormatNumber(decimal value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    /// <summary>
    /// Human-readable answer for Excel / tabular export. Empty when unanswered.
    /// Lookup keys resolve via <paramref name="optionLabels"/> (capabilities as Title (Reference), user groups by name).
    /// </summary>
    public static string FormatAnswerForExport(
        ServiceCensusAnswerFact fact,
        IReadOnlyDictionary<string, string>? optionLabels)
    {
        if (!fact.HasAnswer)
            return "";

        var kind = AggregationKind(fact.FieldType, fact.AllowMultiple);
        return kind switch
        {
            KindNumber => fact.NumberValue.HasValue
                ? FormatNumber(fact.NumberValue.Value)
                : "",
            KindChoice => string.Join(", ",
                fact.ChoiceKeys.Select(k => optionLabels != null ? ResolveLabel(k, optionLabels) : k)),
            KindLinked => string.Join(", ",
                fact.LinkedItemKeys.Select(k => optionLabels != null ? ResolveLabel(k, optionLabels) : k)),
            _ => string.Join("; ", fact.TextValues)
        };
    }

    private static string FormatFactAnswer(
        ServiceCensusAnswerFact fact,
        string kind,
        IReadOnlyDictionary<string, string>? optionLabels)
    {
        return kind switch
        {
            KindNumber => FormatNumber(fact.NumberValue),
            KindChoice => string.Join(", ",
                fact.ChoiceKeys.Select(k => optionLabels != null ? ResolveLabel(k, optionLabels) : k)),
            KindLinked => string.Join(", ",
                fact.LinkedItemKeys.Select(k => optionLabels != null ? ResolveLabel(k, optionLabels) : k)),
            _ => string.Join("; ", fact.TextValues.Select(v => Truncate(v, 120)))
        };
    }

    private static string ResolveLabel(string key, IReadOnlyDictionary<string, string> labels) =>
        labels.TryGetValue(key, out var label) && !string.IsNullOrWhiteSpace(label) ? label : key;

    private static ServiceCensusAnalysisProductAnswerRowViewModel ToProductRow(
        ServiceCensusAnswerFact fact,
        string? answerDisplay) =>
        new()
        {
            ProductId = fact.ProductId,
            AssignmentId = fact.AssignmentId,
            ProductTitle = string.IsNullOrWhiteSpace(fact.ProductTitle) ? "Untitled product" : fact.ProductTitle,
            AnswerDisplay = answerDisplay,
            PhaseName = fact.PhaseName,
            BusinessAreasDisplay = JoinNames(fact.BusinessAreaNames)
        };

    private static string? JoinNames(IReadOnlyList<string> names) =>
        names.Count == 0 ? null : string.Join(", ", names);

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;
        return value[..(max - 1)] + "…";
    }

    private static string StripJsonQuotes(string valueJson)
    {
        var t = valueJson.Trim();
        if (t.Length >= 2 && t[0] == '"' && t[^1] == '"')
            return t[1..^1];
        return t;
    }
}

public sealed class ServiceCensusThemeDefinition
{
    public string StableKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int SortOrder { get; init; }
    public IReadOnlyList<ServiceCensusFieldDefinition> Fields { get; init; } =
        Array.Empty<ServiceCensusFieldDefinition>();
}

public sealed class ServiceCensusFieldDefinition
{
    public string StableKey { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public ServiceDataModelFieldType FieldType { get; init; }
    public bool AllowMultiple { get; init; }
    public int SortOrder { get; init; }
    public string? OptionsLookupKey { get; init; }
    public IReadOnlyList<string> OptionKeys { get; init; } = Array.Empty<string>();
}

public sealed class ServiceCensusParsedAnswer
{
    public bool HasAnswer { get; set; }
    public IReadOnlyList<string> ChoiceKeys { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> TextValues { get; set; } = Array.Empty<string>();
    public IReadOnlyList<string> LinkedItemKeys { get; set; } = Array.Empty<string>();
    public decimal? NumberValue { get; set; }
}

/// <summary>One product × field answer already scoped to the caller's access and filters.</summary>
public sealed class ServiceCensusAnswerFact
{
    public Guid AssignmentId { get; init; }
    public Guid ProductId { get; init; }
    public string ProductTitle { get; init; } = string.Empty;
    public string ThemeStableKey { get; init; } = string.Empty;
    public string ThemeName { get; init; } = string.Empty;
    public string FieldStableKey { get; init; } = string.Empty;
    public string FieldLabel { get; init; } = string.Empty;
    public ServiceDataModelFieldType FieldType { get; init; }
    public bool AllowMultiple { get; init; }
    public bool HasAnswer { get; init; }
    public IReadOnlyList<string> ChoiceKeys { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> TextValues { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> LinkedItemKeys { get; init; } = Array.Empty<string>();
    public decimal? NumberValue { get; init; }
    public string? PhaseName { get; init; }
    public IReadOnlyList<string> BusinessAreaNames { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> DirectorateNames { get; init; } = Array.Empty<string>();
}
