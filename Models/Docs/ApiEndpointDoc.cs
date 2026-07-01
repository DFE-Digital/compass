namespace Compass.Models.Docs;

/// <summary>
/// Documents one Compass API endpoint for the developer documentation
/// page (<c>/docs/api</c>). Drives the per-endpoint card rendering, the
/// generated cURL / JavaScript / C# / Python / Postman snippets, and the
/// API Explorer dropdown.
/// </summary>
/// <param name="Id">Stable anchor id used in the URL hash (e.g. <c>risks-list</c>).</param>
/// <param name="Method">HTTP verb in upper case (<c>GET</c>, <c>POST</c>, <c>PUT</c>, <c>PATCH</c>, <c>DELETE</c>).</param>
/// <param name="Path">Route literal as the API matches it, e.g. <c>/api/v1/Risks</c>. Path placeholders use ASP.NET Core convention (<c>{id:int}</c>).</param>
/// <param name="Scope">The <c>[RequireApiPermission(...)]</c> scope or the literal text <c>Anonymous</c>.</param>
/// <param name="Description">One-sentence summary shown in the card header.</param>
/// <param name="QueryParams">Query string parameters (Name, Type, Description).</param>
/// <param name="RouteParams">Route parameters (Name, Type, Description).</param>
/// <param name="BodyExample">Pretty-printed JSON example body for write methods. <c>null</c> for read methods.</param>
/// <param name="ResponseExample">Pretty-printed JSON example response.</param>
/// <param name="ResponseShape">Optional one-line note about the response wrapper, e.g. "{ data: [], pagination: {} }".</param>
/// <param name="ExplorerExposed">Whether the endpoint should appear in the API Explorer dropdown.</param>
public sealed record ApiEndpointDoc(
    string Id,
    string Method,
    string Path,
    string Scope,
    string Description,
    IReadOnlyList<(string Name, string Type, string Description)>? RouteParams = null,
    IReadOnlyList<(string Name, string Type, string Description)>? QueryParams = null,
    string? BodyExample = null,
    string? ResponseExample = null,
    string? ResponseShape = null,
    bool ExplorerExposed = true);

/// <summary>A grouped section of endpoints on the API docs page.</summary>
public sealed record ApiEndpointSection(
    string Id,
    string Title,
    string Description,
    IReadOnlyList<ApiEndpointDoc> Endpoints);
