using Compass.Data;
using Compass.Models;
using Compass.Models.Census;
using Compass.Models.Fips;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Census;

public static class ServiceCensusReportBuilder
{
    private const int WeekCount = 12;

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
        var areaFilter = string.IsNullOrWhiteSpace(businessArea) ? null : businessArea.Trim();
        var areaOptions = products
            .SelectMany(p => p.Areas.Count == 0 ? new List<string> { "No business area" } : p.Areas)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var scoped = Filter(products, areaFilter);
        var rows = scoped.Select(p => ToProduct(p, responseByProduct, taskCount)).ToList();
        var overallPercent = rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(r => r.Percent), MidpointRounding.AwayFromZero);
        var weeks = BuildWeeks(scoped, responseByProduct, taskCount);

        var thisWeek = weeks.LastOrDefault();
        var last4 = weeks.TakeLast(4).ToList();
        var prev4 = weeks.Count > 4 ? weeks.Take(weeks.Count - 4).TakeLast(4).ToList() : [];
        var avgSub = last4.Count == 0 ? 0 : (decimal)last4.Average(w => w.ProductsSubmitted);
        var prevAvg = prev4.Count == 0 ? 0 : (decimal)prev4.Average(w => w.ProductsSubmitted);
        var remaining = rows.Count(r => !r.Submitted);
        var weeksToFinish = avgSub <= 0
            ? "Not enough submissions yet to estimate"
            : remaining == 0
                ? "All products submitted"
                : $"{Math.Ceiling(remaining / avgSub):0} week{(Math.Ceiling(remaining / avgSub) == 1 ? "" : "s")} at the current 4-week rate";

        var trend = avgSub > prevAvg + 0.1m ? "Up on the previous 4 weeks"
            : avgSub < prevAvg - 0.1m ? "Down on the previous 4 weeks"
            : "Steady against the previous 4 weeks";

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
            TasksCompleteThisWeek = thisWeek?.TasksCompleted ?? 0,
            SubmittedThisWeek = thisWeek?.ProductsSubmitted ?? 0,
            AverageWeeklySubmissions = Math.Round(avgSub, 1),
            PreviousAverageWeeklySubmissions = Math.Round(prevAvg, 1),
            RunRateTrendLabel = trend,
            WeeksToFinishLabel = weeksToFinish,
            BusinessAreas = AreaRows(scoped, responseByProduct, taskCount),
            Sections = SectionRows(scoped, responseByProduct, groups),
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

    private static ServiceCensusReportProductRow ToProduct(
        ProductSnap product, Dictionary<Guid, ResponseSnap> responses, int taskCount)
    {
        responses.TryGetValue(product.Id, out var response);
        var complete = response?.Tasks.Count(t => t.Status == ServiceCensusStatuses.Complete) ?? 0;
        var submitted = string.Equals(response?.Status, ServiceCensusStatuses.Submitted, StringComparison.OrdinalIgnoreCase)
            || response?.SubmittedAt != null;
        var percent = taskCount <= 0 ? 0 : (int)Math.Round(100.0 * complete / taskCount, MidpointRounding.AwayFromZero);
        var censusStatus = submitted ? "Submitted"
            : complete >= taskCount && taskCount > 0 ? "Complete"
            : complete > 0 ? "In progress"
            : "Not started";
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
            Submitted = submitted
        };
    }

    private static List<ServiceCensusReportAreaRow> AreaRows(
        List<ProductSnap> products, Dictionary<Guid, ResponseSnap> responses, int taskCount)
    {
        var names = products
            .SelectMany(p => p.Areas.Count == 0 ? new List<string> { "No business area" } : p.Areas)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return names
            .Select(name =>
            {
                var inArea = Filter(products, name);
                var rows = inArea.Select(p => ToProduct(p, responses, taskCount)).ToList();
                return new ServiceCensusReportAreaRow
                {
                    Name = name,
                    ProductCount = rows.Count,
                    AveragePercent = rows.Count == 0 ? 0 : (int)Math.Round(rows.Average(r => r.Percent), MidpointRounding.AwayFromZero),
                    NotStarted = rows.Count(r => r.CensusStatus == "Not started"),
                    InProgress = rows.Count(r => r.CensusStatus == "In progress"),
                    QuestionsComplete = rows.Count(r => r.CensusStatus is "Complete" or "Submitted"),
                    Submitted = rows.Count(r => r.Submitted)
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
                var complete = group.Keys.Count(key =>
                    response?.Tasks.Any(t =>
                        string.Equals(t.Key, key, StringComparison.OrdinalIgnoreCase)
                        && t.Status == ServiceCensusStatuses.Complete) == true);
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

    private static List<ServiceCensusReportWeekPoint> BuildWeeks(
        List<ProductSnap> products, Dictionary<Guid, ResponseSnap> responses, int taskCount)
    {
        var today = DateTime.UtcNow.Date;
        var currentStart = WeekStart(today);
        var starts = Enumerable.Range(0, WeekCount)
            .Select(i => currentStart.AddDays(-7 * (WeekCount - 1 - i)))
            .ToList();
        var productIds = products.Select(p => p.Id).ToHashSet();
        var scoped = responses.Values.Where(r => productIds.Contains(r.ProductId)).ToList();
        var totalProducts = products.Count;
        var cumulativeSubmitted = 0;
        var points = new List<ServiceCensusReportWeekPoint>();

        foreach (var start in starts)
        {
            var end = start.AddDays(7);
            var tasks = scoped.SelectMany(r => r.Tasks)
                .Count(t => t.Status == ServiceCensusStatuses.Complete
                    && t.UpdatedAt >= start && t.UpdatedAt < end);
            var submitted = scoped.Count(r =>
                r.SubmittedAt != null && r.SubmittedAt.Value >= start && r.SubmittedAt.Value < end);
            cumulativeSubmitted += submitted;

            var inProgress = scoped.Count(r =>
            {
                var submittedByEnd = r.SubmittedAt != null
                    ? r.SubmittedAt.Value < end
                    : string.Equals(r.Status, ServiceCensusStatuses.Submitted, StringComparison.OrdinalIgnoreCase);
                if (submittedByEnd)
                    return false;
                return r.Tasks.Any(t =>
                    t.Status == ServiceCensusStatuses.Complete && t.UpdatedAt < end);
            });

            var completeByEnd = scoped.Sum(r => r.Tasks.Count(t =>
                t.Status == ServiceCensusStatuses.Complete && t.UpdatedAt < end));
            var denom = totalProducts * Math.Max(taskCount, 1);
            var cumulativePercent = denom == 0
                ? 0
                : (int)Math.Round(100.0 * completeByEnd / denom, MidpointRounding.AwayFromZero);

            points.Add(new ServiceCensusReportWeekPoint
            {
                Label = start.ToString("d MMM"),
                IsoWeek = $"{ISOWeekYear(start)}-W{ISOWeek(start):00}",
                TasksCompleted = tasks,
                ProductsInProgress = inProgress,
                ProductsSubmitted = submitted,
                CumulativeSubmitted = cumulativeSubmitted,
                CumulativePercent = cumulativePercent
            });
        }

        return points;
    }

    private static DateTime WeekStart(DateTime date)
    {
        var d = date.Date;
        var offset = ((int)d.DayOfWeek + 6) % 7;
        return d.AddDays(-offset);
    }

    private static int ISOWeek(DateTime date) =>
        System.Globalization.ISOWeek.GetWeekOfYear(date);

    private static int ISOWeekYear(DateTime date) =>
        System.Globalization.ISOWeek.GetYear(date);

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
                    ? "Not started"
                    : state.CannotAnswer
                        ? "Cannot answer"
                        : string.Equals(state.Status, ServiceCensusStatuses.Complete, StringComparison.OrdinalIgnoreCase)
                            ? "Complete"
                            : "In progress";

                void AddAnswer(string? item, string? details, string? reference)
                {
                    answers.Add(new ServiceCensusReportAnswerRow
                    {
                        Product = product.Title,
                        Areas = product.Areas,
                        CensusStatus = product.CensusStatus,
                        SubmittedAt = response.SubmittedAt,
                        SubmittedBy = response.SubmittedBy,
                        Section = task.GroupName,
                        Question = task.NavLabel,
                        QuestionStatus = questionStatus,
                        CannotAnswer = state?.CannotAnswer == true,
                        CannotAnswerReason = state?.CannotAnswerReason,
                        Item = item,
                        Details = details,
                        Reference = reference
                    });
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

                if (task.Kind == CensusTaskKind.Contacts)
                    continue;

                if (task.Kind == CensusTaskKind.LongText
                    || (task.Key is "service" or "service-line" && state.Answers.Count == 0))
                {
                    var text = task.Key == "service" ? response.ServiceDescription
                        : task.Key == "service-line" ? response.ServiceLineDescription
                        : state.Answers.FirstOrDefault()?.Narrative;
                    if (string.IsNullOrWhiteSpace(text))
                        text = state.Answers.FirstOrDefault()?.Narrative;
                    AddAnswer(task.TitleLabel, text, null);
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
                    AddAnswer(
                        DisplayItem(task, answer.Title, answer.LookupCode, lookups),
                        answer.Narrative,
                        answer.ExternalUrl);
            }

            foreach (var review in response.ContactReviews)
            {
                var role = CensusDefinition.ContactRoles.FirstOrDefault(r =>
                    string.Equals(r.Key, review.RoleKey, StringComparison.OrdinalIgnoreCase));
                var added = role == null;
                contacts.Add(new ServiceCensusReportContactRow
                {
                    Product = product.Title,
                    Areas = product.Areas,
                    CensusStatus = product.CensusStatus,
                    Role = role?.Name ?? review.IncorrectReason ?? review.RoleKey,
                    Name = review.CmdbName,
                    Email = review.CmdbEmail,
                    Change = added ? "Added" : review.IsIncorrect ? "Changed" : "Confirmed"
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
                return (revealed.Legend ?? formField.Label) + ": " + (revealedOption?.Label ?? lookupCode ?? "");
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
                return field.Label + ": " + (option?.Label ?? lookupCode ?? "");
            }
            return field.Label;
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
