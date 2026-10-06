using Compass.Models.ServiceSchema;
using Compass.ViewModels.Modern;

namespace Compass.Services.ServiceSchema;

public static class ServiceSchemaResponseModes
{
    public const string Text = "text";
    public const string Lookup = "lookup";
    public const string Catalogue = "catalogue";
    public const string Choice = "choice";
    public const string Products = "products";
    public const string YesChoice = "yes-choice";
    public const string QuestionList = "question";

    public static bool IsKnown(string? mode) =>
        mode is Text or Lookup or Catalogue or Choice or Products or YesChoice;

    public static string Label(ServiceSchemaQuestion question)
    {
        var mode = string.IsNullOrWhiteSpace(question.ResponseMode) ? Text : question.ResponseMode;
        return mode switch
        {
            Lookup => "Select: " + (SourceLabel(question.LookupSource) ?? "list"),
            Catalogue => "Autocomplete: " + (CatalogueLabel(question) ?? "list"),
            Choice => "Choice",
            YesChoice => "Yes or no, then a choice",
            Products => "Service register products",
            _ => "Text"
        };
    }

    public static string? Apply(
        ServiceSchemaQuestion question,
        string? responseMode,
        string? lookupSource,
        string? catalogueKind,
        string? choiceOptions)
    {
        var mode = (responseMode ?? Text).Trim().ToLowerInvariant();
        if (!IsKnown(mode))
            return "Choose how people answer this question.";

        question.LookupSource = null;
        question.CatalogueKind = null;
        var previousChoices = question.ChoiceOptions;
        question.ChoiceOptions = null;
        question.ResponseMode = mode;

        switch (mode)
        {
            case Lookup:
                var source = (lookupSource ?? "").Trim();
                if (ServiceSchemaAdminLookups.Sources.All(s => !string.Equals(s.Key, source, StringComparison.OrdinalIgnoreCase)))
                    return "Choose the list people select from.";
                question.LookupSource = ServiceSchemaAdminLookups.Sources
                    .First(s => string.Equals(s.Key, source, StringComparison.OrdinalIgnoreCase)).Key;
                break;
            case Catalogue:
                var kind = (catalogueKind ?? "").Trim();
                if (string.Equals(kind, QuestionList, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(kind, question.Key, StringComparison.OrdinalIgnoreCase))
                    kind = question.Key;
                else if (!IsCatalogueKind(kind))
                    return "Choose the list for autocomplete.";
                question.CatalogueKind = kind;
                break;
            case Choice:
            case YesChoice:
                var options = ParseChoices(string.IsNullOrWhiteSpace(choiceOptions) ? previousChoices : choiceOptions);
                if (options.Count < 2)
                    return "Enter at least two choices, one on each line.";
                question.ChoiceOptions = string.Join('\n', options.Select(o => o.Label));
                break;
        }

        return null;
    }

    public static List<ServiceSchemaChoice> ParseChoices(string? raw)
    {
        var labels = (raw ?? "")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(line => line.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var choices = new List<ServiceSchemaChoice>(labels.Count);
        foreach (var label in labels)
        {
            var slug = Slug(label);
            var code = slug;
            var suffix = 2;
            while (!used.Add(code))
                code = slug + "-" + suffix++;
            choices.Add(new ServiceSchemaChoice { Value = code, Label = label.Length <= 200 ? label : label[..200] });
        }
        return choices;
    }

    public static bool IsCatalogueKind(string? kind) =>
        !string.IsNullOrWhiteSpace(kind) &&
        kind.Length <= 40 &&
        ServiceSchemaCatalog.CatalogueKinds.Any(k => string.Equals(k.Kind, kind, StringComparison.OrdinalIgnoreCase));

    private static string? SourceLabel(string? key) =>
        ServiceSchemaAdminLookups.Sources.FirstOrDefault(s => string.Equals(s.Key, key, StringComparison.OrdinalIgnoreCase))?.Label;

    private static string? CatalogueLabel(ServiceSchemaQuestion question)
    {
        if (string.Equals(question.CatalogueKind, question.Key, StringComparison.OrdinalIgnoreCase))
            return "this question";
        return ServiceSchemaCatalog.CatalogueKinds
            .FirstOrDefault(k => string.Equals(k.Kind, question.CatalogueKind, StringComparison.OrdinalIgnoreCase)).Label;
    }

    private static string Slug(string label)
    {
        var slug = new string(label.Trim().ToLowerInvariant().Select(c => char.IsLetterOrDigit(c) ? c : '-').ToArray());
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        slug = slug.Trim('-');
        if (slug.Length > 40)
            slug = slug[..40].Trim('-');
        return string.IsNullOrEmpty(slug) ? "choice" : slug;
    }
}
