using Compass.Attributes;
using Compass.Data;
using Compass.Models;
using Compass.ViewModels.Modern;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Compass.Controllers.Modern;

/// <summary>Compass reminder defaults + notification email audit (<c>/modern/admin/notification-settings*</c>).</summary>
[Authorize]
[RequireAdmin]
[Route("modern/admin")]
public sealed class ModernAdminNotificationSettingsController : Controller
{
    private readonly CompassDbContext _db;
    private readonly ILogger<ModernAdminNotificationSettingsController> _logger;

    private static readonly NotificationMeta[] Metadata =
    [
        new(
            Category: "Risk and issues",
            Key: CompassNotificationEventKeys.RiskIssueCreated,
            Label: "Created",
            Help: "When a new risk or issue is raised in RAID.",
            Fips: true, Primary: true, Pmo: false, Creator: false, Ops: true, Owner: true),
        new(
            Category: "Risk and issues",
            Key: CompassNotificationEventKeys.RiskIssueEscalated,
            Label: "Escalated",
            Help: "When escalation tier or status moves to a higher tier.",
            Fips: true, Primary: true, Pmo: false, Creator: false, Ops: true, Owner: true),
        new(
            Category: "Risk and issues",
            Key: CompassNotificationEventKeys.RiskIssueDeescalated,
            Label: "De-escalated",
            Help: "When escalation tier or status moves to a lower tier.",
            Fips: true, Primary: true, Pmo: false, Creator: false, Ops: true, Owner: true),
        new(
            Category: "Risk and issues",
            Key: CompassNotificationEventKeys.RiskIssueClosed,
            Label: "Closed",
            Help: "When a risk or issue is closed.",
            Fips: true, Primary: true, Pmo: false, Creator: false, Ops: true, Owner: true),
        new(
            Category: "Work items",
            Key: CompassNotificationEventKeys.WorkItemCreated,
            Label: "Work item created",
            Help: "When a new work item is created in COMPASS.",
            Fips: false, Primary: false, Pmo: false, Creator: true, Ops: true, Owner: false),
        new(
            Category: "Work reporting",
            Key: CompassNotificationEventKeys.WorkReportingMonthlyOpen,
            Label: "Monthly reporting open",
            Help: "When a monthly work reporting period is open for submission.",
            Fips: false, Primary: true, Pmo: true, Creator: false, Ops: false, Owner: false),
        new(
            Category: "Work reporting",
            Key: CompassNotificationEventKeys.WorkReportingMonthlyDueReminder,
            Label: "Monthly return due reminder",
            Help: "One day before the due date when a monthly return has not been submitted.",
            Fips: false, Primary: true, Pmo: true, Creator: false, Ops: false, Owner: false),
        new(
            Category: "Work reporting",
            Key: CompassNotificationEventKeys.WorkReportingMonthlyOverdue,
            Label: "Monthly return overdue",
            Help: "The day after the due date when a monthly return has not been submitted.",
            Fips: false, Primary: true, Pmo: true, Creator: false, Ops: false, Owner: false),
    ];

    private sealed record NotificationMeta(
        string Category,
        string Key,
        string Label,
        string Help,
        bool Fips,
        bool Primary,
        bool Pmo,
        bool Creator,
        bool Ops,
        bool Owner);

    public ModernAdminNotificationSettingsController(
        CompassDbContext db,
        ILogger<ModernAdminNotificationSettingsController> logger)
    {
        _db = db;
        _logger = logger;
    }

    private void SetChrome(string subItem)
    {
        ViewBag.MainNavSection = "admin";
        ViewBag.SubNavItem = subItem;
    }

    [HttpGet("notification-settings")]
    public async Task<IActionResult> NotificationSettings(CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Notification settings";
        SetChrome("admin-notification-settings");
        await EnsureSettingsRowsAsync(cancellationToken);
        var rowsDb = await _db.CompassNotificationSettings.AsNoTracking()
            .ToDictionaryAsync(s => s.EventKey, cancellationToken);

        var model = new CompassNotificationSettingsPageViewModel();
        foreach (var m in Metadata)
        {
            rowsDb.TryGetValue(m.Key, out var dbRow);
            var flags = (CompassNotificationRecipientFlags)(dbRow?.RecipientFlags ?? 0);
            model.Rows.Add(new CompassNotificationSettingRowViewModel
            {
                Category = m.Category,
                EventKey = m.Key,
                Label = m.Label,
                Help = m.Help,
                ShowFipsServiceOwner = m.Fips,
                ShowPrimaryWorkContact = m.Primary,
                ShowPmoContact = m.Pmo,
                ShowWorkItemCreator = m.Creator,
                ShowCentralOps = m.Ops,
                ShowRiskIssueOwnerOrCreator = m.Owner,
                IsEnabled = dbRow?.IsEnabled ?? false,
                SendToFipsServiceOwner = flags.HasFlag(CompassNotificationRecipientFlags.FipsServiceOwner),
                SendToPrimaryWorkContact = flags.HasFlag(CompassNotificationRecipientFlags.PrimaryWorkContact),
                SendToPmoContact = flags.HasFlag(CompassNotificationRecipientFlags.PmoContact),
                SendToWorkItemCreator = flags.HasFlag(CompassNotificationRecipientFlags.WorkItemCreator),
                SendToCentralOps = flags.HasFlag(CompassNotificationRecipientFlags.CentralOps),
                SendToRiskIssueOwnerOrCreator = flags.HasFlag(CompassNotificationRecipientFlags.RiskIssueOwnerOrCreator),
            });
        }

        if (TempData["AdminMessage"] is string ok)
            ViewBag.AdminMessage = ok;
        if (TempData["AdminError"] is string err)
            ViewBag.AdminError = err;

        return View("~/Views/Modern/Admin/NotificationSettings.cshtml", model);
    }

    [HttpPost("notification-settings")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> NotificationSettings(
        [FromForm] CompassNotificationSettingsPageViewModel model,
        CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Notification settings";
        SetChrome("admin-notification-settings");
        await EnsureSettingsRowsAsync(cancellationToken);

        var postedByKey = (model.Rows ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r.EventKey))
            .ToDictionary(r => r.EventKey, StringComparer.Ordinal);

        if (postedByKey.Count != Metadata.Length)
        {
            _logger.LogWarning(
                "Notification settings POST bound {BoundCount} of {ExpectedCount} rows",
                postedByKey.Count,
                Metadata.Length);
            TempData["AdminError"] = "Could not read all notification settings from the form. Please try again.";
            return RedirectToAction(nameof(NotificationSettings));
        }

        var dbRows = await _db.CompassNotificationSettings.ToDictionaryAsync(s => s.EventKey, cancellationToken);
        var now = DateTime.UtcNow;
        var updated = 0;

        foreach (var meta in Metadata)
        {
            if (!postedByKey.TryGetValue(meta.Key, out var row))
                continue;

            if (!dbRows.TryGetValue(meta.Key, out var entity))
            {
                entity = new CompassNotificationSetting
                {
                    EventKey = meta.Key,
                    UpdatedAtUtc = now,
                };
                _db.CompassNotificationSettings.Add(entity);
                dbRows[meta.Key] = entity;
            }

            entity.IsEnabled = row.IsEnabled;
            entity.RecipientFlags = (int)BuildRecipientFlags(meta, row);
            entity.UpdatedAtUtc = now;
            updated++;
        }

        if (updated == 0)
        {
            TempData["AdminError"] = "No notification settings were saved. Please try again.";
            return RedirectToAction(nameof(NotificationSettings));
        }

        await _db.SaveChangesAsync(cancellationToken);
        TempData["AdminMessage"] = "Notification settings saved.";
        _logger.LogInformation("Compass notification settings updated by admin ({Count} rows)", updated);
        return RedirectToAction(nameof(NotificationSettings));
    }

    private static CompassNotificationRecipientFlags BuildRecipientFlags(
        NotificationMeta meta,
        CompassNotificationSettingRowViewModel row)
    {
        CompassNotificationRecipientFlags f = 0;
        if (meta.Fips && row.SendToFipsServiceOwner)
            f |= CompassNotificationRecipientFlags.FipsServiceOwner;
        if (meta.Primary && row.SendToPrimaryWorkContact)
            f |= CompassNotificationRecipientFlags.PrimaryWorkContact;
        if (meta.Pmo && row.SendToPmoContact)
            f |= CompassNotificationRecipientFlags.PmoContact;
        if (meta.Creator && row.SendToWorkItemCreator)
            f |= CompassNotificationRecipientFlags.WorkItemCreator;
        if (meta.Ops && row.SendToCentralOps)
            f |= CompassNotificationRecipientFlags.CentralOps;
        if (meta.Owner && row.SendToRiskIssueOwnerOrCreator)
            f |= CompassNotificationRecipientFlags.RiskIssueOwnerOrCreator;
        return f;
    }

    [HttpGet("notification-settings/email-log")]
    public async Task<IActionResult> NotificationEmailLog(int page = 1, CancellationToken cancellationToken = default)
    {
        ViewData["Title"] = "Notification email log";
        SetChrome("admin-notification-email-log");
        const int pageSize = 40;
        page = page < 1 ? 1 : page;

        var q = _db.CompassNotificationEmailLogs.AsNoTracking()
            .OrderByDescending(x => x.SentAtUtc);

        var total = await q.CountAsync(cancellationToken);
        var slice = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var rows = slice.Select(x => new CompassNotificationEmailLogRowViewModel
        {
            SentAtUtc = x.SentAtUtc,
            EventKey = x.EventKey,
            EventDisplayName = CompassNotificationEventKeys.GetAdminDisplayName(x.EventKey),
            RecipientEmail = x.RecipientEmail,
            RecipientName = x.RecipientName,
            Subject = x.Subject,
            SendSucceeded = x.SendSucceeded,
            ContextReference = x.ContextReference,
        }).ToList();

        var vm = new CompassNotificationEmailLogPageViewModel
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            Rows = rows,
        };

        return View("~/Views/Modern/Admin/NotificationEmailLog.cshtml", vm);
    }

    private async Task EnsureSettingsRowsAsync(CancellationToken cancellationToken)
    {
        var existing = await _db.CompassNotificationSettings.Select(s => s.EventKey).ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        foreach (var key in CompassNotificationEventKeys.All)
        {
            if (existing.Contains(key))
                continue;
            _db.CompassNotificationSettings.Add(new CompassNotificationSetting
            {
                EventKey = key,
                IsEnabled = false,
                RecipientFlags = 0,
                UpdatedAtUtc = now,
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }
}
