using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Fips;

public class FipsCmdbProductSyncService : IFipsCmdbProductSyncService
{
    private const int MaxErrorSamples = 20;

    private static readonly JsonSerializerOptions CmdbSnapshotJsonOptions = new()
    {
        WriteIndented = false
    };

    private readonly CompassDbContext _db;
    private readonly ICmdbService _cmdb;
    private readonly FipsNewEntryOwnerEmailService _newEntryEmails;
    private readonly ILogger<FipsCmdbProductSyncService> _logger;

    public FipsCmdbProductSyncService(
        CompassDbContext db,
        ICmdbService cmdb,
        FipsNewEntryOwnerEmailService newEntryEmails,
        ILogger<FipsCmdbProductSyncService> logger)
    {
        _db = db;
        _cmdb = cmdb;
        _newEntryEmails = newEntryEmails;
        _logger = logger;
    }

    public async Task<FipsCmdbProductSyncResult> SyncActiveServiceOfferingsAsync(
        string triggeredByEmail,
        CancellationToken cancellationToken = default,
        Func<FipsCmdbSyncProgressUpdate, ValueTask>? reportProgress = null)
    {
        var result = new FipsCmdbProductSyncResult();
        var email = string.IsNullOrWhiteSpace(triggeredByEmail) ? "system" : triggeredByEmail.Trim();
        var now = DateTime.UtcNow;

        async ValueTask Report(string phase, string? message = null, int? processed = null, int? total = null)
        {
            if (reportProgress == null) return;
            await reportProgress(new FipsCmdbSyncProgressUpdate
            {
                Phase = phase,
                Message = message,
                Processed = processed,
                Total = total
            });
        }

        if (await AnotherBulkSyncIsRunningAsync(cancellationToken))
        {
            result.AlreadyRunning = true;
            return result;
        }

        var history = new FipsSyncHistory
        {
            SyncType = FipsCmdbCompassSyncHistory.SyncType,
            SourceEnvironment = FipsCmdbCompassSyncHistory.SourceEnvironment,
            TargetEnvironment = FipsCmdbCompassSyncHistory.TargetEnvironment,
            Status = FipsCmdbCompassSyncHistory.StatusRunning,
            StartedAt = now,
            InitiatedBy = email.Length > 255 ? email[..255] : email
        };
        _db.FipsSyncHistories.Add(history);
        await _db.SaveChangesAsync(cancellationToken);
        var historyId = history.Id;
        result.HistoryId = historyId;

        try
        {
            await Report(FipsCmdbSyncProgressUpdate.PhasePreparing, "Preparing sync (rules and product index)…");

            var rules = await LoadActiveRulesAsync(cancellationToken);
            var roleByName = await LoadRoleMapAsync(cancellationToken);

            await Report(FipsCmdbSyncProgressUpdate.PhaseLoadingCmdb, "Fetching active service offerings from CMDB…");
            var entries = await _cmdb.GetAllCmdbEntriesAsync();

            var existingList = await _db.CMDBProducts
                .AsNoTracking()
                .Where(p => p.CMDBID != null && p.CMDBID != "")
                .Select(p => new { p.CMDBID, p.Id, p.Status })
                .ToListAsync(cancellationToken);

            var retiredByCmdbId = existingList
                .Where(p => p.Status == CMDBProductStatus.Inactive)
                .Select(p => p.CMDBID!.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var idByCmdbId = existingList
                .GroupBy(p => p.CMDBID!.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

            var total = entries.Count;
            await Report(
                FipsCmdbSyncProgressUpdate.PhaseProcessing,
                total == 0
                    ? "No CMDB entries returned. Check FipsSync:Cmdb settings and that ServiceNow returns active service offerings."
                    : $"{total} CMDB entries loaded. Creating or updating Compass products…",
                0,
                total);

            const int progressEvery = 25;
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                string? sysKey = null;
                if (string.IsNullOrWhiteSpace(entry.SysId))
                    result.SkippedNoSysId++;
                else
                {
                    sysKey = entry.SysId.Trim();
                    if (retiredByCmdbId.Contains(sysKey))
                        result.SkippedRetired++;
                    else
                    {
                        try
                        {
                            var users = await _cmdb.GetServiceOfferingUsersAsync(entry);
                            var isNew = !idByCmdbId.TryGetValue(sysKey, out var existingId);
                            CMDBProduct product;
                            if (isNew)
                            {
                                product = new CMDBProduct
                                {
                                    CMDBID = sysKey,
                                    Status = CMDBProductStatus.New,
                                    CreatedAt = now,
                                    CreatedBy = email,
                                    UpdatedAt = now,
                                    UpdatedBy = email
                                };
                                _db.CMDBProducts.Add(product);
                            }
                            else
                            {
                                product = await _db.CMDBProducts
                                    .Include(p => p.Contacts)
                                    .FirstAsync(p => p.Id == existingId, cancellationToken);
                            }

                            var applied = await ApplyCmdbEntryToTrackedProductAsync(
                                product, entry, users, rules, roleByName, email, now, cancellationToken);
                            if (isNew)
                            {
                                result.Created++;
                                idByCmdbId[sysKey] = product.Id;
                                result.CreatedProducts.Add(new FipsCmdbSyncedProduct
                                {
                                    Id = product.Id,
                                    Title = product.Title,
                                    Status = product.Status
                                });
                            }
                            else if (applied.Changed)
                            {
                                result.Updated++;
                            }
                            else
                            {
                                result.Unchanged++;
                            }

                            if (applied.StatusSetByRule)
                                result.StatusSetByRules++;
                            if (applied.Changed)
                            {
                                result.Changes.Add(new FipsCmdbProductChange
                                {
                                    Id = product.Id,
                                    SysId = sysKey,
                                    Title = product.Title,
                                    Kind = isNew ? "Created" : "Updated",
                                    Fields = applied.Fields
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            result.Errors++;
                            _logger.LogWarning(ex, "CMDB FIPS sync failed for sys_id {SysId}", sysKey);
                            if (result.ErrorSamples.Count < MaxErrorSamples)
                                result.ErrorSamples.Add($"{sysKey}: {ex.Message}");
                            _db.ChangeTracker.Clear();
                        }
                    }
                }

                if (reportProgress != null && (total <= 1 || (i + 1) % progressEvery == 0 || i == entries.Count - 1))
                    await Report(FipsCmdbSyncProgressUpdate.PhaseProcessing, null, i + 1, total);
            }

            result.NewStatusCount = await _db.CMDBProducts
                .CountAsync(p => p.Status == CMDBProductStatus.New, cancellationToken);

            await NotifyNewEntriesAsync(cancellationToken);
            await FinalizeHistoryAsync(historyId, FipsCmdbCompassSyncHistory.StatusCompleted, result, null, now, cancellationToken);
            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await NotifyNewEntriesAsync(CancellationToken.None);
            await FinalizeHistoryAsync(historyId, FipsCmdbCompassSyncHistory.StatusFailed, result, ex.ToString(), now, cancellationToken);
            throw;
        }
        catch (OperationCanceledException)
        {
            await FinalizeHistoryAsync(historyId, FipsCmdbCompassSyncHistory.StatusFailed, result, "The sync was cancelled.", now, CancellationToken.None);
            throw;
        }
    }

    public async Task<FipsCmdbBulkSyncRunInfo?> GetLastBulkRunAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.FipsSyncHistories.AsNoTracking()
            .Where(h => h.SyncType == FipsCmdbCompassSyncHistory.SyncType)
            .OrderByDescending(h => h.StartedAt)
            .ThenByDescending(h => h.Id)
            .Select(h => new
            {
                h.StartedAt,
                h.CompletedAt,
                h.Status,
                h.InitiatedBy,
                h.ProductsCreated,
                h.ProductsUpdated,
                h.ErrorsEncountered
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row == null)
            return null;

        return new FipsCmdbBulkSyncRunInfo
        {
            StartedAtUtc = row.StartedAt,
            CompletedAtUtc = row.CompletedAt,
            Status = row.Status,
            InitiatedBy = row.InitiatedBy,
            ProductsCreated = row.ProductsCreated,
            ProductsUpdated = row.ProductsUpdated,
            ErrorsEncountered = row.ErrorsEncountered
        };
    }

    private async Task<bool> AnotherBulkSyncIsRunningAsync(CancellationToken cancellationToken)
    {
        var staleBefore = DateTime.UtcNow.AddHours(-2);
        var running = await _db.FipsSyncHistories
            .Where(h => h.SyncType == FipsCmdbCompassSyncHistory.SyncType
                        && h.Status == FipsCmdbCompassSyncHistory.StatusRunning)
            .ToListAsync(cancellationToken);

        var stale = running.Where(h => h.StartedAt < staleBefore).ToList();
        if (stale.Count > 0)
        {
            foreach (var row in stale)
            {
                row.Status = FipsCmdbCompassSyncHistory.StatusFailed;
                row.CompletedAt = DateTime.UtcNow;
                row.DurationSeconds = Math.Max(0, (int)(row.CompletedAt.Value - row.StartedAt).TotalSeconds);
                row.ErrorDetails = "Marked failed because the run did not finish (stale).";
            }

            await _db.SaveChangesAsync(cancellationToken);
        }

        return running.Any(h => h.StartedAt >= staleBefore);
    }

    private async Task NotifyNewEntriesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _newEntryEmails.SendPendingAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not send new service register entry emails");
        }
    }

    private async Task FinalizeHistoryAsync(
        int historyId,
        string status,
        FipsCmdbProductSyncResult result,
        string? errorDetails,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        try
        {
            var history = await _db.FipsSyncHistories.FirstOrDefaultAsync(h => h.Id == historyId, cancellationToken);
            if (history == null)
                return;

            var end = DateTime.UtcNow;
            history.Status = status;
            history.CompletedAt = end;
            history.DurationSeconds = Math.Max(0, (int)(end - startedAt).TotalSeconds);
            history.ProductsCreated = result.Created;
            history.ProductsUpdated = result.Updated;
            history.ProductsSkipped = result.SkippedRetired + result.SkippedNoSysId;
            history.ErrorsEncountered = result.Errors;
            history.ErrorDetails = errorDetails;
            history.ActionsLog = JsonSerializer.Serialize(new
            {
                result.Created,
                result.Updated,
                result.SkippedRetired,
                result.SkippedNoSysId,
                result.StatusSetByRules,
                result.Errors,
                result.Unchanged,
                result.NewStatusCount,
                errorSamples = result.ErrorSamples,
                created = result.CreatedProducts.Select(p => new
                {
                    id = p.Id,
                    title = p.Title,
                    status = p.Status.ToString()
                }),
                changes = result.Changes.Select(change => new
                {
                    id = change.Id,
                    sysId = change.SysId,
                    title = change.Title,
                    kind = change.Kind,
                    fields = change.Fields.Select(field => new
                    {
                        field = field.Field,
                        from = field.From,
                        to = field.To
                    })
                })
            }, CmdbSnapshotJsonOptions);

            await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not update CMDB sync history {HistoryId}", historyId);
        }
    }

    public async Task<FipsCmdbProductResetResult> ResetAllProductsForCmdbResyncAsync(
        string triggeredByEmail,
        CancellationToken cancellationToken = default)
    {
        var email = string.IsNullOrWhiteSpace(triggeredByEmail) ? "system" : triggeredByEmail.Trim();
        var now = DateTime.UtcNow;
        var result = new FipsCmdbProductResetResult();

        result.SkippedInactive = await _db.CMDBProducts
            .CountAsync(p => p.Status == CMDBProductStatus.Inactive, cancellationToken);

        var eligible = _db.CMDBProducts.Where(p => p.Status != CMDBProductStatus.Inactive);

        await _db.CMDBProductBusinessAreas
            .Where(ba => eligible.Select(p => p.Id).Contains(ba.CMDBProductId))
            .ExecuteDeleteAsync(cancellationToken);
        await _db.CMDBProductChannels
            .Where(ch => eligible.Select(p => p.Id).Contains(ch.CMDBProductId))
            .ExecuteDeleteAsync(cancellationToken);
        await _db.CMDBProductTypes
            .Where(t => eligible.Select(p => p.Id).Contains(t.CMDBProductId))
            .ExecuteDeleteAsync(cancellationToken);

        result.ProductsReset = await eligible.ExecuteUpdateAsync(
            s => s
                .SetProperty(p => p.Status, CMDBProductStatus.New)
                .SetProperty(p => p.PhaseId, (int?)null)
                .SetProperty(p => p.ProductURL, (string?)null)
                .SetProperty(p => p.IsEnterpriseService, false)
                .SetProperty(p => p.UpdatedAt, now)
                .SetProperty(p => p.UpdatedBy, email),
            cancellationToken);

        _logger.LogInformation(
            "CMDB product reset for resync: {Reset} product(s) cleared, {Skipped} retired skipped, by {Email}",
            result.ProductsReset,
            result.SkippedInactive,
            email);

        return result;
    }

    public async Task<FipsCmdbSingleProductSyncResult> SyncSingleProductAsync(
        Guid compassProductId,
        string triggeredByEmail,
        CancellationToken cancellationToken = default)
    {
        var email = string.IsNullOrWhiteSpace(triggeredByEmail) ? "system" : triggeredByEmail.Trim();
        var now = DateTime.UtcNow;

        var product = await _db.CMDBProducts
            .Include(p => p.Contacts)
            .FirstOrDefaultAsync(p => p.Id == compassProductId, cancellationToken);

        if (product == null)
            return new FipsCmdbSingleProductSyncResult { Success = false, Message = "Product not found." };

        if (string.IsNullOrWhiteSpace(product.CMDBID))
            return new FipsCmdbSingleProductSyncResult { Success = false, Message = "This product has no CMDB ID to sync against." };

        if (product.Status == CMDBProductStatus.Inactive)
            return new FipsCmdbSingleProductSyncResult { Success = false, Message = "Retired (inactive) products are not updated from CMDB. Reactivate the product first." };

        try
        {
            var entry = await _cmdb.GetServiceOfferingBySysIdAsync(product.CMDBID.Trim());
            if (entry == null)
                return new FipsCmdbSingleProductSyncResult { Success = false, Message = "No CMDB service offering was returned for this CMDB ID." };

            var users = await _cmdb.GetServiceOfferingUsersAsync(entry);
            var rules = await LoadActiveRulesAsync(cancellationToken);
            var roleByName = await LoadRoleMapAsync(cancellationToken);

            var applied = await ApplyCmdbEntryToTrackedProductAsync(
                product, entry, users, rules, roleByName, email, now, cancellationToken);

            return new FipsCmdbSingleProductSyncResult
            {
                Success = true,
                Message = !applied.Changed
                    ? "Product already matches CMDB. No changes were saved."
                    : applied.StatusSetByRule
                        ? "Product updated from CMDB. Status was set by a sync rule."
                        : "Product updated from CMDB.",
                StatusSetByRule = applied.StatusSetByRule
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Single CMDB sync failed for product {ProductId}", compassProductId);
            _db.ChangeTracker.Clear();
            return new FipsCmdbSingleProductSyncResult { Success = false, Message = "Sync failed: " + ex.Message };
        }
    }

    private async Task<List<FipsCmdbSyncRule>> LoadActiveRulesAsync(CancellationToken cancellationToken) =>
        await _db.FipsCmdbSyncRules.AsNoTracking()
            .Where(r => r.IsActive)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Id)
            .ToListAsync(cancellationToken);

    private async Task<Dictionary<string, int>> LoadRoleMapAsync(CancellationToken cancellationToken)
    {
        var roleRows = await _db.FipsContactRoles
            .AsNoTracking()
            .Where(r => r.Active)
            .ToListAsync(cancellationToken);
        return roleRows.ToDictionary(r => r.Name, r => r.Id, StringComparer.OrdinalIgnoreCase);
    }

    private sealed class CmdbApplyOutcome
    {
        public bool StatusSetByRule { get; init; }
        public bool Changed { get; init; }
        public List<FipsCmdbFieldChange> Fields { get; init; } = [];
    }

    /// <summary>Applies CMDB row to an already-tracked product and saves when mapped fields differ.</summary>
    private async Task<CmdbApplyOutcome> ApplyCmdbEntryToTrackedProductAsync(
        CMDBProduct product,
        CmdbEntry entry,
        CmdbServiceUsers users,
        IReadOnlyList<FipsCmdbSyncRule> rules,
        IReadOnlyDictionary<string, int> roleByName,
        string updatedByEmail,
        DateTime updatedUtc,
        CancellationToken cancellationToken)
    {
        var isNew = _db.Entry(product).State == EntityState.Added;
        var title = string.IsNullOrWhiteSpace(entry.Name)
            ? "Untitled service offering"
            : entry.Name.Trim();
        if (title.Length > 300)
            title = title[..300];

        var description = string.IsNullOrWhiteSpace(entry.Description) ? null : entry.Description.Trim();
        var entryJson = string.IsNullOrEmpty(entry.RecordJson)
            ? JsonSerializer.Serialize(entry, CmdbSnapshotJsonOptions)
            : entry.RecordJson;

        var ruleStatus = FipsCmdbSyncRuleEvaluator.EvaluateFirstStatusMatch(
            rules, entry, entryJson, product, title, _logger);
        var status = ruleStatus ?? product.Status;
        var statusSet = ruleStatus.HasValue && ruleStatus.Value != product.Status;
        var enterprise = product.IsEnterpriseService
                         || FipsCmdbSyncRuleEvaluator.EvaluateSetsEnterpriseService(
                             rules, entry, entryJson, product, title, _logger);

        var intendedContacts = BuildIntendedContacts(roleByName, users);
        var roleIdToName = roleByName
            .GroupBy(pair => pair.Value)
            .ToDictionary(group => group.Key, group => group.First().Key);
        var beforeContacts = FormatContacts(product.Contacts.Select(contact => (
            roleIdToName.TryGetValue(contact.FipsContactRoleId, out var roleName) ? roleName : "Contact",
            contact.UserName,
            contact.UserEmail)));
        var afterContacts = FormatContacts(intendedContacts.Select(contact => (
            contact.Role,
            contact.User.Name,
            (string?)contact.User.Email)));
        var fields = BuildFieldChanges(
            isNew,
            product,
            title,
            description,
            status,
            enterprise,
            beforeContacts,
            afterContacts);

        if (!isNew && fields.Count == 0)
            return new CmdbApplyOutcome();

        product.Title = title;
        product.CMDBDescription = description;
        product.UpdatedAt = updatedUtc;
        product.UpdatedBy = updatedByEmail;
        product.Status = status;
        product.IsEnterpriseService = enterprise;
        product.LastCmdbSnapshotJson = entryJson;
        if (isNew && status != CMDBProductStatus.New)
            product.NewOwnerCompletionEmailSentAt = updatedUtc;

        if (!string.Equals(beforeContacts, afterContacts, StringComparison.Ordinal))
        {
            if (product.Contacts.Count > 0)
            {
                _db.CMDBProductContacts.RemoveRange(product.Contacts);
                product.Contacts.Clear();
            }

            foreach (var contact in intendedContacts)
                AddContact(product, roleByName, contact.Role, contact.User, contact.CanManage);
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new CmdbApplyOutcome
        {
            StatusSetByRule = statusSet,
            Changed = true,
            Fields = fields
        };
    }

    private static List<FipsCmdbFieldChange> BuildFieldChanges(
        bool isNew,
        CMDBProduct product,
        string title,
        string? description,
        CMDBProductStatus status,
        bool enterprise,
        string beforeContacts,
        string afterContacts)
    {
        var fields = new List<FipsCmdbFieldChange>();
        if (isNew)
        {
            fields.Add(new FipsCmdbFieldChange { Field = "Title", To = title });
            if (!string.IsNullOrWhiteSpace(description))
                fields.Add(new FipsCmdbFieldChange { Field = "CMDB description", To = description });
            fields.Add(new FipsCmdbFieldChange { Field = "Status", To = status.ToString() });
            if (enterprise)
                fields.Add(new FipsCmdbFieldChange { Field = "Enterprise service", To = "Yes" });
            if (!string.IsNullOrWhiteSpace(afterContacts))
                fields.Add(new FipsCmdbFieldChange { Field = "Contacts", To = afterContacts });
            return fields;
        }

        AddField(fields, "Title", product.Title, title);
        AddField(fields, "CMDB description", product.CMDBDescription, description);
        AddField(fields, "Status", product.Status.ToString(), status.ToString());
        AddField(fields, "Enterprise service", product.IsEnterpriseService ? "Yes" : "No", enterprise ? "Yes" : "No");
        AddField(fields, "Contacts", beforeContacts, afterContacts);
        return fields;
    }

    private static void AddField(List<FipsCmdbFieldChange> fields, string name, string? from, string? to)
    {
        var left = Normalize(from);
        var right = Normalize(to);
        if (string.Equals(left, right, StringComparison.Ordinal))
            return;

        fields.Add(new FipsCmdbFieldChange
        {
            Field = name,
            From = string.IsNullOrEmpty(left) ? null : left,
            To = string.IsNullOrEmpty(right) ? null : right
        });
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

    private static List<(string Role, CmdbUser User, bool CanManage)> BuildIntendedContacts(
        IReadOnlyDictionary<string, int> roleByName,
        CmdbServiceUsers users)
    {
        var contacts = new List<(string Role, CmdbUser User, bool CanManage)>();
        AddIntended(contacts, roleByName, "Service Owner", users.ServiceOwner, canManage: true);
        AddIntended(contacts, roleByName, "Product manager", users.ProductManager, canManage: false);
        AddIntended(contacts, roleByName, "Delivery Manager", users.DeliveryManager, canManage: false);
        AddIntended(contacts, roleByName, "Information Asset Owner", users.InformationAssetOwner, canManage: false);
        AddIntended(contacts, roleByName, "Senior Responsible Officer", users.SeniorResponsibleOwner, canManage: false);
        return contacts;
    }

    private static void AddIntended(
        List<(string Role, CmdbUser User, bool CanManage)> contacts,
        IReadOnlyDictionary<string, int> roleByName,
        string roleName,
        CmdbUser? user,
        bool canManage)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.Email) || !roleByName.ContainsKey(roleName))
            return;
        contacts.Add((roleName, user, canManage));
    }

    private static string FormatContacts(IEnumerable<(string Role, string? Name, string? Email)> contacts) =>
        string.Join("; ", contacts
            .Select(contact =>
            {
                var email = contact.Email?.Trim() ?? "";
                var name = contact.Name?.Trim();
                var who = string.IsNullOrWhiteSpace(name) ? email : $"{name} <{email}>";
                return $"{contact.Role}: {who}";
            })
            .Where(line => line.Length > 0)
            .OrderBy(line => line, StringComparer.OrdinalIgnoreCase));

    private static void AddContact(
        CMDBProduct product,
        IReadOnlyDictionary<string, int> roleByName,
        string roleName,
        CmdbUser? user,
        bool canManage)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
            return;
        if (!roleByName.TryGetValue(roleName, out var roleId))
            return;

        var mail = user.Email.Trim();
        if (mail.Length > 320)
            mail = mail[..320];

        string? uname = user.Name?.Trim();
        if (!string.IsNullOrEmpty(uname) && uname.Length > 200)
            uname = uname[..200];

        product.Contacts.Add(new CMDBProductContact
        {
            FipsContactRoleId = roleId,
            UserEmail = mail,
            UserName = string.IsNullOrEmpty(uname) ? null : uname,
            CanManage = canManage
        });
    }
}
