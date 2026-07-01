using Compass.Data;
using Compass.Models;
using Compass.Models.Fips;
using Compass.Services;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services.Modern;

public sealed class WorkServiceRegisterLinkService : IWorkServiceRegisterLinkService
{
    private readonly CompassDbContext _db;
    private readonly IPermissionService _permissions;
    private readonly IProductsApiService _productsApi;

    public WorkServiceRegisterLinkService(
        CompassDbContext db,
        IPermissionService permissions,
        IProductsApiService productsApi)
    {
        _db = db;
        _permissions = permissions;
        _productsApi = productsApi;
    }

    public async Task<bool> CanLinkFromWorkItemAsync(int projectId, string userEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return false;
        if (await _permissions.IsOperationConsoleUserAsync(userEmail))
            return true;

        var emailLower = userEmail.Trim().ToLowerInvariant();
        var userId = await _db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == emailLower)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return await _db.Projects.AsNoTracking()
            .Where(p => p.Id == projectId && !p.IsDeleted)
            .AnyAsync(p =>
                (userId.HasValue && p.PrimaryContactUserId == userId.Value) ||
                (p.PrimaryContactUser != null && p.PrimaryContactUser.Email.ToLower() == emailLower),
                cancellationToken);
    }

    public async Task<bool> CanCreateServiceOfferingFromWorkItemAsync(
        int projectId,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return false;
        if (await _permissions.IsOperationConsoleUserAsync(userEmail))
            return true;

        var emailLower = userEmail.Trim().ToLowerInvariant();
        var userId = await _db.Users.AsNoTracking()
            .Where(u => u.Email.ToLower() == emailLower)
            .Select(u => (int?)u.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return await _db.Projects.AsNoTracking()
            .Where(p => p.Id == projectId && !p.IsDeleted)
            .AnyAsync(p =>
                (userId.HasValue && p.PrimaryContactUserId == userId.Value) ||
                (p.PrimaryContactUser != null && p.PrimaryContactUser.Email.ToLower() == emailLower) ||
                (userId.HasValue && p.ServiceOwners.Any(so => so.UserId == userId.Value)) ||
                p.ServiceOwners.Any(so =>
                    so.User != null && so.User.Email != null &&
                    so.User.Email.Trim().ToLower() == emailLower),
                cancellationToken);
    }

    public async Task<bool> CanLinkFromServiceRegisterProductAsync(
        Guid cmdbProductId,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userEmail))
            return false;
        if (await _permissions.IsOperationConsoleUserAsync(userEmail))
            return true;

        var emailLower = userEmail.Trim().ToLowerInvariant();
        return await _db.CMDBProductContacts.AsNoTracking()
            .AnyAsync(c =>
                c.CMDBProductId == cmdbProductId &&
                c.UserEmail != null &&
                c.UserEmail.Trim().ToLower() == emailLower,
                cancellationToken);
    }

    public Task<int> CountLinksForWorkItemAsync(int projectId, CancellationToken cancellationToken = default) =>
        _db.ProjectProducts.AsNoTracking()
            .Where(pp => pp.ProjectId == projectId)
            .CountAsync(cancellationToken);

    public async Task<int> CountLinksForServiceRegisterProductAsync(
        Guid cmdbProductId,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.CMDBProducts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == cmdbProductId, cancellationToken);
        if (product == null)
            return 0;

        var matchKeys = await BuildProductMatchKeysAsync(product, cancellationToken);
        return await CountProjectProductsForProductKeysAsync(matchKeys, cancellationToken);
    }

    public async Task<IReadOnlyList<WorkServiceRegisterLinkRow>> GetLinksForWorkItemAsync(
        int projectId,
        Func<Guid, string> productDetailUrl,
        CancellationToken cancellationToken = default)
    {
        var links = await _db.ProjectProducts.AsNoTracking()
            .Where(pp => pp.ProjectId == projectId)
            .OrderBy(pp => pp.ProductTitle)
            .ToListAsync(cancellationToken);

        if (links.Count == 0)
            return Array.Empty<WorkServiceRegisterLinkRow>();

        var fipsIds = links
            .Select(l => l.ProductFipsId?.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var docIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in links.Select(l => l.ProductDocumentId?.Trim()).Where(s => !string.IsNullOrEmpty(s)))
        {
            docIds.Add(raw!);
            if (Guid.TryParse(raw, out var g))
                docIds.Add(g.ToString("D"));
        }

        var productGuids = docIds
            .Select(d => Guid.TryParse(d, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .Distinct()
            .ToList();

        var products = await _db.CMDBProducts.AsNoTracking()
            .Include(p => p.Phase)
            .Include(p => p.Contacts).ThenInclude(c => c.FipsContactRole)
            .Where(p =>
                (p.CMDBID != null && fipsIds.Contains(p.CMDBID)) ||
                productGuids.Contains(p.Id))
            .ToListAsync(cancellationToken);

        var byFips = products
            .Where(p => !string.IsNullOrWhiteSpace(p.CMDBID))
            .GroupBy(p => p.CMDBID!.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var byDoc = products.ToDictionary(p => p.Id.ToString("D"), p => p, StringComparer.OrdinalIgnoreCase);

        var rows = new List<WorkServiceRegisterLinkRow>();
        foreach (var link in links)
        {
            CMDBProduct? product = null;
            if (!string.IsNullOrWhiteSpace(link.ProductFipsId) &&
                byFips.TryGetValue(link.ProductFipsId.Trim(), out var byF))
            {
                product = byF;
            }
            else if (byDoc.TryGetValue(link.ProductDocumentId.Trim(), out var byD))
            {
                product = byD;
            }
            else if (Guid.TryParse(link.ProductDocumentId.Trim(), out var docGuid)
                     && byDoc.TryGetValue(docGuid.ToString("D"), out var byDGuid))
            {
                product = byDGuid;
            }

            if (product == null)
            {
                rows.Add(new WorkServiceRegisterLinkRow
                {
                    ProjectProductId = link.Id,
                    ServiceRegisterProductId = Guid.Empty,
                    Title = link.ProductTitle,
                    LinkedAt = link.CreatedAt,
                    DetailUrl = "",
                });
                continue;
            }

            rows.Add(new WorkServiceRegisterLinkRow
            {
                ProjectProductId = link.Id,
                ServiceRegisterProductId = product.Id,
                Title = product.Title,
                RegisterUniqueId = product.UniqueID,
                ServiceOwner = ResolveServiceOwner(product),
                PhaseName = product.Phase?.Name,
                LinkedAt = link.CreatedAt,
                DetailUrl = productDetailUrl(product.Id),
            });
        }

        return rows;
    }

    public async Task<IReadOnlyList<ServiceRegisterWorkLinkRow>> GetLinksForServiceRegisterProductAsync(
        Guid cmdbProductId,
        Func<int, string> workDetailUrl,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.CMDBProducts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == cmdbProductId, cancellationToken);
        if (product == null)
            return Array.Empty<ServiceRegisterWorkLinkRow>();

        var matchKeys = await BuildProductMatchKeysAsync(product, cancellationToken);
        var links = await QueryProjectProductsForProductKeysAsync(matchKeys, cancellationToken);
        if (links.Count == 0)
            return Array.Empty<ServiceRegisterWorkLinkRow>();

        var projectIds = links.Select(l => l.ProjectId).Distinct().ToList();
        var projects = await _db.Projects.AsNoTracking()
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        return links
            .OrderBy(l => l.ProductTitle)
            .Select(l =>
            {
                projects.TryGetValue(l.ProjectId, out var project);
                var title = project?.Title ?? l.ProductTitle;
                return new ServiceRegisterWorkLinkRow
                {
                    ProjectProductId = l.Id,
                    WorkItemId = l.ProjectId,
                    Title = title ?? "",
                    WorkCode = "WI-" + l.ProjectId.ToString("D8"),
                    Status = project?.Status,
                    LinkedAt = l.CreatedAt,
                    DetailUrl = workDetailUrl(l.ProjectId),
                };
            })
            .ToList();
    }

    public async Task<(bool Success, string? Error)> LinkAsync(
        int projectId,
        Guid cmdbProductId,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await CanLinkFromWorkItemAsync(projectId, userEmail, cancellationToken)
            && !await CanLinkFromServiceRegisterProductAsync(cmdbProductId, userEmail, cancellationToken))
        {
            return (false, "You do not have permission to link these records.");
        }

        var projectExists = await _db.Projects.AsNoTracking()
            .AnyAsync(p => p.Id == projectId && !p.IsDeleted, cancellationToken);
        if (!projectExists)
            return (false, "Work item not found.");

        var product = await _db.CMDBProducts.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == cmdbProductId, cancellationToken);
        if (product == null)
            return (false, "Service register entry not found.");

        var matchKeys = await BuildProductMatchKeysAsync(product, cancellationToken);
        var existing = await QueryProjectProductsForProductKeysAsync(matchKeys, cancellationToken);
        if (existing.Any(pp => pp.ProjectId == projectId))
            return (false, "This service register entry is already linked to the work item.");

        var cmsDocId = await ResolveCmsDocumentIdAsync(product, cancellationToken);
        var fipsId = string.IsNullOrWhiteSpace(product.CMDBID)
            ? product.Id.ToString("D")
            : product.CMDBID.Trim();

        var now = DateTime.UtcNow;
        _db.ProjectProducts.Add(new ProjectProduct
        {
            ProjectId = projectId,
            ProductDocumentId = cmsDocId,
            ProductFipsId = fipsId,
            ProductTitle = product.Title,
            CreatedAt = now,
            UpdatedAt = now,
        });
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<(bool Success, string? Error)> UnlinkAsync(
        int projectProductId,
        string userEmail,
        CancellationToken cancellationToken = default)
    {
        var link = await _db.ProjectProducts
            .FirstOrDefaultAsync(pp => pp.Id == projectProductId, cancellationToken);
        if (link == null)
            return (false, "Link not found.");

        var product = await ResolveCmdbProductForLinkAsync(link, cancellationToken);
        var canUnlink = await CanLinkFromWorkItemAsync(link.ProjectId, userEmail, cancellationToken);
        if (!canUnlink && product != null)
            canUnlink = await CanLinkFromServiceRegisterProductAsync(product.Id, userEmail, cancellationToken);
        if (!canUnlink)
            return (false, "You do not have permission to remove this link.");

        _db.ProjectProducts.Remove(link);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    private sealed record ProductMatchKeys(
        string DocumentId,
        string? FipsId,
        HashSet<string> DocumentIdCandidates);

    private async Task<ProductMatchKeys> BuildProductMatchKeysAsync(
        CMDBProduct product,
        CancellationToken cancellationToken)
    {
        var cmsDocId = await ResolveCmsDocumentIdAsync(product, cancellationToken);
        var fipsId = string.IsNullOrWhiteSpace(product.CMDBID) ? null : product.CMDBID.Trim();
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            product.Id.ToString("D"),
            cmsDocId,
        };
        if (!string.IsNullOrWhiteSpace(fipsId))
            candidates.Add(fipsId);

        return new ProductMatchKeys(cmsDocId, fipsId, candidates);
    }

    private async Task<string> ResolveCmsDocumentIdAsync(CMDBProduct product, CancellationToken cancellationToken)
    {
        var fipsKey = product.CMDBID?.Trim();
        if (!string.IsNullOrEmpty(fipsKey))
        {
            try
            {
                var dto = await _productsApi.GetProductByFipsIdAsync(fipsKey);
                if (!string.IsNullOrWhiteSpace(dto?.DocumentId))
                    return dto.DocumentId.Trim();
            }
            catch
            {
                // CMS unavailable — fall back below
            }
        }

        return product.Id.ToString("D");
    }

    private Task<List<ProjectProduct>> QueryProjectProductsForProductKeysAsync(
        ProductMatchKeys keys,
        CancellationToken cancellationToken)
    {
        var docList = keys.DocumentIdCandidates.ToList();
        var fipsId = keys.FipsId;
        return _db.ProjectProducts
            .Where(pp =>
                docList.Contains(pp.ProductDocumentId) ||
                (fipsId != null && pp.ProductFipsId == fipsId))
            .ToListAsync(cancellationToken);
    }

    private Task<int> CountProjectProductsForProductKeysAsync(
        ProductMatchKeys keys,
        CancellationToken cancellationToken)
    {
        var docList = keys.DocumentIdCandidates.ToList();
        var fipsId = keys.FipsId;
        return _db.ProjectProducts.AsNoTracking()
            .Where(pp =>
                docList.Contains(pp.ProductDocumentId) ||
                (fipsId != null && pp.ProductFipsId == fipsId))
            .CountAsync(cancellationToken);
    }

    private async Task<CMDBProduct?> ResolveCmdbProductForLinkAsync(
        ProjectProduct link,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(link.ProductFipsId))
        {
            var byFips = await _db.CMDBProducts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.CMDBID == link.ProductFipsId, cancellationToken);
            if (byFips != null)
                return byFips;
        }

        if (Guid.TryParse(link.ProductDocumentId, out var guid))
        {
            return await _db.CMDBProducts.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == guid, cancellationToken);
        }

        return null;
    }

    private static string? ResolveServiceOwner(CMDBProduct product) =>
        product.Contacts
            .Where(c => c.FipsContactRole != null &&
                        string.Equals(c.FipsContactRole.Name, "Service Owner", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.UserName ?? c.UserEmail)
            .FirstOrDefault(so => !string.IsNullOrWhiteSpace(so));
}
