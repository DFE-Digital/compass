using System.Collections.Generic;

namespace Compass.Data;

public sealed record RaidLookupSeedItem(string Code, string Label, string? Description, int SortOrder);

/// <summary>Default seed values for RAID admin lookups, aligned to <c>compass/documentation/raid.md</c>.</summary>
public static class RaidLookupSeedData
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<RaidLookupSeedItem>> _definitions =
        new Dictionary<string, IReadOnlyList<RaidLookupSeedItem>>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["risk-statuses"] = new[]
            {
                Item("PROP", "Proposed", "Identified but not yet triaged.", 10),
                Item("OPEN", "Open", "Accepted into the RAID register.", 20),
                Item("ACTIVE", "Active", "Being actively managed.", 30),
                Item("MON", "Monitoring", "Controlled; under watch.", 40),
                Item("ESC", "Escalated", "Escalated to higher governance.", 50),
                Item("DEESC", "De-escalated", "Returned from escalation.", 60),
                Item("MAT", "Materialised", "Risk has become an issue.", 70),
                Item("CLOSED", "Closed", "Closed with rationale.", 80)
            },
            ["risk-priorities"] = new[]
            {
                Item("CRIT", "Critical", null, 10),
                Item("HIGH", "High", null, 20),
                Item("MED", "Medium", null, 30),
                Item("LOW", "Low", null, 40)
            },
            ["risk-likelihoods"] = new[]
            {
                Item("VL", "Very Likely", null, 10),
                Item("LK", "Likely", null, 20),
                Item("POS", "Possible", null, 30),
                Item("UNL", "Unlikely", null, 40),
                Item("VU", "Very Unlikely", null, 50)
            },
            ["risk-impact-levels"] = new[]
            {
                Item("CRISIS", "Crisis", null, 10),
                Item("CRIT", "Critical", null, 20),
                Item("MOD", "Moderate", null, 30),
                Item("MARG", "Marginal", null, 40),
                Item("NEGL", "Negligible", null, 50)
            },
            ["risk-proximities"] = new[]
            {
                Item("M06", "Within 6 months", null, 10),
                Item("M612", "6 to 12 months", null, 20),
                Item("Y12", "1 to 2 years", null, 30),
                Item("Y2P", "Beyond 2 years", null, 40),
                Item("UNK", "Unknown", null, 50),
                Item("NOW", "Now", null, 60)
            },
            ["risk-treatments"] = new[]
            {
                Item("AVOID", "Avoid", null, 10),
                Item("MITIGATE", "Mitigate", null, 20),
                Item("ACCEPT", "Accept", null, 30),
                Item("FALLBACK", "Fallback", null, 40),
                Item("TRANSFER", "Transfer", null, 50),
                Item("SHARE", "Share", null, 60),
                Item("EXPLOIT", "Exploit", null, 70)
            },
            ["risk-appetites"] = new[]
            {
                Item("EAGER", "Eager", null, 10),
                Item("OPEN", "Open", null, 20),
                Item("CAUT", "Cautious", null, 30),
                Item("MIN", "Minimalist", null, 40),
                Item("AVERSE", "Averse", null, 50)
            },
            ["risk-categories"] = new[]
            {
                Item("CAP", "Capital", null, 10),
                Item("COM", "Commercial", null, 20),
                Item("DAT", "Data and information Management", null, 30),
                Item("FIN", "Financial", null, 40),
                Item("FRD", "Fraud", null, 50),
                Item("GOV", "Governance", null, 60),
                Item("LEG", "Legal", null, 70),
                Item("OPD", "Operational Delivery", null, 80),
                Item("PPL", "People", null, 90),
                Item("POL", "Policy", null, 100),
                Item("PRP", "Project and Programme", null, 110),
                Item("PRY", "Property", null, 120),
                Item("REP", "Reputation", null, 130),
                Item("SEC", "Security", null, 140),
                Item("STR", "Strategy", null, 150),
                Item("SUS", "Sustainability and Climate Change", null, 160),
                Item("TEC", "Technology and systems", null, 170)
            },
            ["issue-statuses"] = new[]
            {
                Item("PROP", "Proposed", null, 10),
                Item("OPEN", "Open", null, 20),
                Item("ACT", "Active", null, 30),
                Item("RES", "Being resolved", null, 40),
                Item("ESC", "Escalated", null, 50),
                Item("DEESC", "De-escalated", null, 60),
                Item("RESOLV", "Resolved", null, 70),
                Item("CLOSED", "Closed", null, 80)
            },
            ["issue-priorities"] = new[]
            {
                Item("CRIT", "Critical", null, 10),
                Item("HIGH", "High", null, 20),
                Item("MED", "Medium", null, 30),
                Item("LOW", "Low", null, 40)
            },
            ["issue-severities"] = new[]
            {
                Item("MAJ", "Major", null, 10),
                Item("MED", "Medium", null, 20),
                Item("MIN", "Minor", null, 30)
            },
            ["issue-categories"] = new[]
            {
                Item("FIN", "Financial", null, 10),
                Item("OPD", "Operational Delivery", null, 20),
                Item("PRP", "Project and Programme", null, 30),
                Item("REP", "Reputation", null, 40),
                Item("LEG", "Legal", null, 50),
                Item("FRD", "Fraud", null, 60),
                Item("COM", "Commercial", null, 70),
                Item("DAT", "Data and information Management", null, 80),
                Item("TEC", "Technology and systems", null, 90),
                Item("PPL", "People", null, 100),
                Item("SUS", "Sustainability and Climate Change", null, 110),
                Item("POL", "Policy", null, 120),
                Item("CAP", "Capital", null, 130),
                Item("GOV", "Governance", null, 140),
                Item("PRY", "Property", null, 150),
                Item("SEC", "Security", null, 160),
                Item("STR", "Strategy", null, 170)
            },
            ["action-statuses"] = new[]
            {
                Item("OPEN", "Open", null, 10),
                Item("PROG", "In progress", null, 20),
                Item("BLOCK", "Blocked", null, 30),
                Item("DONE", "Completed", null, 40),
                Item("CANC", "Cancelled", null, 50)
            },
            ["action-priorities"] = new[]
            {
                Item("CRIT", "Critical", null, 10),
                Item("HIGH", "High", null, 20),
                Item("MED", "Medium", null, 30),
                Item("LOW", "Low", null, 40)
            },
            ["action-types"] = new[]
            {
                Item("MIT", "Mitigation", null, 10),
                Item("RES", "Resolution", null, 20),
                Item("IMPL", "Implementation", null, 30),
                Item("GOV", "Governance", null, 40),
                Item("OPS", "Operational", null, 50)
            },
            ["action-categories"] = new[]
            {
                Item("DEL", "Delivery", null, 10),
                Item("TEC", "Technology", null, 20),
                Item("DATA", "Data", null, 30),
                Item("PEO", "People", null, 40),
                Item("ASS", "Assurance", null, 50)
            },
            ["action-impact-levels"] = new[]
            {
                Item("CRISIS", "Crisis", null, 10),
                Item("CRIT", "Critical", null, 20),
                Item("MOD", "Moderate", null, 30),
                Item("MARG", "Marginal", null, 40),
                Item("NEGL", "Negligible", null, 50)
            },
            ["decision-statuses"] = new[]
            {
                Item("DRAFT", "Draft", null, 10),
                Item("PROP", "Proposed", null, 20),
                Item("APP", "Approved", null, 30),
                Item("REJ", "Rejected", null, 40),
                Item("SUP", "Superseded", null, 50),
                Item("CLOSED", "Closed", null, 60)
            },
            ["decision-priorities"] = new[]
            {
                Item("CRIT", "Critical", null, 10),
                Item("HIGH", "High", null, 20),
                Item("MED", "Medium", null, 30),
                Item("LOW", "Low", null, 40)
            },
            ["decision-outcomes"] = new[]
            {
                Item("APPROVE", "Approve", "Proposal is accepted as presented.", 10),
                Item("APPROVE_COND", "Approve with conditions", "Accepted subject to defined conditions.", 20),
                Item("REJECT", "Reject", "Proposal not accepted.", 30),
                Item("RMOREINFO", "Request more information", "Deferred pending further evidence.", 40),
                Item("DEFER", "Defer", "Decision will be revisited later.", 50),
                Item("ESCALATE", "Escalate", "Escalated to a higher governance body.", 60),
                Item("CHANGE", "Change approach", "Proposal must be reworked or alternative chosen.", 70),
                Item("PAUSE", "Pause work", "Work paused pending further review.", 80),
                Item("PROCEED", "Proceed to next stage", "Move forward to the next phase.", 90)
            },
            ["decision-implementation-statuses"] = new[]
            {
                Item("NOTST", "Not started", null, 10),
                Item("INPROG", "In progress", null, 20),
                Item("BLOCK", "Blocked", null, 30),
                Item("PENDV", "Pending verification", null, 40),
                Item("DONE", "Completed", null, 50)
            },
            ["governance-boards"] = new[]
            {
                Item("SDB", "Service Design Board", "Oversees service design and standards alignment.", 10),
                Item("TRIAGE", "Central Ops Triage Board", "Prioritises requests and demand.", 20),
                Item("DDB", "Digital Delivery Board", "Oversees delivery progress and risk.", 30),
                Item("ARB", "Architecture Review Board", "Reviews technical and architectural decisions.", 40),
                Item("SAP", "Service Assessment Panel", "Formal service assessment and assurance forum.", 50),
                Item("ASG", "Accessibility Steering Group", "Oversees accessibility and inclusive design risk.", 60),
                Item("PORTF", "Portfolio Board", "Portfolio-level risk, dependency and benefits oversight.", 70),
                Item("SMT", "Senior Management Team", "Senior strategic decisions and major risks.", 80)
            },
            ["raid-evidence-types"] = new[]
            {
                Item("DOC", "Document", "Formal document, report or paper.", 10),
                Item("LINK", "URL / Link", "Web page, Confluence, GitHub, etc.", 20),
                Item("SCREEN", "Screenshot", "Image evidence of behaviour or issue.", 30),
                Item("AUDIT", "Audit output", "External or internal audit report.", 40),
                Item("TEST", "Test results", "Automated or manual test evidence.", 50),
                Item("PR", "Code change / PR", "Link to pull request or code change.", 60),
                Item("MIN", "Meeting notes", "Notes/minutes from workshop or board.", 70)
            },
            ["action-reminder-frequencies"] = new[]
            {
                Item("NONE", "None", "No automated reminders.", 10),
                Item("DAILY", "Daily", "Reminder every day until closed.", 20),
                Item("WEEK", "Weekly", "Reminder every week until closed.", 30),
                Item("BIWK", "Bi-weekly", "Reminder every two weeks until closed.", 40),
                Item("MONTH", "Monthly", "Reminder every month until closed.", 50)
            },
            ["action-escalation-thresholds"] = new[]
            {
                Item("D7", "7 days", "Escalate when overdue by 7 days.", 10),
                Item("D14", "14 days", "Escalate when overdue by 14 days.", 20),
                Item("D30", "30 days", "Escalate when overdue by 30 days.", 30),
                Item("D60", "60 days", "Escalate when overdue by 60 days.", 40)
            },
            ["demand-request-statuses"] = new[]
            {
                Item("DRAFT", "Draft", null, 10),
                Item("SUB", "Submitted", null, 20),
                Item("REV", "In review", null, 30),
                Item("ACC", "Accepted", null, 40),
                Item("REJ", "Rejected", null, 50)
            },
            ["triage-outcome-stages"] = new[]
            {
                Item("ACT", "Active", null, 10),
                Item("DRF", "Draft", null, 20),
                Item("IND", "In delivery", null, 30),
                Item("REJ", "Rejected", null, 40),
                Item("PAU", "Paused", null, 50)
            },
            ["assumption-statuses"] = new[]
            {
                Item("ACT", "Active", null, 10),
                Item("REV", "Under review", null, 20),
                Item("VAL", "Validated", null, 30),
                Item("INV", "Invalidated", null, 40),
                Item("CLOSED", "Closed", null, 50)
            },
            ["assumption-criticalities"] = new[]
            {
                Item("VH", "Very High", null, 10),
                Item("H", "High", null, 20),
                Item("M", "Medium", null, 30),
                Item("L", "Low", null, 40)
            },
            ["dependency-criticalities"] = new[]
            {
                Item("VH", "Very High", null, 10),
                Item("H", "High", null, 20),
                Item("M", "Medium", null, 30),
                Item("L", "Low", null, 40)
            },
            ["dependency-link-types"] = new[]
            {
                Item("INT", "Internal team", null, 10),
                Item("XD", "Cross-department", null, 20),
                Item("EXT", "External supplier", null, 30),
                Item("TEC", "Technology", null, 40),
                Item("POL", "Policy", null, 50),
                Item("DATA", "Data", null, 60),
                Item("OPS", "Operational", null, 70)
            }
        };

    public static bool TryGetValues(string lookupKey, out IReadOnlyList<RaidLookupSeedItem> values) =>
        _definitions.TryGetValue(lookupKey, out values);

    public static IReadOnlyDictionary<string, IReadOnlyList<RaidLookupSeedItem>> Definitions => _definitions;

    private static RaidLookupSeedItem Item(string code, string label, string? description, int sortOrder) =>
        new(code, label, description, sortOrder);
}
