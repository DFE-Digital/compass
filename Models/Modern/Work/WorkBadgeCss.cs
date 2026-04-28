using System.Text.RegularExpressions;

namespace Compass.Models.Modern.Work;

/// <summary>Renders work RAG badges like admin previews: <c>dfe-c-tag dfe-c-tag--*</c> using <see cref="RagStatusLookup.CssClass"/>.</summary>
public static class WorkBadgeCss
{
    /// <summary>Allows safe token for <c>dfe-c-tag--{token}</c> (admin-entered).</summary>
    public static string? SafeTagModifier(string? cssClass)
    {
        if (string.IsNullOrWhiteSpace(cssClass)) return null;
        var t = cssClass.Trim();
        return Regex.IsMatch(t, @"^[a-zA-Z0-9_-]+$") ? t : null;
    }

    /// <summary>Full <c>class</c> attribute value for current RAG (matches <c>Admin → Rag definitions</c> preview).</summary>
    public static string RagDfeTagClass(string? cssClassFromLookup, string? ragDisplayName)
    {
        var safe = SafeTagModifier(cssClassFromLookup);
        if (!string.IsNullOrEmpty(safe))
            return "dfe-c-tag dfe-c-tag--" + safe;
        return RagDfeTagClassFallback(ragDisplayName);
    }

    private static string RagDfeTagClassFallback(string? ragName)
    {
        if (string.IsNullOrEmpty(ragName) || ragName == "—")
            return "dfe-c-tag dfe-c-tag--grey";
        var n = ragName.ToLowerInvariant();
        if (n.Contains("amber") && n.Contains("red")) return "dfe-c-tag dfe-c-tag--orange";
        if (n.Contains("red")) return "dfe-c-tag dfe-c-tag--red";
        if (n.Contains("amber")) return "dfe-c-tag dfe-c-tag--amber";
        if (n.Contains("green")) return "dfe-c-tag dfe-c-tag--green";
        return "dfe-c-tag dfe-c-tag--grey";
    }

    /// <summary>Work status as <c>dfe-c-tag dfe-c-tag--*</c> (Active, Paused, Completed, Cancelled).</summary>
    public static string StatusDfeTagClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status == "—")
            return "dfe-c-tag dfe-c-tag--grey";
        var s = status.Trim().ToLowerInvariant();
        if (s == "active") return "dfe-c-tag dfe-c-tag--green";
        if (s == "paused") return "dfe-c-tag dfe-c-tag--amber";
        if (s == "completed" || s == "complete") return "dfe-c-tag dfe-c-tag--green dfe-c-tag--work-completed";
        if (s == "cancelled" || s == "canceled") return "dfe-c-tag dfe-c-tag--red";
        return "dfe-c-tag dfe-c-tag--grey";
    }

    /// <summary>Milestone row status: <c>not_started</c>, <c>on_track</c>, <c>at_risk</c>, <c>delayed</c>, <c>complete</c>, <c>cancelled</c>.</summary>
    public static string MilestoneStatusDfeTagClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status == "—")
            return "dfe-c-tag dfe-c-tag--grey";
        var s = status.Trim().ToLowerInvariant().Replace(" ", "_");
        if (s == "not_started") return "dfe-c-tag dfe-c-tag--grey";
        if (s == "on_track") return "dfe-c-tag dfe-c-tag--green";
        if (s == "at_risk") return "dfe-c-tag dfe-c-tag--amber";
        if (s == "delayed") return "dfe-c-tag dfe-c-tag--red";
        if (s == "complete") return "dfe-c-tag dfe-c-tag--green-strong";
        if (s == "cancelled" || s == "canceled") return "dfe-c-tag dfe-c-tag--red";
        return "dfe-c-tag dfe-c-tag--grey";
    }

    /// <summary>Delivery phase as <c>dfe-c-tag</c> (aligned with RAG / priority styling on work detail).</summary>
    public static string PhaseDfeTagClass(string? phaseName)
    {
        if (string.IsNullOrWhiteSpace(phaseName) || phaseName == "—")
            return "dfe-c-tag dfe-c-tag--grey";
        var n = phaseName.Trim().ToLowerInvariant();
        if (n.Contains("public") && n.Contains("beta")) return "dfe-c-tag dfe-c-tag--amber";
        if (n.Contains("private") && n.Contains("beta")) return "dfe-c-tag dfe-c-tag--orange";
        if (n.Contains("alpha")) return "dfe-c-tag dfe-c-tag--blue";
        if (n.Contains("live")) return "dfe-c-tag dfe-c-tag--green";
        if (n.Contains("retired")) return "dfe-c-tag dfe-c-tag--grey";
        if (n.Contains("discovery")) return "dfe-c-tag dfe-c-tag--teal";
        if (n.Contains("explore")) return "dfe-c-tag dfe-c-tag--navy";
        return "dfe-c-tag dfe-c-tag--navy";
    }

    /// <summary>Delivery priority as <c>dfe-c-tag</c> (high/critical → red, medium → amber, low → green).</summary>
    public static string PriorityDfeTagClass(string? name)
    {
        if (string.IsNullOrEmpty(name)) return "dfe-c-tag dfe-c-tag--grey";
        var s = name.ToLowerInvariant();
        if (s.Contains("high") || s.Contains("critical")) return "dfe-c-tag dfe-c-tag--red";
        if (s.Contains("medium")) return "dfe-c-tag dfe-c-tag--amber";
        if (s.Contains("low")) return "dfe-c-tag dfe-c-tag--green";
        return "dfe-c-tag dfe-c-tag--grey";
    }
}
