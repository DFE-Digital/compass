using System.Text.Json;

namespace Compass.Services.DemandPipeline;

/// <summary>Parse <see cref="Compass.Models.DemandPipeline.DemandPipelineTriageMeeting.AgendaJson"/> (Compass2-compatible shape).</summary>
public static class TriageAgendaJsonHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private class AgendaItemStored
    {
        public string? Type { get; set; }
        public string? Key { get; set; }
        public Guid? DemandId { get; set; }
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public int DurationMinutes { get; set; }
    }

    public static bool IsDemandAgendaItem(string? type) =>
        string.Equals(type, "Demand", StringComparison.OrdinalIgnoreCase);

    public static List<Guid> GetDemandIdsInAgenda(string? agendaJson)
    {
        if (string.IsNullOrWhiteSpace(agendaJson)) return new List<Guid>();
        try
        {
            var stored = JsonSerializer.Deserialize<List<AgendaItemStored>>(agendaJson, JsonOptions);
            return stored?
                       .Where(s => IsDemandAgendaItem(s.Type) && s.DemandId.HasValue)
                       .Select(s => s.DemandId!.Value)
                       .Distinct()
                       .ToList()
                   ?? new List<Guid>();
        }
        catch
        {
            return new List<Guid>();
        }
    }
}
