namespace Compass.Helpers;

public static class PriorityStyleHelper
{
    public static string GetPriorityKey(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return "default";
        }

        var normalized = name.Trim().ToLowerInvariant();
        return normalized switch
        {
            "critical" or "urgent" or "very high" or "priority 0" => "critical",
            "high" or "priority 1" => "high",
            "medium" or "priority 2" => "medium",
            "low" or "priority 3" => "low",
            _ => "default"
        };
    }
}

