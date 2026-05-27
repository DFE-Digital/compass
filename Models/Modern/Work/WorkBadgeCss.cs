using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Compass.Models;

namespace Compass.Models.Modern.Work;

/// <summary>Renders work RAG badges like admin previews: <c>dfe-c-tag dfe-c-tag--*</c> using <see cref="RagStatusLookup.CssClass"/>.</summary>
public static class WorkBadgeCss
{
    /// <summary>Modifiers supported by DfE Frontend <see href="https://design.education.gov.uk/components/badge">dfe-f-badge</see>.</summary>
    private static readonly HashSet<string> DfeFrontendBadgeModifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "blue", "green", "teal", "purple", "magenta", "red", "orange", "brown", "black", "grey"
    };

    /// <summary>Allows safe token for <c>dfe-c-tag--{token}</c> (admin-entered).</summary>
    public static string? SafeTagModifier(string? cssClass)
    {
        if (string.IsNullOrWhiteSpace(cssClass)) return null;
        var t = cssClass.Trim();
        return Regex.IsMatch(t, @"^[a-zA-Z0-9_-]+$") ? t : null;
    }

    private const string RagDfeBadgeBase = "dfe-f-badge dfe-f-badge--small";

    /// <summary>RAG colour variant for <c>dfe-f-badge</c> (gov palette in <c>modern.css</c>).</summary>
    public static string RagDfeBadgeVariantClass(string? ragDisplayName)
    {
        if (string.IsNullOrWhiteSpace(ragDisplayName) || ragDisplayName == "—")
            return "dfe-f-badge--rag-none";

        var t = ragDisplayName.Trim();
        if (t.Equals("Red", StringComparison.OrdinalIgnoreCase))
            return "dfe-f-badge--rag-red";
        if (t.Equals("Amber-Red", StringComparison.OrdinalIgnoreCase) || t.Equals("Amber Red", StringComparison.OrdinalIgnoreCase))
            return "dfe-f-badge--rag-amber-red";
        if (t.Equals("Amber-Green", StringComparison.OrdinalIgnoreCase) || t.Equals("Amber Green", StringComparison.OrdinalIgnoreCase))
            return "dfe-f-badge--rag-amber-green";
        if (t.Equals("Green", StringComparison.OrdinalIgnoreCase))
            return "dfe-f-badge--rag-green";

        var n = t.ToLowerInvariant();
        if (n.Contains("amber") && n.Contains("red"))
            return "dfe-f-badge--rag-amber-red";
        if (n.Contains("red"))
            return "dfe-f-badge--rag-red";
        if (n.Contains("amber"))
            return "dfe-f-badge--rag-amber-green";
        if (n.Contains("green"))
            return "dfe-f-badge--rag-green";
        return "dfe-f-badge--rag-none";
    }

    /// <summary>Full RAG <c>dfe-f-badge</c> class list (base + gov colour variant).</summary>
    public static string RagBadgeClass(string? ragDisplayName)
        => RagDfeBadgeBase + " " + RagDfeBadgeVariantClass(ragDisplayName);

    /// <summary>Full <c>class</c> attribute value for current RAG (chrome header tags).</summary>
    public static string RagDfeTagClass(string? cssClassFromLookup, string? ragDisplayName)
        => RagBadgeClass(ragDisplayName);

    /// <summary>RAG <c>dfe-f-badge</c> with gov colour variant.</summary>
    public static string RagDfeFrontendBadgeClass(string? cssClassFromLookup, string? ragDisplayName)
        => RagBadgeClass(ragDisplayName);

    /// <summary>Compact RAG <c>dfe-f-badge</c> for dense reporting tables.</summary>
    public static string RagCompactBadgeClass(string? ragDisplayName)
        => RagBadgeClass(ragDisplayName);

    /// <summary>Colour-coded completion % badge for service register reporting tables.</summary>
    public static string ServiceRegisterCompletionBadgeClass(int completionPercent)
    {
        var baseClass = "dfe-f-badge dfe-f-badge--small";
        return completionPercent switch
        {
            >= 100 => baseClass + " dfe-f-badge--green",
            >= 67 => baseClass + " dfe-f-badge--blue",
            >= 34 => baseClass + " dfe-f-badge--orange",
            _ => baseClass + " dfe-f-badge--red"
        };
    }

    /// <summary>Count badge on monthly report toggle headers.</summary>
    public static string ToggleCountBadgeClass(int count, bool highlightWhenPositive = false, bool warnWhenPositive = false)
    {
        var baseClass = "dfe-f-badge dfe-f-badge--small";
        if (warnWhenPositive && count > 0)
            return baseClass + " dfe-f-badge--red";
        if (highlightWhenPositive && count > 0)
            return baseClass + " dfe-f-badge--blue";
        return baseClass + " dfe-f-badge--grey";
    }

    /// <summary>RAG change bucket toggle header (Improving / Same / Worsening).</summary>
    public static string RagChangeBucketToggleBadgeClass(string bucketTitle, int count)
    {
        var baseClass = "dfe-f-badge dfe-f-badge--small";
        if (count == 0)
            return baseClass + " dfe-f-badge--grey";
        return bucketTitle switch
        {
            "Worsening" => baseClass + " dfe-f-badge--rag-red",
            "Improving" => baseClass + " dfe-f-badge--rag-green",
            _ => baseClass + " dfe-f-badge--grey"
        };
    }

    /// <summary>Priority change bucket toggle header (Improving / Same / Worsening).</summary>
    public static string PriorityChangeBucketToggleBadgeClass(string bucketTitle, int count)
    {
        var baseClass = "dfe-f-badge dfe-f-badge--small";
        if (count == 0)
            return baseClass + " dfe-f-badge--grey";
        return bucketTitle switch
        {
            "Worsening" => baseClass + " dfe-f-badge--red",
            "Improving" => baseClass + " dfe-f-badge--green",
            _ => baseClass + " dfe-f-badge--grey"
        };
    }

    /// <summary>Six-month RAG trend toggle header (Stable / Improving / Worsening / Stale).</summary>
    public static string RagTrendToggleBadgeClass(string trendCategory)
    {
        var baseClass = "dfe-f-badge dfe-f-badge--small";
        return trendCategory switch
        {
            "Stable" => baseClass + " dfe-f-badge--blue",
            "Improving" => baseClass + " dfe-f-badge--rag-green",
            "Worsening" => baseClass + " dfe-f-badge--rag-red",
            "Stale" => baseClass + " dfe-f-badge--grey",
            _ => baseClass + " dfe-f-badge--grey"
        };
    }

    /// <summary>Bucket label for reporting (Critical, High, Medium, Low, Not Set).</summary>
    public static string PriorityBucket(string? priorityName)
    {
        if (string.IsNullOrWhiteSpace(priorityName) || priorityName == "—")
            return "Not Set";
        var priorityNameLower = priorityName.Trim().ToLowerInvariant();
        if (priorityNameLower.Contains("critical"))
            return "Critical";
        if (priorityNameLower.Contains("high"))
            return "High";
        if (priorityNameLower.Contains("medium"))
            return "Medium";
        if (priorityNameLower.Contains("low"))
            return "Low";
        return "Not Set";
    }

    /// <summary>Compact priority text for dense table badges (maps long lookup names e.g. Critical / Essential).</summary>
    public static string PriorityShortLabel(string? priorityName)
    {
        var bucket = PriorityBucket(priorityName);
        return bucket switch
        {
            "Not Set" => "Not set",
            _ => bucket
        };
    }

    /// <summary>Priority label as <c>dfe-f-badge</c> (work dashboard register).</summary>
    public static string PriorityDfeFrontendBadgeClass(string? priorityName)
    {
        if (string.IsNullOrWhiteSpace(priorityName))
            return "dfe-f-badge dfe-f-badge--grey";
        var s = priorityName.ToLowerInvariant();
        if (s.Contains("high") || s.Contains("critical")) return "dfe-f-badge dfe-f-badge--red";
        if (s.Contains("medium")) return "dfe-f-badge dfe-f-badge--orange";
        if (s.Contains("low")) return "dfe-f-badge dfe-f-badge--green";
        return "dfe-f-badge dfe-f-badge--grey";
    }

    /// <summary>Work status as <c>dfe-f-badge</c> (Active, Paused, Completed, Cancelled).</summary>
    public static string StatusDfeFrontendBadgeClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status) || status == "—")
            return "dfe-f-badge dfe-f-badge--grey";
        var s = status.Trim().ToLowerInvariant();
        if (s == "active") return "dfe-f-badge dfe-f-badge--green";
        if (s == "paused") return "dfe-f-badge dfe-f-badge--orange";
        if (s == "completed" || s == "complete") return "dfe-f-badge dfe-f-badge--green";
        if (s == "cancelled" || s == "canceled") return "dfe-f-badge dfe-f-badge--red";
        return "dfe-f-badge dfe-f-badge--grey";
    }

    /// <summary>Monthly reporting column on the work dashboard table.</summary>
    public static string MonthlyReportingStatusDfeFrontendBadgeClass(string? statusLabel)
    {
        return statusLabel switch
        {
            "Submitted" => "dfe-f-badge dfe-f-badge--green",
            "Draft" => "dfe-f-badge dfe-f-badge--orange",
            "Not due" => "dfe-f-badge dfe-f-badge--grey",
            _ => "dfe-f-badge dfe-f-badge--red",
        };
    }

    /// <summary>All work / register monthly column (<c>_WorkRegisterMonthlyUpdateCell</c>).</summary>
    public static string WorkRegisterMonthlyLabelDfeFrontendBadgeClass(string? label)
    {
        return label switch
        {
            "Submitted" => "dfe-f-badge dfe-f-badge--green",
            "Draft" => "dfe-f-badge dfe-f-badge--orange",
            "Not due" => "dfe-f-badge dfe-f-badge--grey",
            "Not started" => "dfe-f-badge dfe-f-badge--red",
            _ => "dfe-f-badge dfe-f-badge--grey",
        };
    }

    /// <summary>Performance commission product submission status (<see cref="CommissionSubmissionStatus"/>).</summary>
    public static string CommissionSubmissionDfeFrontendBadgeClass(CommissionSubmissionStatus s) => s switch
    {
        CommissionSubmissionStatus.Submitted => "dfe-f-badge dfe-f-badge--green",
        CommissionSubmissionStatus.Late => "dfe-f-badge dfe-f-badge--red",
        CommissionSubmissionStatus.InProgress => "dfe-f-badge dfe-f-badge--orange",
        _ => "dfe-f-badge dfe-f-badge--grey",
    };

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

    /// <summary>Milestone progress label on work detail (plain-language status).</summary>
    public static string MilestoneProgressDfeFrontendBadgeClass(string? progressName)
    {
        if (string.IsNullOrEmpty(progressName))
            return "dfe-f-badge dfe-f-badge--grey";
        var n = progressName.Trim().ToLowerInvariant();
        if (n.Contains("reached") && n.Contains("late")) return "dfe-f-badge dfe-f-badge--red";
        if (n.Contains("completed")) return "dfe-f-badge dfe-f-badge--blue";
        if (n.Contains("at risk")) return "dfe-f-badge dfe-f-badge--orange";
        if (n.Contains("not started")) return "dfe-f-badge dfe-f-badge--grey";
        if (n.Contains("in progress") || n.Contains("on track")) return "dfe-f-badge dfe-f-badge--green";
        return "dfe-f-badge dfe-f-badge--grey";
    }

    /// <summary>Delivery phase as <c>dfe-f-badge</c> (dashboard / detail).</summary>
    public static string PhaseDfeFrontendBadgeClass(string? phaseName)
    {
        if (string.IsNullOrWhiteSpace(phaseName) || phaseName == "—")
            return "dfe-f-badge dfe-f-badge--grey";
        var n = phaseName.Trim().ToLowerInvariant();
        if (n.Contains("public") && n.Contains("beta")) return "dfe-f-badge dfe-f-badge--orange";
        if (n.Contains("private") && n.Contains("beta")) return "dfe-f-badge dfe-f-badge--orange";
        if (n.Contains("alpha")) return "dfe-f-badge dfe-f-badge--blue";
        if (n.Contains("live")) return "dfe-f-badge dfe-f-badge--green";
        if (n.Contains("retired")) return "dfe-f-badge dfe-f-badge--grey";
        if (n.Contains("discovery")) return "dfe-f-badge dfe-f-badge--teal";
        if (n.Contains("explore")) return "dfe-f-badge dfe-f-badge--purple";
        return "dfe-f-badge dfe-f-badge--blue";
    }

    /// <summary>Resourcing band as <c>dfe-f-badge</c> using an admin-configured colour modifier.</summary>
    public static string ResourceBandDfeFrontendBadgeClass(string? cssClassOrModifier)
    {
        const string baseClass = "dfe-f-badge dfe-f-badge--small";
        if (string.IsNullOrWhiteSpace(cssClassOrModifier))
            return baseClass + " dfe-f-badge--grey";

        var token = cssClassOrModifier.Trim();
        if (token.StartsWith("dfe-f-badge--", StringComparison.OrdinalIgnoreCase))
            token = token["dfe-f-badge--".Length..];

        token = token.ToLowerInvariant();
        if (!DfeFrontendBadgeModifiers.Contains(token))
            return baseClass + " dfe-f-badge--grey";

        return $"{baseClass} dfe-f-badge--{token}";
    }

    /// <summary>Raid risk/issue status (open = blue, closed = green).</summary>
    public static string RaidOpenClosedDfeFrontendBadgeClass(bool closed)
        => closed ? "dfe-f-badge dfe-f-badge--green" : "dfe-f-badge dfe-f-badge--blue";

    /// <summary>Risk status on RAID registers (escalated, closed, proposed, open).</summary>
    public static string RaidRiskStatusDfeFrontendBadgeClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        var s = status.ToLowerInvariant();
        if (s.Contains("escalat"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--red";
        if (s.Contains("closed"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--green";
        if (s.Contains("proposed"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        return "dfe-f-badge dfe-f-badge--small dfe-f-badge--blue";
    }

    /// <summary>Issue status on RAID registers.</summary>
    public static string RaidIssueStatusDfeFrontendBadgeClass(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        var closed = status.Contains("closed", StringComparison.OrdinalIgnoreCase);
        return RaidOpenClosedDfeFrontendBadgeClass(closed) + " dfe-f-badge--small";
    }

    /// <summary>Raid priority text (critical/high/medium/low).</summary>
    public static string RaidPriorityLabelDfeFrontendBadgeClass(string? pri)
    {
        if (string.IsNullOrWhiteSpace(pri)) return "dfe-f-badge dfe-f-badge--grey";
        var p = pri.ToLowerInvariant();
        if (p.Contains("critical")) return "dfe-f-badge dfe-f-badge--red";
        if (p.Contains("high")) return "dfe-f-badge dfe-f-badge--orange";
        if (p.Contains("medium")) return "dfe-f-badge dfe-f-badge--blue";
        if (p.Contains("low")) return "dfe-f-badge dfe-f-badge--grey";
        return "dfe-f-badge dfe-f-badge--grey";
    }

    /// <summary>Risk appetite label on work detail / governance (banded like legacy risk-appetite tags).</summary>
    public static string RiskAppetiteDfeFrontendBadgeClass(string? name, List<LookupOption> opts, int? riskAppetiteId)
    {
        if (string.IsNullOrWhiteSpace(name) || name == "—" || !riskAppetiteId.HasValue)
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        var selected = opts.FirstOrDefault(o => o.Id == riskAppetiteId.Value);
        var label = (selected?.Name ?? selected?.Value ?? name).Trim();
        if (string.IsNullOrEmpty(label))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        var n = label.ToLowerInvariant();
        if (n.Contains("averse") || n.Contains("minimal") || n.Contains("cautious"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        if (n.Contains("medium") || n.Contains("moderate") || n.Contains("balanced"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--blue";
        if (n.Contains("open") || n.Contains("elevated"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--green";
        if (n.Contains("high"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--green";
        if (n.Contains("low"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        var idx = opts.FindIndex(o => o.Id == riskAppetiteId.Value);
        if (idx < 0 || opts.Count <= 1)
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--blue";
        var band = idx * 3 / opts.Count;
        if (band == 0)
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        if (band >= 2)
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--green";
        return "dfe-f-badge dfe-f-badge--small dfe-f-badge--blue";
    }

    /// <summary>RAID tier column on work detail risks register.</summary>
    public static string RaidTierDfeFrontendBadgeClass(string? tier)
    {
        if (string.IsNullOrWhiteSpace(tier))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        var t = tier.Trim().ToLowerInvariant();
        if (t.Contains("proposed"))
        {
            if (t.Contains("1") || t.Contains("tier one") || t == "t1")
                return "dfe-f-badge dfe-f-badge--small dfe-f-badge--blue";
            if (t.Contains("2") || t.Contains("tier two") || t == "t2")
                return "dfe-f-badge dfe-f-badge--small dfe-f-badge--orange";
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        }
        if (t.Contains("3") || t.Contains("tier three") || t == "t3")
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--red";
        if (t.Contains("2") || t.Contains("tier two") || t == "t2")
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--orange";
        if (t.Contains("1") || t.Contains("tier one") || t == "t1")
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--blue";
        return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
    }

    /// <summary>Likelihood or impact label (lookup text or 1–5 style rating) as <c>dfe-f-badge</c> on RAID registers.</summary>
    public static string RaidRiskLikelihoodImpactLabelDfeFrontendBadgeClass(string? label)
    {
        if (string.IsNullOrWhiteSpace(label))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        var s = label.Trim().ToLowerInvariant();
        if (int.TryParse(s, out var n))
        {
            if (n <= 0) return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
            if (n <= 2) return "dfe-f-badge dfe-f-badge--small dfe-f-badge--green";
            if (n == 3) return "dfe-f-badge dfe-f-badge--small dfe-f-badge--orange";
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--red";
        }

        if (s is "very low" or "negligible" or "rare" or "minimal")
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--green";
        if (s.Contains("very low") || s.Contains("very small") || s.Contains("negligible") || s.Contains("rare"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--green";
        if (s == "low" || s == "small" || s == "minor" || s == "unlikely")
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--green";
        if (s.Contains("medium") || s.Contains("moderate") || s.Contains("possible"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--orange";
        if (s.Contains("very high") || s.Contains("certain") || s.Contains("severe") || s.Contains("catastrophic"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--red";
        if (s.Contains("high") || s.Contains("major") || s.Contains("likely") || s.Contains("critical"))
            return "dfe-f-badge dfe-f-badge--small dfe-f-badge--red";
        return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
    }

    /// <summary>Inherent risk score (0–25) as <c>dfe-f-badge</c>; bands match work detail RAID register score pills.</summary>
    public static string RaidRiskScoreDfeFrontendBadgeClass(int score)
    {
        if (score >= 20) return "dfe-f-badge dfe-f-badge--small dfe-f-badge--red";
        if (score >= 15) return "dfe-f-badge dfe-f-badge--small dfe-f-badge--orange";
        if (score >= 8) return "dfe-f-badge dfe-f-badge--small dfe-f-badge--orange";
        if (score <= 0) return "dfe-f-badge dfe-f-badge--small dfe-f-badge--grey";
        return "dfe-f-badge dfe-f-badge--small dfe-f-badge--green";
    }

    /// <summary>Raid severity text.</summary>
    public static string RaidSeverityLabelDfeFrontendBadgeClass(string? sev)
    {
        if (string.IsNullOrWhiteSpace(sev)) return "dfe-f-badge dfe-f-badge--grey";
        var s = sev.ToLowerInvariant();
        if (s.Contains("critical")) return "dfe-f-badge dfe-f-badge--red";
        if (s.Contains("major")) return "dfe-f-badge dfe-f-badge--orange";
        return "dfe-f-badge dfe-f-badge--green";
    }

    /// <summary>Raid assumption criticality (high / medium / low).</summary>
    public static string RaidAssumptionCriticalityDfeFrontendBadgeClass(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return "dfe-f-badge dfe-f-badge--grey";
        var s = label.ToLowerInvariant();
        if (s.Contains("high")) return "dfe-f-badge dfe-f-badge--red";
        if (s.Contains("medium")) return "dfe-f-badge dfe-f-badge--orange";
        if (s.Contains("low")) return "dfe-f-badge dfe-f-badge--green";
        return "dfe-f-badge dfe-f-badge--grey";
    }

    /// <summary>Raid dependency link type label.</summary>
    public static string RaidDependencyLinkTypeDfeFrontendBadgeClass(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return "dfe-f-badge dfe-f-badge--grey";
        return "dfe-f-badge dfe-f-badge--blue";
    }

    /// <summary>Raid dependency criticality (very high / high / medium / low).</summary>
    public static string RaidDependencyCriticalityDfeFrontendBadgeClass(string? label)
    {
        if (string.IsNullOrWhiteSpace(label)) return "dfe-f-badge dfe-f-badge--grey";
        var s = label.ToLowerInvariant();
        if (s.Contains("very") && s.Contains("high")) return "dfe-f-badge dfe-f-badge--red";
        if (s.Contains("high")) return "dfe-f-badge dfe-f-badge--red";
        if (s.Contains("medium")) return "dfe-f-badge dfe-f-badge--orange";
        if (s.Contains("low")) return "dfe-f-badge dfe-f-badge--green";
        return "dfe-f-badge dfe-f-badge--grey";
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
