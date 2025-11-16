using System.Collections.Generic;

namespace Compass.Data;

public sealed record RaidLookupSeedItem(string Code, string Label, string? Description, int SortOrder);

public static class RaidLookupSeedData
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyList<RaidLookupSeedItem>> _definitions =
        new Dictionary<string, IReadOnlyList<RaidLookupSeedItem>>(System.StringComparer.OrdinalIgnoreCase)
        {
            ["risk-statuses"] = new[]
            {
                Item("OPEN", "Open", "Risk has been logged but not yet analysed or mitigated.", 10),
                Item("ANALYSING", "Analysing", "Being assessed for impact, likelihood, tier and owner.", 20),
                Item("MITIGATING", "Mitigating", "Mitigation actions are in progress.", 30),
                Item("MONITORING", "Monitoring", "Controls in place; risk remains but is being watched.", 40),
                Item("ESCALATED", "Escalated", "Escalated to a higher governance level.", 50),
                Item("CLOSED", "Closed", "Risk is no longer relevant or has been fully addressed.", 60)
            },
            ["risk-priorities"] = new[]
            {
                Item("LOW", "Low", "Limited impact; manage locally.", 10),
                Item("MEDIUM", "Medium", "Noticeable impact; requires tracking and mitigation.", 20),
                Item("HIGH", "High", "Significant impact; active mitigation and regular review.", 30),
                Item("CRIT", "Critical", "Departmental-level impact; urgent mitigation and escalation.", 40)
            },
            ["risk-likelihoods"] = new[]
            {
                Item("RARE", "Rare", "Very unlikely to occur.", 10),
                Item("UNLIKELY", "Unlikely", "Could occur at some time but not expected.", 20),
                Item("POSSIBLE", "Possible", "Might occur at least once.", 30),
                Item("LIKELY", "Likely", "Will probably occur in most circumstances.", 40),
                Item("ALMOST", "Almost certain", "Expected to occur frequently.", 50)
            },
            ["risk-impact-levels"] = new[]
            {
                Item("MINOR", "Minor", "Limited impact; small localised effect.", 10),
                Item("MODERATE", "Moderate", "Noticeable impact; manageable within team/portfolio.", 20),
                Item("SIGNIFICANT", "Significant", "Major effect on delivery or service quality.", 30),
                Item("MAJOR", "Major", "Serious departmental impact; requires senior attention.", 40),
                Item("CRITICAL", "Critical", "Severe or systemic impact; unacceptable risk.", 50)
            },
            ["risk-proximities"] = new[]
            {
                Item("IMMINENT", "Imminent", "Expected within 0–3 months.", 10),
                Item("NEAR", "Near term", "Expected within 3–12 months.", 20),
                Item("MEDIUM", "Medium term", "Expected within 1–2 years.", 30),
                Item("LONG", "Long term", "Beyond 2 years.", 40),
                Item("UNKNOWN", "Unknown", "Timescale uncertain.", 50)
            },
            ["risk-categories"] = new[]
            {
                Item("DEL", "Delivery", "Timelines, milestones, resourcing, delivery quality.", 10),
                Item("TECH", "Technology", "Platforms, infrastructure, technical debt, resilience.", 20),
                Item("DATA", "Data & Reporting", "Data quality, availability, or reporting risk.", 30),
                Item("ACCESS", "Accessibility", "WCAG compliance, inclusive design, assistive tech support.", 40),
                Item("SEC", "Security", "Information or cyber security risk.", 50),
                Item("POL", "Policy & Legislative", "Policy alignment, legal or regulatory change.", 60),
                Item("PEOPLE", "People & Capability", "Skills, capacity, roles, or staffing.", 70),
                Item("FIN", "Financial", "Budget, cost overrun, or funding risk.", 80)
            },
            ["issue-statuses"] = new[]
            {
                Item("OPEN", "Open", "Logged but not yet triaged.", 10),
                Item("TRIAGED", "Triaged", "Assessed and prioritised.", 20),
                Item("INPROG", "In progress", "Being actively worked on.", 30),
                Item("BLOCKED", "Blocked", "Cannot progress due to a constraint.", 40),
                Item("PENDING", "Pending review", "Work done; awaiting verification.", 50),
                Item("RESOLVED", "Resolved", "Fix applied; verification completed.", 60),
                Item("CLOSED", "Closed", "Fully closed with evidence.", 70)
            },
            ["issue-priorities"] = new[]
            {
                Item("LOW", "Low", "Limited impact; resolve when convenient.", 10),
                Item("MEDIUM", "Medium", "Needs action but not urgent.", 20),
                Item("HIGH", "High", "Time-sensitive; requires prompt resolution.", 30),
                Item("CRIT", "Critical", "Severe impact; immediate attention required.", 40)
            },
            ["issue-severities"] = new[]
            {
                Item("S1", "Minor", "Localised, low impact on users.", 10),
                Item("S2", "Moderate", "Affects a subset of users or non-core journeys.", 20),
                Item("S3", "Major", "Affects critical journeys or large user groups.", 30),
                Item("S4", "Critical", "Service-wide or regulatory failure.", 40)
            },
            ["issue-categories"] = new[]
            {
                Item("DEFECT", "Defect / Bug", "Functionality not working as intended.", 10),
                Item("ACCESS", "Accessibility", "Fails accessibility or inclusive design expectations.", 20),
                Item("CONTENT", "Content", "Errors or gaps in content or guidance.", 30),
                Item("PERFORMANCE", "Performance", "Latency, reliability, or scalability issue.", 40),
                Item("SECURITY", "Security", "Vulnerability or security incident.", 50),
                Item("PROCESS", "Process", "Gap or failure in process or controls.", 60)
            },
            ["action-statuses"] = new[]
            {
                Item("OPEN", "Open", "Logged but work has not started.", 10),
                Item("INPROG", "In progress", "Work is underway.", 20),
                Item("BLOCKED", "Blocked", "Cannot progress due to a dependency or constraint.", 30),
                Item("PENDING", "Pending verification", "Awaiting confirmation that work is complete.", 40),
                Item("DONE", "Completed", "Action completed successfully.", 50),
                Item("CANC", "Cancelled", "Action no longer required.", 60)
            },
            ["action-priorities"] = new[]
            {
                Item("LOW", "Low", "Can be managed as part of routine work.", 10),
                Item("MEDIUM", "Medium", "Should be planned into team work.", 20),
                Item("HIGH", "High", "Needs near-term attention.", 30),
                Item("CRIT", "Critical", "Time-critical; must be prioritised.", 40)
            },
            ["action-types"] = new[]
            {
                Item("MITIGATE", "Mitigation", "Reduces the likelihood or impact of a risk.", 10),
                Item("RESOLVE", "Resolution", "Fixes or contains an active issue.", 20),
                Item("IMPL", "Implementation", "Implements an agreed decision.", 30),
                Item("FOLLOWUP", "Follow-up", "Additional activity after a meeting or review.", 40),
                Item("GOVERN", "Governance", "Governance/assurance activity.", 50),
                Item("OPER", "Operational", "BAU/operational action.", 60),
                Item("TECH", "Technical", "Technical change or configuration.", 70),
                Item("CONTENT", "Content", "Content or design-related change.", 80),
                Item("ACCESS", "Accessibility", "Accessibility remediation or improvement.", 90)
            },
            ["action-categories"] = new[]
            {
                Item("DELIVERY", "Delivery", "Planning, scheduling, execution tasks.", 10),
                Item("DESIGN", "Design", "UX, interaction, service design work.", 20),
                Item("RESEARCH", "Research", "User research or insight gathering.", 30),
                Item("TECH", "Technology", "Build, integration, infrastructure.", 40),
                Item("DATA", "Data & Analytics", "Data quality, reporting, analytics tasks.", 50),
                Item("ACCESS", "Accessibility", "Accessibility fixes, audits, statements.", 60),
                Item("ASSUR", "Assurance", "Reviews, assessments, checks.", 70)
            },
            ["action-impact-levels"] = new[]
            {
                Item("MINOR", "Minor", "Limited impact; small localised effect.", 10),
                Item("MODERATE", "Moderate", "Noticeable impact; manageable within team.", 20),
                Item("SIGNIFICANT", "Significant", "Major effect on delivery or service quality.", 30),
                Item("MAJOR", "Major", "Serious departmental impact; requires senior attention.", 40),
                Item("CRITICAL", "Critical", "Severe or systemic impact; unacceptable risk.", 50)
            },
            ["decision-statuses"] = new[]
            {
                Item("PROPOSED", "Proposed", "Logged but not yet discussed.", 10),
                Item("REVIEW", "Under review", "Being considered by the governance group.", 20),
                Item("APPROVED", "Approved", "Agreed and ready to implement.", 30),
                Item("REJECTED", "Rejected", "Not agreed; rationale recorded.", 40),
                Item("DEFERRED", "Deferred", "Decision postponed to a later point.", 50),
                Item("WITHDRAWN", "Withdrawn", "No longer required.", 60)
            },
            ["decision-priorities"] = new[]
            {
                Item("LOW", "Low", "Optional or low-impact decision.", 10),
                Item("MEDIUM", "Medium", "Important, but not time critical.", 20),
                Item("HIGH", "High", "Important and time-sensitive.", 30),
                Item("CRIT", "Critical", "Urgent, high-impact decision.", 40)
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
                Item("NOTST", "Not started", "Implementation not yet underway.", 10),
                Item("INPROG", "In progress", "Implementation in progress.", 20),
                Item("BLOCK", "Blocked", "Implementation blocked.", 30),
                Item("PENDV", "Pending verification", "Awaiting verification that decision is enacted.", 40),
                Item("DONE", "Completed", "Decision fully implemented.", 50)
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
            }
        };

    public static bool TryGetValues(string lookupKey, out IReadOnlyList<RaidLookupSeedItem> values) =>
        _definitions.TryGetValue(lookupKey, out values);

    public static IReadOnlyDictionary<string, IReadOnlyList<RaidLookupSeedItem>> Definitions => _definitions;

    private static RaidLookupSeedItem Item(string code, string label, string? description, int sortOrder) =>
        new(code, label, description, sortOrder);
}
