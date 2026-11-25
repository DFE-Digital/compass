using Compass.Data;
using Compass.Models;
using Compass.Models.Census;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Census;

public static class ServiceCensusReportBuilder
{
    public static async Task<ServiceCensusReportViewModel> LoadAsync(
        CompassDbContext db, string? businessArea, CancellationToken ct, bool includeSubmissions = false)
    {
        var taskCount = CensusDefinition.Tasks.Count;
        var groups = CensusDefinition.Groups()
            .Select(g => new SectionDef(g.Key, g.First().GroupName, g.Select(t => t.Key).ToList()))
            .ToList();

        var products = await db.CMDBProducts.AsNoTracking()
            .Where(p => p.Status == CMDBProductStatus.Active && !p.IsEnterpriseService)
            .Select(p => new ProductSnap
            {
                Id = p.Id,
                Title = p.Title,
                Status = p.Status.ToString(),
                Areas = p.BusinessAreas.Select(b => b.FipsBusinessArea.Name).ToList()
            })
            .ToListAsync(ct);

        var responses = await db.ServiceCensusResponses.AsNoTracking()
            .Select(r => new ResponseSnap
            {
                ProductId = r.ProductId,
                Status = r.Status,
                CreatedAt = r.CreatedAt,
                SubmittedAt = r.SubmittedAt,
                Tasks = r.Tasks.Select(t => new TaskSnap
                {
                    Key = t.TaskKey,
                    Status = t.Status,
                    UpdatedAt = t.UpdatedAt
                }).ToList()
            })
            .ToListAsync(ct);

        var responseByProduct = responses.ToDictionary(r => r.ProductId);
        var contactRoles = await db.FipsContactRoles.AsNoTracking()
            .Where(r => r.Active
                && r.Name != "Reporting contact"
                && r.Name != "Reporting user")
            .OrderBy(r => r.DisplayOrder)
            .ThenBy(r => r.Name)
            .Select(r => r.Name)
            .ToListAsync(ct);
        var productIds = products.Select(p => p.Id).ToList();
        var contactRows = await db.CMDBProductContacts.AsNoTracking()
            .Where(c => productIds.Contains(c.CMDBProductId))
            .Select(c => new { c.CMDBProductId, Role = c.FipsContactRole.Name, c.UserName })
            .ToListAsync(ct);
        var contactsByProduct = contactRows
            .GroupBy(c => c.CMDBProductId)
            .ToDictionary(
                g => g.Key,
                g => g.GroupBy(c => c.Role, StringComparer.OrdinalIgnoreCase)
                    .ToDictionary(
                        r => r.Key,
                        r => string.Join(", ", r
                            .Select(x => (x.UserName ?? "").Trim())
                            .Where(n => n.Length > 0)
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)),
                        StringComparer.OrdinalIgnoreCase));
        var areaFilter = string.IsNullOrWhiteSpace(businessArea) ? null : businessArea.Trim();
        var areaOptions = products
            .SelectMany(p => p.Areas.Count == 0 ? new List<string> { "No business area" } : p.Areas)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var scoped = Filter(products, areaFilter);
        var rows = scoped.Select(p => ToProduct(p, responseByProduct, taskCount, contactsByProduct)).ToList();
        var overallPercent = rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(r => r.Percent), MidpointRounding.AwayFromZero);
        var weeks = BuildDays(scoped, responseByProduct, taskCount);

        var todayUk = UkDate(DateTime.UtcNow);
        var elapsed = weeks.Where(d => ParseDay(d.IsoWeek) <= todayUk).ToList();
        var todayPoint = elapsed.LastOrDefault();
        var avgSub = elapsed.Count == 0 ? 0 : (decimal)elapsed.Average(d => d.ProductsSubmitted);
        var remaining = rows.Count(r => !r.Submitted);
        var daysToFinish = avgSub <= 0
            ? "Not enough submissions yet to estimate"
            : remaining == 0
                ? "All products submitted"
                : $"{Math.Ceiling(remaining / avgSub):0} day{(Math.Ceiling(remaining / avgSub) == 1 ? "" : "s")} at the current daily rate";

        var trend = "Daily run rate from 16 to 27 November 2026";

        var report = new ServiceCensusReportViewModel
        {
            BusinessAreaFilter = areaFilter,
            BusinessAreaOptions = areaOptions,
            ProductCount = rows.Count,
            TaskCount = taskCount,
            OverallPercent = overallPercent,
            NotStarted = rows.Count(r => r.CensusStatus == "Not started"),
            InProgress = rows.Count(r => r.CensusStatus == "In progress"),
            QuestionsComplete = rows.Count(r => r.CensusStatus is "Complete" or "Submitted"),
            Submitted = rows.Count(r => r.Submitted),
            TasksCompleteThisWeek = todayPoint?.TasksCompleted ?? 0,
            SubmittedThisWeek = todayPoint?.ProductsSubmitted ?? 0,
            AverageWeeklySubmissions = Math.Round(avgSub, 1),
            PreviousAverageWeeklySubmissions = 0,
            RunRateTrendLabel = trend,
            WeeksToFinishLabel = daysToFinish,
            BusinessAreas = AreaRows(scoped, responseByProduct, taskCount, contactsByProduct),
            ContactRoles = contactRoles,
            Sections = SectionRows(scoped, responseByProduct, groups),
            Questions = QuestionRows(rows, responseByProduct),
            Products = rows
                .OrderBy(r => r.Percent)
                .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            Weeks = weeks
        };

        if (includeSubmissions)
            await AddSubmissionRowsAsync(db, report, ct);

        return report;
    }

    private static List<ProductSnap> Filter(List<ProductSnap> products, string? area)
    {
        if (string.IsNullOrWhiteSpace(area)) return products;
        if (string.Equals(area, "No business area", StringComparison.OrdinalIgnoreCase))
            return products.Where(p => p.Areas.Count == 0).ToList();
        return products
            .Where(p => p.Areas.Any(a => string.Equals(a, area, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private static bool IsSubmitted(ResponseSnap? response) =>
        string.Equals(response?.Status, ServiceCensusStatuses.Submitted, StringComparison.OrdinalIgnoreCase)
        || response?.SubmittedAt != null;

    private static bool TaskIsComplete(ResponseSnap? response, string key, bool submitted)
    {
        if (submitted)
            return true;
        return response?.Tasks.Any(t =>
            string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase)
            && t.Status == ServiceCensusStatuses.Complete) == true;
    }

    private static int CompleteTaskCount(ResponseSnap? response, int taskCount)
    {
        if (taskCount <= 0)
            return 0;
        if (IsSubmitted(response))
            return taskCount;
        return CensusDefinition.Tasks.Count(t => TaskIsComplete(response, t.Key, false));
    }

    private static ServiceCensusReportProductRow ToProduct(
        ProductSnap product,
        Dictionary<Guid, ResponseSnap> responses,
        int taskCount,
        Dictionary<Guid, Dictionary<string, string>> contactsByProduct)
    {
        responses.TryGetValue(product.Id, out var response);
        var submitted = IsSubmitted(response);
        var complete = CompleteTaskCount(response, taskCount);
        var percent = taskCount <= 0 ? 0 : (int)Math.Round(100.0 * complete / taskCount, MidpointRounding.AwayFromZero);
        var censusStatus = submitted ? "Submitted"
            : complete >= taskCount && taskCount > 0 ? "Complete"
            : complete > 0 ? "In progress"
            : "Not started";
        contactsByProduct.TryGetValue(product.Id, out var roleContacts);
        return new ServiceCensusReportProductRow
        {
            Id = product.Id,
            Title = product.Title,
            Status = product.Status,
            Areas = product.Areas.Count == 0 ? "No business area" : string.Join(", ", product.Areas.OrderBy(a => a)),
            TasksComplete = complete,
            TaskCount = taskCount,
            Percent = percent,
            CensusStatus = censusStatus,
            Submitted = submitted,
            RoleContacts = roleContacts ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        };
    }

    private static List<ServiceCensusReportAreaRow> AreaRows(
        List<ProductSnap> products,
        Dictionary<Guid, ResponseSnap> responses,
        int taskCount,
        Dictionary<Guid, Dictionary<string, string>> contactsByProduct)
    {
        var names = products
            .SelectMany(p => p.Areas.Count == 0 ? new List<string> { "No business area" } : p.Areas)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return names
            .Select(name =>
            {
                var inArea = Filter(products, name);
                var rows = inArea.Select(p => ToProduct(p, responses, taskCount, contactsByProduct)).ToList();
                return new ServiceCensusReportAreaRow
                {
                    Name = name,
                    ProductCount = rows.Count,
                    AveragePercent = rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(r => r.Percent), MidpointRounding.AwayFromZero),
                    NotStarted = rows.Count(r => r.CensusStatus == "Not started"),
                    InProgress = rows.Count(r => r.CensusStatus == "In progress"),
                    QuestionsComplete = rows.Count(r => r.CensusStatus is "Complete" or "Submitted"),
                    Submitted = rows.Count(r => r.Submitted),
                    Products = rows
                        .OrderBy(r => r.Percent)
                        .ThenBy(r => r.Title, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                };
            })
            .OrderBy(r => r.AveragePercent)
            .ThenBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ServiceCensusReportSectionRow> SectionRows(
        List<ProductSnap> products,
        Dictionary<Guid, ResponseSnap> responses,
        List<SectionDef> groups)
    {
        return groups.Select(group =>
        {
            var percents = products.Select(p =>
            {
                responses.TryGetValue(p.Id, out var response);
                var submitted = IsSubmitted(response);
                var complete = group.Keys.Count(key => TaskIsComplete(response, key, submitted));
                return group.Keys.Count == 0 ? 0 : 100.0 * complete / group.Keys.Count;
            }).ToList();
            var completeProducts = percents.Count(p => p >= 100);
            return new ServiceCensusReportSectionRow
            {
                Name = group.Name,
                AveragePercent = percents.Count == 0 ? 0 : (int)Math.Round(percents.Average(), MidpointRounding.AwayFromZero),
                CompleteProducts = completeProducts,
                ProductCount = products.Count
            };
        }).ToList();
    }

    private static List<ServiceCensusReportQuestionRow> QuestionRows(
        List<ServiceCensusReportProductRow> products,
        Dictionary<Guid, ResponseSnap> responses)
    {
        return CensusDefinition.Tasks.Select(task =>
        {
            var complete = products.Count(p =>
            {
                responses.TryGetValue(p.Id, out var response);
                return TaskIsComplete(response, task.Key, p.Submitted);
            });
            return new ServiceCensusReportQuestionRow
            {
                Key = task.Key,
                Section = task.GroupName,
                Question = task.NavLabel,
                CompleteProducts = complete,
                ProductCount = products.Count,
                AveragePercent = products.Count == 0
                    ? 0
                    : (int)Math.Round(100.0 * complete / products.Count, MidpointRounding.AwayFromZero)
            };
        }).ToList();
    }

    private static readonly DateTime CampaignStart = new(2026, 11, 16);
    private static readonly DateTime CampaignEnd = new(2026, 11, 27);

    private static List<ServiceCensusReportWeekPoint> BuildDays(
        List<ProductSnap> products, Dictionary<Guid, ResponseSnap> responses, int taskCount)
    {
        var days = Enumerable.Range(0, (CampaignEnd - CampaignStart).Days + 1)
            .Select(i => CampaignStart.AddDays(i))
            .ToList();
        var productIds = products.Select(p => p.Id).ToHashSet();
        var scoped = responses.Values.Where(r => productIds.Contains(r.ProductId)).ToList();
        var totalProducts = products.Count;
        var cumulativeSubmitted = 0;
        var points = new List<ServiceCensusReportWeekPoint>();

        foreach (var day in days)
        {
            var tasks = scoped.SelectMany(r => r.Tasks)
                .Count(t => t.Status == ServiceCensusStatuses.Complete
                    && UkDate(t.UpdatedAt) == day);
            var started = scoped.Count(r => StartedOn(r) == day);
            var submitted = scoped.Count(r =>
                r.SubmittedAt != null && UkDate(r.SubmittedAt) == day);
            cumulativeSubmitted += submitted;

            var inProgress = scoped.Count(r =>
            {
                var startedBy = StartedOn(r);
                if (startedBy == DateTime.MinValue || startedBy > day)
                    return false;
                if (r.SubmittedAt == null)
                    return !string.Equals(r.Status, ServiceCensusStatuses.Submitted, StringComparison.OrdinalIgnoreCase);
                return UkDate(r.SubmittedAt) > day;
            });

            var completeByEnd = scoped.Sum(r => r.Tasks.Count(t =>
                t.Status == ServiceCensusStatuses.Complete && UkDate(t.UpdatedAt) <= day));
            var denom = totalProducts * Math.Max(taskCount, 1);
            var cumulativePercent = denom == 0
                ? 0
                : (int)Math.Round(100.0 * completeByEnd / denom, MidpointRounding.AwayFromZero);

            points.Add(new ServiceCensusReportWeekPoint
            {
                Label = day.ToString("d MMM"),
                IsoWeek = day.ToString("yyyy-MM-dd"),
                TasksCompleted = tasks,
                ProductsInProgress = inProgress,
                ProductsStarted = started,
                ProductsSubmitted = submitted,
                CumulativeSubmitted = cumulativeSubmitted,
                CumulativePercent = cumulativePercent
            });
        }

        return points;
    }

    private static DateTime StartedOn(ResponseSnap response)
    {
        return response.Tasks
            .Where(t => t.Status == ServiceCensusStatuses.Complete)
            .Select(t => UkDate(t.UpdatedAt))
            .Where(d => d > DateTime.MinValue)
            .DefaultIfEmpty(DateTime.MinValue)
            .Min();
    }

    private static DateTime ParseDay(string iso) =>
        DateTime.TryParse(iso, out var day) ? day.Date : DateTime.MinValue;

    private static readonly TimeZoneInfo UkZone = ResolveUkZone();

    private static TimeZoneInfo ResolveUkZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/London");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
        }
    }

    private static DateTime UkDate(DateTime? value)
    {
        if (value == null || value.Value == default)
            return DateTime.MinValue;
        var utc = value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(utc, UkZone).Date;
    }

    private static async Task AddSubmissionRowsAsync(
        CompassDbContext db, ServiceCensusReportViewModel report, CancellationToken ct)
    {
        var productIds = report.Products.Select(p => p.Id).ToList();
        if (productIds.Count == 0)
            return;

        var productById = report.Products.ToDictionary(p => p.Id);
        var lookups = await LoadLookupLabelsAsync(db, ct);
        var responses = await db.ServiceCensusResponses.AsNoTracking()
            .Include(r => r.Tasks).ThenInclude(t => t.Answers)
            .Include(r => r.ContactReviews)
            .Where(r => productIds.Contains(r.ProductId))
            .ToListAsync(ct);
        var cmdbContacts = await db.CMDBProductContacts.AsNoTracking()
            .Include(c => c.FipsContactRole)
            .Where(c => productIds.Contains(c.CMDBProductId))
            .Select(c => new { c.CMDBProductId, c.UserName, c.UserEmail, RoleName = c.FipsContactRole != null ? c.FipsContactRole.Name : null })
            .ToListAsync(ct);
        var cmdbByProduct = cmdbContacts
            .GroupBy(c => c.CMDBProductId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(c => (c.UserName, c.UserEmail, c.RoleName)).ToList());

        var answers = new List<ServiceCensusReportAnswerRow>();
        var contacts = new List<ServiceCensusReportContactRow>();

        foreach (var response in responses)
        {
            if (!productById.TryGetValue(response.ProductId, out var product))
                continue;
            var taskByKey = response.Tasks.ToDictionary(t => t.TaskKey, StringComparer.OrdinalIgnoreCase);

            foreach (var task in CensusDefinition.Tasks)
            {
                taskByKey.TryGetValue(task.Key, out var state);
                var questionStatus = state == null
                    ? (product.Submitted ? "Complete" : "Not started")
                    : state.CannotAnswer
                        ? "Cannot answer"
                        : string.Equals(state.Status, ServiceCensusStatuses.Complete, StringComparison.OrdinalIgnoreCase)
                            || product.Submitted
                            ? "Complete"
                            : "In progress";

                void AddAnswer(string? item, string? details, string? reference)
                {
                    answers.Add(new ServiceCensusReportAnswerRow
                    {
                        ProductId = product.Id,
                        Product = product.Title,
                        Areas = product.Areas,
                        CensusStatus = product.CensusStatus,
                        SubmittedAt = response.SubmittedAt,
                        SubmittedBy = response.SubmittedBy,
                        Section = task.GroupName,
                        QuestionKey = task.Key,
                        Question = task.NavLabel,
                        QuestionStatus = questionStatus,
                        CannotAnswer = state?.CannotAnswer == true,
                        CannotAnswerReason = state?.CannotAnswerReason,
                        Item = item,
                        Details = details,
                        Reference = reference
                    });
                }

                if (task.Kind == CensusTaskKind.Contacts)
                {
                    if (response.ContactReviews.Count == 0)
                    {
                        AddAnswer(null, null, null);
                        continue;
                    }

                    foreach (var review in response.ContactReviews)
                    {
                        var role = CensusDefinition.ContactRoles.FirstOrDefault(r =>
                            string.Equals(r.Key, review.RoleKey, StringComparison.OrdinalIgnoreCase));
                        var added = role == null;
                        AddAnswer(
                            role?.Name ?? review.IncorrectReason ?? review.RoleKey,
                            string.Join(" · ", new[] { review.CmdbName, review.CmdbEmail }.Where(v => !string.IsNullOrWhiteSpace(v))),
                            ContactChangeLabel(added, review.IsIncorrect, review.CmdbName, review.CmdbEmail));
                    }
                    continue;
                }

                if (state == null)
                {
                    AddAnswer(null, null, null);
                    continue;
                }

                if (state.CannotAnswer)
                {
                    AddAnswer(null, state.CannotAnswerReason, null);
                    continue;
                }

                if (task.Key == "service")
                {
                    var name = FirstAnswerNarrative(state, "name") ?? response.ServiceDescription;
                    AddAnswer(string.IsNullOrWhiteSpace(name) ? null : name.Trim(), null, null);
                    continue;
                }

                if (task.Key == "service-line")
                {
                    var name = FirstAnswerNarrative(state, "name") ?? response.ServiceLineDescription;
                    var description = FirstAnswerNarrative(state, "description");
                    AddAnswer(
                        string.IsNullOrWhiteSpace(name) ? null : name.Trim(),
                        string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
                        null);
                    continue;
                }

                if (task.Kind == CensusTaskKind.LongText)
                {
                    var text = state.Answers.FirstOrDefault()?.Narrative;
                    // Put the value in Item so analysis/export do not show a repeated field label.
                    AddAnswer(string.IsNullOrWhiteSpace(text) ? null : text.Trim(), null, null);
                    continue;
                }

                if (task.Kind == CensusTaskKind.Accessibility && !string.IsNullOrWhiteSpace(response.AccessibilityNotEnrolledReason)
                    && state.Answers.Count == 0)
                {
                    AddAnswer("Not enrolled", response.AccessibilityNotEnrolledReason, null);
                    continue;
                }

                if (state.Answers.Count == 0)
                {
                    AddAnswer(null, null, null);
                    continue;
                }

                foreach (var answer in state.Answers.OrderBy(a => a.SortOrder))
                {
                    var item = DisplayItem(task, answer.Title, answer.LookupCode, lookups);
                    var details = answer.Narrative;
                    // Promote free-text values into Item so answers are not shown as "label + same value".
                    if (string.IsNullOrWhiteSpace(item) && !string.IsNullOrWhiteSpace(details))
                    {
                        item = details.Trim();
                        details = null;
                    }
                    AddAnswer(item, details, answer.ExternalUrl);
                }
            }

            var productCmdb = cmdbByProduct.GetValueOrDefault(product.Id) ?? [];
            foreach (var review in response.ContactReviews)
            {
                var role = CensusDefinition.ContactRoles.FirstOrDefault(r =>
                    string.Equals(r.Key, review.RoleKey, StringComparison.OrdinalIgnoreCase));
                var added = role == null || review.RoleKey.StartsWith("cmdb-", StringComparison.OrdinalIgnoreCase);
                var original = role == null
                    ? null
                    : MatchCmdbContact(productCmdb, role);
                var roleLabel = role?.Name;
                if (string.IsNullOrWhiteSpace(roleLabel) && !string.IsNullOrWhiteSpace(review.IncorrectReason))
                    roleLabel = review.IncorrectReason;
                if (string.IsNullOrWhiteSpace(roleLabel))
                    roleLabel = added ? "Additional contact" : review.RoleKey;

                contacts.Add(new ServiceCensusReportContactRow
                {
                    ProductId = product.Id,
                    Product = product.Title,
                    Areas = product.Areas,
                    CensusStatus = product.CensusStatus,
                    RoleKey = review.RoleKey,
                    Role = roleLabel ?? "Additional contact",
                    Name = review.CmdbName,
                    Email = review.CmdbEmail,
                    OriginalName = original?.UserName,
                    OriginalEmail = original?.UserEmail,
                    IsAdded = added,
                    Change = ContactChangeLabel(added, review.IsIncorrect, review.CmdbName, review.CmdbEmail)
                });
            }
        }

        var answeredProductIds = responses.Select(r => r.ProductId).ToHashSet();
        foreach (var product in report.Products.Where(p => !answeredProductIds.Contains(p.Id)))
        {
            foreach (var task in CensusDefinition.Tasks)
            {
                answers.Add(new ServiceCensusReportAnswerRow
                {
                    ProductId = product.Id,
                    Product = product.Title,
                    Areas = product.Areas,
                    CensusStatus = product.CensusStatus,
                    Section = task.GroupName,
                    QuestionKey = task.Key,
                    Question = task.NavLabel,
                    QuestionStatus = product.Submitted ? "Complete" : "Not started"
                });
            }
        }

        report.SubmissionAnswers = answers
            .OrderBy(a => a.Product, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Section, StringComparer.OrdinalIgnoreCase)
            .ThenBy(a => a.Question, StringComparer.OrdinalIgnoreCase)
            .ToList();
        report.SubmissionContacts = contacts
            .OrderBy(c => c.Product, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Role, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static async Task<Dictionary<CensusLookupSource, Dictionary<string, string>>> LoadLookupLabelsAsync(
        CompassDbContext db, CancellationToken ct)
    {
        static Dictionary<string, string> Map(IEnumerable<(int Id, string Name)> rows) =>
            rows.GroupBy(x => x.Id.ToString())
                .ToDictionary(g => g.Key, g => g.First().Name, StringComparer.OrdinalIgnoreCase);

        var phases = await db.PhaseLookups.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var areas = await db.BusinessAreaLookups.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var dirs = await db.DirectorateLookups.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var channels = await db.FipsChannels.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var types = await db.FipsTypes.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var userGroups = await db.FipsUserGroups.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var missions = await db.Missions.AsNoTracking().Select(x => new { x.Id, x.Title }).ToListAsync(ct);
        var objectives = await db.Objectives.AsNoTracking().Select(x => new { x.Id, x.Title }).ToListAsync(ct);
        var tech = await db.CensusTechnologyLookups.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var integ = await db.CensusIntegrationLookups.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(ct);
        var appetite = await db.RiskAppetiteLookups.AsNoTracking().Select(x => new { x.Id, x.Name }).ToListAsync(ct);

        return new Dictionary<CensusLookupSource, Dictionary<string, string>>
        {
            [CensusLookupSource.Phase] = Map(phases.Select(x => (x.Id, x.Name))),
            [CensusLookupSource.BusinessArea] = Map(areas.Select(x => (x.Id, x.Name))),
            [CensusLookupSource.Directorate] = Map(dirs.Select(x => (x.Id, x.Name))),
            [CensusLookupSource.Channel] = Map(channels.Select(x => (x.Id, x.Name))),
            [CensusLookupSource.Type] = Map(types.Select(x => (x.Id, x.Name))),
            [CensusLookupSource.UserGroup] = Map(userGroups.Select(x => (x.Id, x.Name))),
            [CensusLookupSource.MissionPillar] = Map(missions.Select(x => (x.Id, x.Title))),
            [CensusLookupSource.StrategicObjective] = Map(objectives.Select(x => (x.Id, x.Title))),
            [CensusLookupSource.Technology] = Map(tech.Select(x => (x.Id, x.Name))),
            [CensusLookupSource.Integration] = Map(integ.Select(x => (x.Id, x.Name))),
            [CensusLookupSource.RiskAppetite] = Map(appetite.Select(x => (x.Id, x.Name)))
        };
    }

    private static string? DisplayItem(
        CensusTask task, string? title, string? lookupCode,
        Dictionary<CensusLookupSource, Dictionary<string, string>> lookups)
    {
        if (string.Equals(title, "stack-info", StringComparison.OrdinalIgnoreCase))
            return "Tech stack notes";

        foreach (var formField in task.FormFields)
        {
            var revealed = formField.ConditionalChoices.FirstOrDefault(c =>
                string.Equals(c.AnswerTitle, title, StringComparison.OrdinalIgnoreCase));
            if (revealed != null)
            {
                var revealedOption = revealed.Choices.FirstOrDefault(o =>
                    string.Equals(o.Value, lookupCode, StringComparison.OrdinalIgnoreCase));
                // Option label only — the question/legend is already shown as context.
                return revealedOption?.Label ?? lookupCode;
            }
        }

        var field = task.FormFields.FirstOrDefault(f =>
            string.Equals(f.Key, title, StringComparison.OrdinalIgnoreCase));
        if (field != null)
        {
            if (field.Kind == CensusFormFieldKind.Radio)
            {
                var option = field.Options.FirstOrDefault(o =>
                    string.Equals(o.Value, lookupCode, StringComparison.OrdinalIgnoreCase));
                return option?.Label ?? lookupCode;
            }

            // Text / long-text values live in Narrative; avoid repeating the field label as the answer.
            return null;
        }

        var choice = task.ChoiceOptions.FirstOrDefault(o =>
            string.Equals(o.Value, lookupCode, StringComparison.OrdinalIgnoreCase));
        if (choice != null)
            return choice.Label;

        if (!string.IsNullOrWhiteSpace(lookupCode)
            && lookups.TryGetValue(task.LookupSource, out var map)
            && map.TryGetValue(lookupCode, out var label))
            return label;

        if (!string.IsNullOrWhiteSpace(title) && lookups.TryGetValue(task.LookupSource, out var byTitle)
            && byTitle.TryGetValue(title, out var titled))
            return titled;

        return string.IsNullOrWhiteSpace(title) ? lookupCode : title;
    }

    private static string? FirstAnswerNarrative(ServiceCensusTaskState? state, string fieldKey)
    {
        if (state == null)
            return null;
        return state.Answers
            .Where(a => string.Equals(a.Title, fieldKey, StringComparison.OrdinalIgnoreCase))
            .Select(a => a.Narrative)
            .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }

    private static string ContactChangeLabel(bool added, bool isIncorrect, string? name, string? email)
    {
        if (added)
            return "Added";
        if (!isIncorrect)
            return "Confirmed";
        if (string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(email))
            return "Removed";
        return "Changed";
    }

    private static (string? UserName, string? UserEmail, string? RoleName)? MatchCmdbContact(
        List<(string? UserName, string? UserEmail, string? RoleName)> contacts,
        CensusContactRole role)
    {
        foreach (var contact in contacts)
        {
            var name = (contact.RoleName ?? "").Trim().ToLowerInvariant();
            if (role.NameAliases.Any(alias => name.Contains(alias, StringComparison.OrdinalIgnoreCase))
                || name.Contains(role.Name.ToLowerInvariant(), StringComparison.Ordinal))
                return contact;
        }

        return null;
    }

    private sealed record SectionDef(string Key, string Name, List<string> Keys);

    private sealed class ProductSnap
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Status { get; set; } = "";
        public List<string> Areas { get; set; } = [];
    }

    private sealed class ResponseSnap
    {
        public Guid ProductId { get; set; }
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public List<TaskSnap> Tasks { get; set; } = [];
    }

    private sealed class TaskSnap
    {
        public string Key { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime UpdatedAt { get; set; }
    }
}
