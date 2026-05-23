using Compass.Models.Docs;

namespace Compass.Services.Docs;

public static class ApiExplorerCatalogueBuilder
{
    public static object[] BuildSections() =>
        ApiCatalogue.Sections
            .Select(section => new
            {
                id = section.Id,
                title = section.Title,
                description = section.Description,
                endpoints = section.Endpoints
                    .Where(e => e.ExplorerExposed)
                    .Select(e => new
                    {
                        id = e.Id,
                        method = e.Method,
                        path = e.Path,
                        scope = e.Scope,
                        description = e.Description,
                        responseShape = e.ResponseShape,
                        routeParams = (e.RouteParams ?? Array.Empty<(string, string, string)>())
                            .Select(p => new { name = p.Name, type = p.Type, description = p.Description }),
                        queryParams = (e.QueryParams ?? Array.Empty<(string, string, string)>())
                            .Select(p => new { name = p.Name, type = p.Type, description = p.Description }),
                        bodyExample = e.BodyExample,
                        responseExample = e.ResponseExample
                    })
                    .ToArray()
            })
            .Where(s => s.endpoints.Length > 0)
            .ToArray();

    public static int CountEndpoints() =>
        ApiCatalogue.Sections.Sum(s => s.Endpoints.Count(e => e.ExplorerExposed));
}
