using System;
using System.Linq;
using System.Text.Json;
using Compass.ViewModels.Dashboard;

namespace Compass.Helpers;

public static class DashboardLayoutHelper
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static IReadOnlyCollection<DashboardBlockDefinition> GetBlockCatalog() => new[]
    {
        new DashboardBlockDefinition
        {
            Type = "open-risks-count",
            Title = "Open risks",
            Category = "Counts",
            Description = "Number of active risks across your projects.",
            DefaultWidth = 3,
            DefaultHeight = 1,
            MinWidth = 3,
            MinHeight = 1,
            SupportsConfiguration = false
        },
        new DashboardBlockDefinition
        {
            Type = "open-issues-count",
            Title = "Open issues",
            Category = "Counts",
            Description = "Number of high priority issues.",
            DefaultWidth = 3,
            DefaultHeight = 1,
            MinWidth = 3,
            MinHeight = 1,
            SupportsConfiguration = false
        },
        new DashboardBlockDefinition
        {
            Type = "open-actions-count",
            Title = "Assigned actions",
            Category = "Counts",
            Description = "Outstanding actions assigned to you.",
            DefaultWidth = 3,
            DefaultHeight = 1,
            MinWidth = 3,
            MinHeight = 1,
            SupportsConfiguration = false
        },
        new DashboardBlockDefinition
        {
            Type = "products-by-phase-chart",
            Title = "Products by phase",
            Category = "Charts",
            Description = "Breakdown of your products across the delivery lifecycle.",
            DefaultWidth = 6,
            DefaultHeight = 3,
            MinWidth = 4,
            MinHeight = 2,
            SupportsConfiguration = false,
            UsesChart = true
        },
        new DashboardBlockDefinition
        {
            Type = "projects-table",
            Title = "Active projects",
            Category = "Tables",
            Description = "Tabular view of active projects with RAG status.",
            DefaultWidth = 8,
            DefaultHeight = 4,
            MinWidth = 6,
            MinHeight = 2,
            SupportsConfiguration = false,
            IsTable = true
        },
        new DashboardBlockDefinition
        {
            Type = "products-table",
            Title = "Product overview",
            Category = "Tables",
            Description = "List of products and lifecycle phase.",
            DefaultWidth = 6,
            DefaultHeight = 4,
            MinWidth = 6,
            MinHeight = 2,
            SupportsConfiguration = false,
            IsTable = true
        }
    };

    public static List<DashboardBlockInstance> GetDefaultLayout(IReadOnlyCollection<DashboardBlockDefinition> definitions)
    {
        var layout = new List<DashboardBlockInstance>();
        var blocks = new[]
        {
            ("open-risks-count", 0, 0),
            ("open-issues-count", 3, 0),
            ("open-actions-count", 6, 0),
            ("products-by-phase-chart", 0, 1),
            ("projects-table", 0, 4),
            ("products-table", 6, 4)
        };

        foreach (var (type, x, y) in blocks)
        {
            var definition = definitions.FirstOrDefault(d => d.Type == type);
            if (definition == null)
            {
                continue;
            }

            layout.Add(new DashboardBlockInstance
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                X = x,
                Y = y,
                Width = definition.DefaultWidth,
                Height = definition.DefaultHeight
            });
        }

        return layout;
    }

    public static List<DashboardBlockInstance> ParseLayout(string? json, IReadOnlyCollection<DashboardBlockDefinition> definitions)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<DashboardBlockInstance>();
        }

        try
        {
            var blocks = JsonSerializer.Deserialize<List<DashboardBlockInstance>>(json, SerializerOptions) ?? new();
            var validTypes = definitions.Select(d => d.Type).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return blocks
                .Where(b => validTypes.Contains(b.Type))
                .Select(b =>
                {
                    b.Width = Math.Max(1, b.Width);
                    b.Height = Math.Max(1, b.Height);
                    return b;
                })
                .ToList();
        }
        catch
        {
            return new List<DashboardBlockInstance>();
        }
    }

    public static string SerializeLayout(IEnumerable<DashboardBlockInstance> blocks)
        => JsonSerializer.Serialize(blocks, SerializerOptions);
}

