namespace Compass.Configuration;

/// <summary>API explorer environment bases and CSP connect-src allow list.</summary>
public sealed class DocsApiExplorerOptions
{
    public const string SectionName = "Docs:ApiExplorer";

    public string ProductionBaseUrl { get; set; } = "https://compass.education.gov.uk";

    public string? TestBaseUrl { get; set; }

    /// <summary>Extra host authorities allowed in connect-src (e.g. staging slots).</summary>
    public List<string> ConnectSrcHosts { get; set; } = [];

    public IEnumerable<string> AllConnectHosts()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var url in new[] { ProductionBaseUrl, TestBaseUrl }.Concat(ConnectSrcHosts))
        {
            if (string.IsNullOrWhiteSpace(url)) continue;
            if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) continue;
            var authority = uri.GetLeftPart(UriPartial.Authority);
            if (seen.Add(authority))
                yield return authority;
        }
    }
}
