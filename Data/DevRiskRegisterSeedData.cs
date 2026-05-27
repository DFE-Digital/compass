using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Data;

/// <summary>
/// Seeds 50 realistic DDT-related risks and issues into a "Development" RAID register,
/// linking them to existing work items and service register entries. Directorate and
/// business area values are derived from the assigned work item.
/// Run: <c>dotnet run -- --seed-dev-risk-register [--environment Development]</c>
/// </summary>
public static class DevRiskRegisterSeedData
{
    private static readonly Random _rng = new(42);

    public static async Task<(int risksAdded, int issuesAdded)> ApplyAsync(CompassDbContext db)
    {
        var projects = await db.Projects
            .Include(p => p.Directorates).ThenInclude(pd => pd.Division)
            .Include(p => p.BusinessAreaLookup)
            .Where(p => !p.IsDeleted)
            .ToListAsync();

        var services = await db.Services.Where(s => s.IsActive).ToListAsync();

        if (projects.Count == 0)
        {
            Console.WriteLine("No work items found — cannot seed risks/issues without at least one work item.");
            return (0, 0);
        }

        // Load lookup IDs by code so we can assign them to risks/issues
        var riskStatuses = await db.RiskStatuses.Where(l => l.IsActive).ToListAsync();
        var riskPriorities = await db.RiskPriorities.Where(l => l.IsActive).ToListAsync();
        var riskLikelihoods = await db.RiskLikelihoods.Where(l => l.IsActive).ToListAsync();
        var riskImpactLevels = await db.RiskImpactLevels.Where(l => l.IsActive).ToListAsync();
        var riskProximities = await db.RiskProximities.Where(l => l.IsActive).ToListAsync();
        var riskCategories = await db.RiskCategories.Where(l => l.IsActive).ToListAsync();
        var riskTreatments = await db.RiskTreatments.Where(l => l.IsActive).ToListAsync();
        var governanceBoards = await db.GovernanceBoards.Where(l => l.IsActive).ToListAsync();
        var issueStatuses = await db.IssueStatuses.Where(l => l.IsActive).ToListAsync();
        var issuePriorities = await db.IssuePriorities.Where(l => l.IsActive).ToListAsync();
        var issueSeverities = await db.IssueSeverities.Where(l => l.IsActive).ToListAsync();
        var issueCategories = await db.IssueCategories.Where(l => l.IsActive).ToListAsync();

        if (riskStatuses.Count == 0 || riskLikelihoods.Count == 0 || riskImpactLevels.Count == 0)
        {
            Console.WriteLine("RAID lookups not yet seeded — run the RAID lookup seeder first.");
            return (0, 0);
        }

        // Find or create the "Development" RAID register
        var register = await db.RaidRegisters.FirstOrDefaultAsync(r => r.Name == "Development" && !r.IsDeleted);
        if (register == null)
        {
            var firstUser = await db.Users.FirstOrDefaultAsync();
            register = new RaidRegister
            {
                Name = "Development",
                Description = "Development risk register for DDT seeded risks and issues.",
                CreatedByUserId = firstUser?.Id ?? 1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.RaidRegisters.Add(register);
            await db.SaveChangesAsync();
            Console.WriteLine($"Created RAID register 'Development' (Id={register.Id}).");
        }

        // Scope the register to all projects and services
        var existingScopeProjectIds = await db.RaidRegisterWorkItems
            .Where(w => w.RaidRegisterId == register.Id)
            .Select(w => w.ProjectId)
            .ToHashSetAsync();

        foreach (var p in projects.Where(p => !existingScopeProjectIds.Contains(p.Id)))
        {
            db.RaidRegisterWorkItems.Add(new RaidRegisterWorkItem
            {
                RaidRegisterId = register.Id,
                ProjectId = p.Id
            });
        }

        var existingScopeServiceIds = await db.RaidRegisterServices
            .Where(s => s.RaidRegisterId == register.Id)
            .Select(s => s.FipsServiceId)
            .ToHashSetAsync();

        foreach (var s in services.Where(s => !existingScopeServiceIds.Contains(s.ServiceId)))
        {
            db.RaidRegisterServices.Add(new RaidRegisterService
            {
                RaidRegisterId = register.Id,
                FipsServiceId = s.ServiceId
            });
        }

        await db.SaveChangesAsync();

        var utcNow = DateTime.UtcNow;

        // Create 30 risks
        var risksAdded = 0;
        foreach (var def in RiskDefinitions)
        {
            var project = Pick(projects);
            var service = services.Count > 0 ? Pick(services) : null;
            var likelihood = Pick(riskLikelihoods);
            var impact = Pick(riskImpactLevels);
            var inherentScore = likelihood.MatrixScore * impact.MatrixScore;

            var risk = new Risk
            {
                Title = def.Title,
                Description = def.Description,
                ProjectId = project.Id,
                PrimaryProductId = service?.ServiceId,
                RaidAssociationKind = "WorkItem",
                Source = "Seed",

                // Lookup FKs
                RiskStatusId = Pick(riskStatuses).Id,
                RiskPriorityId = Pick(riskPriorities).Id,
                RiskLikelihoodId = likelihood.Id,
                RiskImpactLevelId = impact.Id,
                InherentScore = inherentScore,
                CurrentLikelihoodId = likelihood.Id,
                CurrentImpactLevelId = impact.Id,
                CurrentScore = inherentScore,
                RiskProximityId = riskProximities.Count > 0 ? Pick(riskProximities).Id : null,
                RiskCategoryId = riskCategories.Count > 0 ? Pick(riskCategories).Id : null,
                GovernanceBoardId = governanceBoards.Count > 0 ? Pick(governanceBoards).Id : null,

                // Legacy fields kept consistent
                ImpactRating = Math.Clamp(impact.MatrixScore, 1, 5),
                LikelihoodRating = Math.Clamp(likelihood.MatrixScore, 1, 5),
                RiskScore = inherentScore,
                Status = "open",
                Cause = def.Cause,
                ImpactIfRealised = def.Impact,
                ResponseStrategy = riskTreatments.Count > 0 ? Pick(riskTreatments).Label : null,

                IdentifiedDate = utcNow.AddDays(-_rng.Next(7, 180)),
                NextReviewDate = utcNow.AddDays(_rng.Next(14, 90)),
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

            db.Risks.Add(risk);
            await db.SaveChangesAsync();

            // Link to the register
            db.RaidRegisterRisks.Add(new RaidRegisterRisk
            {
                RaidRegisterId = register.Id,
                RiskId = risk.Id,
                AddedAt = utcNow
            });

            // Directorate (Division) from work item
            var projectDivision = project.Directorates.FirstOrDefault()?.Division;
            if (projectDivision != null)
            {
                db.RiskDivisions.Add(new RiskDivision
                {
                    RiskId = risk.Id,
                    DivisionId = projectDivision.Id
                });
            }

            // Business area from work item
            if (project.BusinessAreaId.HasValue)
            {
                db.RiskBusinessAreas.Add(new RiskBusinessArea
                {
                    RiskId = risk.Id,
                    BusinessAreaLookupId = project.BusinessAreaId.Value
                });
            }

            // Random category junction
            if (riskCategories.Count > 0)
            {
                db.RiskRiskCategories.Add(new RiskRiskCategory
                {
                    RiskId = risk.Id,
                    RiskCategoryId = Pick(riskCategories).Id
                });
            }

            risksAdded++;
        }

        await db.SaveChangesAsync();

        // Create 20 issues
        var issuesAdded = 0;
        foreach (var def in IssueDefinitions)
        {
            var project = Pick(projects);
            var service = services.Count > 0 ? Pick(services) : null;

            var issue = new Issue
            {
                Title = def.Title,
                Description = def.Description,
                ProjectId = project.Id,
                PrimaryProductId = service?.ServiceId,
                RaidAssociationKind = "WorkItem",
                Source = "Seed",

                StatusId = issueStatuses.Count > 0 ? Pick(issueStatuses).Id : null,
                PriorityId = issuePriorities.Count > 0 ? Pick(issuePriorities).Id : null,
                SeverityId = issueSeverities.Count > 0 ? Pick(issueSeverities).Id : null,
                IssueCategoryId = issueCategories.Count > 0 ? Pick(issueCategories).Id : null,

                Severity = "medium",
                Status = "open",
                DetectedDate = utcNow.AddDays(-_rng.Next(1, 120)),
                TargetResolutionDate = utcNow.AddDays(_rng.Next(14, 90)),
                DetailedCause = def.Cause,
                UserImpactSummary = def.Impact,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            };

            db.Issues.Add(issue);
            await db.SaveChangesAsync();

            db.RaidRegisterIssues.Add(new RaidRegisterIssue
            {
                RaidRegisterId = register.Id,
                IssueId = issue.Id,
                AddedAt = utcNow
            });

            var projectDivision = project.Directorates.FirstOrDefault()?.Division;
            if (projectDivision != null)
            {
                db.IssueDivisions.Add(new IssueDivision
                {
                    IssueId = issue.Id,
                    DivisionId = projectDivision.Id
                });
            }

            if (project.BusinessAreaId.HasValue)
            {
                db.IssueBusinessAreas.Add(new IssueBusinessArea
                {
                    IssueId = issue.Id,
                    BusinessAreaLookupId = project.BusinessAreaId.Value
                });
            }

            if (issueCategories.Count > 0)
            {
                db.IssueIssueCategories.Add(new IssueIssueCategory
                {
                    IssueId = issue.Id,
                    IssueCategoryId = Pick(issueCategories).Id
                });
            }

            issuesAdded++;
        }

        await db.SaveChangesAsync();
        return (risksAdded, issuesAdded);
    }

    private static T Pick<T>(IList<T> list) => list[_rng.Next(list.Count)];

    private record SeedDef(string Title, string Description, string? Cause, string? Impact);

    private static readonly SeedDef[] RiskDefinitions =
    {
        new("Legacy hosting contract expires before cloud migration completes",
            "The existing hosting contract for on-premise services ends in Q3 and the Azure migration timeline shows slippage.",
            "Dependency on infrastructure team capacity and delayed procurement of Azure landing zones.",
            "Service outage for multiple DDT products; emergency contract extension at premium cost."),

        new("Key delivery roles unfilled across multiple squads",
            "Several senior developer and delivery manager posts remain unfilled after two recruitment rounds.",
            "Civil service pay constraints and competition from private sector for cloud-native skills.",
            "Slower feature delivery, increased pressure on existing staff, knowledge concentration risk."),

        new("Third-party API deprecation threatens integration layer",
            "A critical upstream API used by three DDT products is being deprecated with a 6-month sunset notice.",
            "External supplier roadmap change outside DDT control.",
            "Loss of real-time data feed; manual workarounds required until replacement integration built."),

        new("WCAG 2.2 AA compliance gap on public-facing services",
            "Accessibility audit identified 12 high-severity WCAG 2.2 failures across two services nearing go-live.",
            "Insufficient accessibility testing earlier in the delivery lifecycle.",
            "Legal exposure under the Public Sector Bodies Accessibility Regulations; reputational damage."),

        new("Data migration quality issues for service consolidation",
            "Test migrations show 8% record-level data discrepancies between legacy and target schemas.",
            "Undocumented business rules in the legacy database and missing referential integrity.",
            "Incorrect citizen records in production; potential safeguarding and data protection breaches."),

        new("Shared platform dependency creates single point of failure",
            "Multiple DDT services depend on a single shared platform component with no failover.",
            "Architectural decision made under time pressure during initial build.",
            "Cascading outage across all dependent services during peak usage."),

        new("Cyber security pen-test findings not remediated before go-live",
            "IT Health Check identified 3 critical and 7 high findings; remediation timeline exceeds release date.",
            "Late scheduling of pen-test and complexity of findings in authentication layer.",
            "Service cannot proceed to live without SIRO sign-off; delayed benefits realisation."),

        new("Budget pressure may force scope reduction in current financial year",
            "Spending review outcome indicates a potential 15% budget reduction for DDT programmes.",
            "Cross-government fiscal tightening and competing departmental priorities.",
            "Features deferred; contractual commitments to suppliers may need renegotiation."),

        new("Ministerial priority shift redirects team capacity",
            "A new ministerial commitment requires rapid delivery of a policy tool, drawing staff from existing programmes.",
            "Political priority change outside DDT planning cycle.",
            "Existing programme milestones at risk; potential reputational impact if commitments missed."),

        new("GOV.UK Design System breaking changes in next release",
            "Upcoming major version of the GOV.UK Design System introduces breaking changes affecting 5 DDT services.",
            "Upstream design system evolution and accumulated tech debt in local component overrides.",
            "Regression defects in production; increased testing and remediation effort."),

        new("Cloud cost overrun due to unoptimised resource provisioning",
            "Azure consumption is tracking 40% above forecast due to over-provisioned compute in non-production environments.",
            "Lack of FinOps discipline and missing auto-scaling policies.",
            "Budget overspend; potential need to de-scope planned features to offset costs."),

        new("Supplier contract lock-in limits future architectural choices",
            "Current SaaS supplier contract contains exclusivity clauses that constrain adoption of alternative tools.",
            "Procurement terms agreed before DDT architecture strategy was formalised.",
            "Inability to adopt better-fit technology; ongoing cost premium."),

        new("Service Standard assessment readiness gap",
            "Internal pre-assessment identified significant gaps in research evidence and technical documentation.",
            "Team focus on delivery velocity at expense of documentation and evidence gathering.",
            "Failed or conditional assessment; blocked progression to next phase."),

        new("Knowledge concentration in single team member",
            "A critical service relies on undocumented knowledge held by one developer who is approaching contract end.",
            "Organic growth of service without pairing or documentation practices.",
            "Service becomes unsupportable; extended outages during incidents."),

        new("Incomplete disaster recovery testing for Tier 1 services",
            "DR runbooks exist but have not been exercised end-to-end for 18 months.",
            "Competing delivery priorities and lack of dedicated SRE capacity.",
            "Untested recovery procedures fail during a real incident; prolonged service outage."),

        new("Performance degradation under projected user load",
            "Load testing shows response times exceed SLA thresholds at 70% of projected peak concurrent users.",
            "Database query patterns not optimised for scale; missing caching layer.",
            "Poor user experience; potential service unavailability during high-demand periods."),

        new("Dependency on deprecated .NET framework version",
            "Two services still target .NET Framework 4.8 which is approaching end of support.",
            "Technical debt accumulated over successive delivery phases without upgrade investment.",
            "Security vulnerabilities without patches; inability to use modern libraries."),

        new("Cross-departmental data sharing agreement not in place",
            "A planned integration with another department requires a data sharing agreement that is still in legal review.",
            "Complex information governance requirements and competing legal team priorities.",
            "Feature launch delayed; temporary manual data exchange workarounds needed."),

        new("Inadequate monitoring and alerting for production services",
            "Several services lack structured logging, APM integration, or meaningful alerting thresholds.",
            "Monitoring treated as post-launch activity; no dedicated observability standards.",
            "Incidents detected by users before the team; extended mean-time-to-resolution."),

        new("GDS service assessment timeline clashes with ministerial deadline",
            "The mandatory GDS assessment slot is scheduled 2 weeks after the ministerial launch commitment.",
            "Limited assessment panel availability and fixed ministerial diary.",
            "Either launch without assessment (compliance risk) or miss ministerial deadline (reputational risk)."),

        new("Open source dependency with known CVE not yet patched",
            "Dependency scanning identified a critical CVE in a transitive npm package used across multiple frontends.",
            "Deep dependency chain makes upgrade complex; breaking changes in patched version.",
            "Exploitable vulnerability in production; potential data breach."),

        new("Test environment parity drift causing false confidence",
            "Staging environment configuration has diverged significantly from production over 6 months.",
            "Manual environment management and lack of infrastructure-as-code enforcement.",
            "Defects pass staging but fail in production; increased incident rate post-deployment."),

        new("Accessibility statement accuracy risk across service portfolio",
            "Several accessibility statements reference outdated audit results and do not reflect current known issues.",
            "No automated process to keep statements synchronised with issue trackers.",
            "Regulatory non-compliance; user complaints to the Equality and Human Rights Commission."),

        new("Multi-tenancy isolation gap in shared platform",
            "Security review identified potential for cross-tenant data leakage in the shared reporting module.",
            "Shared database schema without row-level security enforcement.",
            "Data breach affecting multiple service areas; ICO notification required."),

        new("Release pipeline fragility causing deployment failures",
            "CI/CD pipeline has a 30% failure rate on first attempt due to flaky integration tests and race conditions.",
            "Accumulated test debt and non-deterministic test fixtures.",
            "Slower release cadence; team morale impact; increased manual intervention."),

        new("Geopolitical supply chain risk for cloud region availability",
            "Single-region Azure deployment with no cross-region failover for UK South.",
            "Cost optimisation decision to avoid multi-region complexity.",
            "Extended outage if Azure UK South experiences a regional failure."),

        new("Insufficient capacity planning for seasonal demand spikes",
            "Historical usage data shows 3x traffic spikes during September that current auto-scaling cannot handle.",
            "Auto-scaling policies set using average rather than peak usage patterns.",
            "Service degradation during critical policy announcement periods."),

        new("Privacy impact assessment not completed for new data collection",
            "A new feature collecting additional personal data has been designed without a completed DPIA.",
            "Feature delivery prioritised over governance process; DPIA template not embedded in backlog.",
            "ICO enforcement action; need to disable feature post-launch until DPIA completed."),

        new("Architecture decision records missing for key technical choices",
            "Major architectural decisions (database selection, auth provider, messaging) have no formal ADRs.",
            "Fast-paced delivery without lightweight governance practices in place.",
            "Future teams cannot understand rationale; repeated re-evaluation of settled decisions."),

        new("Container image supply chain vulnerability",
            "Base container images are pulled from public registries without signature verification or scanning.",
            "No container image governance policy or private registry enforcement.",
            "Compromised base image introduced into production; supply chain attack vector.")
    };

    private static readonly SeedDef[] IssueDefinitions =
    {
        new("Production outage on citizen-facing service due to expired TLS certificate",
            "The TLS certificate for a public-facing service expired overnight causing a complete service outage for 4 hours.",
            "Certificate renewal process was manual with no automated monitoring or alerting.",
            "Citizens unable to access service; 1,200 support calls received; media coverage."),

        new("Deployment rollback required after database schema migration failure",
            "A schema migration ran successfully in staging but caused deadlocks in production due to table size difference.",
            "Migration not tested against production-scale data volumes.",
            "2-hour service degradation; emergency rollback and re-planning required."),

        new("Incorrect benefit calculations displayed to users for 3 days",
            "A logic error in the calculation engine caused incorrect figures to be shown to approximately 500 users.",
            "Edge case in date handling not covered by unit tests.",
            "Users received incorrect information; remediation communications and recalculations needed."),

        new("Third-party identity provider intermittent failures blocking sign-in",
            "Users are experiencing intermittent 503 errors when authenticating via the external identity provider.",
            "Supplier infrastructure issues; no SLA breach acknowledged by supplier yet.",
            "15% of sign-in attempts failing; user complaints escalating."),

        new("Data breach: internal user accessed records outside their authorisation scope",
            "Audit log review revealed an internal user accessed 200 records they were not authorised to view.",
            "Row-level security policy not enforced in the reporting API; authorisation checked only at UI level.",
            "ICO notification required within 72 hours; affected individuals to be contacted."),

        new("Accessibility regression introduced in latest release",
            "Screen reader users report that the main navigation is no longer keyboard-accessible after the last deployment.",
            "Component library update removed aria attributes; no automated accessibility regression tests.",
            "Equality Act compliance breach; urgent hotfix required."),

        new("CI/CD pipeline compromised by malicious dependency update",
            "Dependabot auto-merged a compromised package version that injected telemetry into build artifacts.",
            "Auto-merge enabled for patch updates without human review or signature verification.",
            "Build artifacts for 2 services potentially compromised; full audit and rebuild required."),

        new("Service performance SLA breached for 5 consecutive days",
            "Average page response times exceeded the 2-second SLA threshold due to unoptimised database queries.",
            "New reporting feature introduced N+1 query pattern not caught in code review.",
            "SLA breach triggers contractual review clause; user satisfaction scores declining."),

        new("Production secrets exposed in application logs",
            "Database connection strings and API keys were written to application logs visible in the shared logging platform.",
            "Structured logging configuration included the full request context without redaction rules.",
            "Secrets rotated; logging configuration patched; security incident review initiated."),

        new("Cross-site scripting vulnerability discovered in user input field",
            "Security testing identified a stored XSS vulnerability in the service feedback form.",
            "Input sanitisation library not applied to this specific form; missed in security review.",
            "Potential for session hijacking; emergency patch deployed within 24 hours."),

        new("Service unavailable in Wales due to CDN misconfiguration",
            "Users in Wales unable to access the service for 6 hours due to a CDN routing rule error.",
            "Manual CDN configuration change applied without peer review or rollback plan.",
            "Geographic service disparity; ministerial question raised."),

        new("Duplicate records created by race condition in concurrent submissions",
            "High-traffic period caused race condition creating 340 duplicate case records in the database.",
            "Missing idempotency key on the submission endpoint; no database-level uniqueness constraint.",
            "Data quality issue requiring manual deduplication; downstream systems affected."),

        new("Automated email notifications sending to wrong recipients",
            "A configuration error caused 150 notification emails to be sent to incorrect email addresses.",
            "Environment-specific config not applied during deployment; staging addresses leaked to production.",
            "Personal data sent to wrong recipients; data breach reported to DPO."),

        new("Mobile responsive layout broken on iOS Safari after framework update",
            "The primary service layout is unusable on iOS Safari following a CSS framework version bump.",
            "No cross-browser testing included in the release pipeline; Safari-specific flexbox issue.",
            "30% of mobile users affected; workaround is to use Chrome."),

        new("API rate limiting not applied, causing downstream service overload",
            "An unthrottled API endpoint allowed a single consumer to send 50k requests/minute, overloading the backend.",
            "Rate limiting was planned but not implemented before the API was promoted to production.",
            "Downstream database connection pool exhausted; 45-minute partial outage."),

        new("Incorrect GOV.UK Notify template causing confusing citizen communications",
            "A template variable was misconfigured causing letters to display raw placeholder text instead of personalised data.",
            "Template change deployed without end-to-end testing of the notification flow.",
            "800 letters sent with incorrect content; reissue and apology communications required."),

        new("Health check endpoint returning false positive masking real failures",
            "The /health endpoint returns 200 OK even when the database connection is down.",
            "Health check only validates the web server process, not downstream dependencies.",
            "Load balancer continues routing traffic to unhealthy instance; users see 500 errors."),

        new("PII data retained beyond statutory retention period",
            "Data retention audit found 45,000 records retained 2 years beyond the stated retention policy.",
            "Retention policy not implemented as automated job; manual deletion process not followed.",
            "GDPR Article 5(1)(e) breach; records must be purged and DPO notified."),

        new("Feature flag left enabled in production causing unintended behaviour",
            "A feature flag intended for A/B testing in staging was left on in production, showing unfinished UI to all users.",
            "No environment-scoped feature flag management; manual toggle process.",
            "Confusing user experience for 2 days; support ticket volume doubled."),

        new("DNS failover not triggering during primary provider outage",
            "When the primary DNS provider experienced issues, failover to secondary did not activate as expected.",
            "DNS failover configuration had an incorrect health check URL; never tested end-to-end.",
            "30-minute resolution delay; service appeared down despite healthy infrastructure.")
    };
}
