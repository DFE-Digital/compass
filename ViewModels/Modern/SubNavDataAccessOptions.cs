namespace Compass.ViewModels.Modern;

/// <summary>Export and API access for list and item views (sub-navigation toolbar + popovers).</summary>
public sealed class SubNavDataAccessOptions
{
    public bool IsItemView { get; init; }

    public SubNavExportPanel? Export { get; init; }

    public SubNavApiPanel? Api { get; init; }

    public bool ShowExport => Export is { HasLinks: true };

    public bool ShowApi => Api != null && (
        Api.ItemEndpoint != null
        || !string.IsNullOrWhiteSpace(Api.Intro)
        || Api.HasEndpoints);

    public bool ShowToolbar => ShowExport || ShowApi;

    public bool ShowItemHint => IsItemView && ShowToolbar;
}

public sealed class SubNavExportPanel
{
    public string Title { get; init; } = "Download data";

    public string? Intro { get; init; }

    public List<SubNavExportLink> Links { get; init; } = new();

    public bool HasLinks => Links.Count > 0;
}

public sealed class SubNavExportLink
{
    public string Label { get; init; } = "";

    public string Href { get; init; } = "";

    public string? Description { get; init; }

    /// <summary>json, csv, pdf, excel — affects icon in item download menu.</summary>
    public string Format { get; init; } = "excel";

    /// <summary>When true, uses generating-report-modal on click.</summary>
    public bool UseDownloadModal { get; init; } = true;

    public string? DownloadModalTitle { get; init; }
}

public sealed class SubNavApiPanel
{
    public string Title { get; init; } = "API data access";

    public string? Intro { get; init; }

    public List<SubNavApiEndpoint> Endpoints { get; init; } = new();

    public SubNavApiItemEndpoint? ItemEndpoint { get; init; }

    public string? DocsUrl { get; init; }

    public string? ApiTokensUrl { get; init; }

    public string? ApiExplorerUrl { get; init; }

    public string? BaseUrl { get; init; }

    public bool HasEndpoints => Endpoints.Count > 0;
}

/// <summary>Primary API endpoint for a single record (item detail views).</summary>
public sealed class SubNavApiItemEndpoint
{
    public string Method { get; init; } = "GET";

    public string Url { get; init; } = "";

    public string? Scope { get; init; }

    public string? UpdatedAt { get; init; }
}

public sealed class SubNavApiEndpoint
{
    public string Label { get; init; } = "";

    public string Method { get; init; } = "GET";

    public string Path { get; init; } = "";

    public string? Scope { get; init; }

    public string? Note { get; init; }
}
