using Compass.Data;
using Compass.Models;
using Compass.Services.ServiceDataModels;
using Compass.ViewModels.Modern;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public interface ICapabilityAdminService
{
    Task<IReadOnlyList<CapabilityLookup>> ListAsync(CancellationToken cancellationToken = default);
    Task<CapabilityLookup?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error, Guid? Id)> CreateAsync(CapabilityEditViewModel input, string actorEmail, CancellationToken cancellationToken = default);
    Task<(bool Ok, string? Error)> UpdateAsync(Guid id, CapabilityEditViewModel input, string actorEmail, CancellationToken cancellationToken = default);
    Task<bool> SetDisabledAsync(Guid id, bool disabled, string actorEmail, CancellationToken cancellationToken = default);
}

public sealed class CapabilityAdminService : ICapabilityAdminService
{
    private readonly CompassDbContext _db;
    private readonly IServiceDataModelAccessService _access;

    public CapabilityAdminService(CompassDbContext db, IServiceDataModelAccessService access)
    {
        _db = db;
        _access = access;
    }

    public async Task<IReadOnlyList<CapabilityLookup>> ListAsync(CancellationToken cancellationToken = default) =>
        await _db.CapabilityLookups.AsNoTracking()
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Title)
            .ToListAsync(cancellationToken);

    public async Task<CapabilityLookup?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _db.CapabilityLookups.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<(bool Ok, string? Error, Guid? Id)> CreateAsync(
        CapabilityEditViewModel input,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return (false, "You do not have permission to manage capabilities.", null);

        var title = (input.Title ?? "").Trim();
        var reference = (input.Reference ?? "").Trim();
        if (string.IsNullOrEmpty(title))
            return (false, "Enter a title.", null);
        if (string.IsNullOrEmpty(reference))
            return (false, "Enter a reference.", null);

        if (await _db.CapabilityLookups.AnyAsync(
                x => x.Reference == reference, cancellationToken))
            return (false, $"Reference \"{reference}\" is already used by another capability.", null);

        var entity = new CapabilityLookup
        {
            Id = Guid.NewGuid(),
            Title = title,
            Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim(),
            Reference = reference,
            SortOrder = input.SortOrder,
            IsActive = true,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
        _db.CapabilityLookups.Add(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null, entity.Id);
    }

    public async Task<(bool Ok, string? Error)> UpdateAsync(
        Guid id,
        CapabilityEditViewModel input,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return (false, "You do not have permission to manage capabilities.");

        var entity = await _db.CapabilityLookups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return (false, "Capability not found.");

        var title = (input.Title ?? "").Trim();
        var reference = (input.Reference ?? "").Trim();
        if (string.IsNullOrEmpty(title))
            return (false, "Enter a title.");
        if (string.IsNullOrEmpty(reference))
            return (false, "Enter a reference.");

        if (await _db.CapabilityLookups.AnyAsync(
                x => x.Reference == reference && x.Id != id, cancellationToken))
            return (false, $"Reference \"{reference}\" is already used by another capability.");

        entity.Title = title;
        entity.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();
        entity.Reference = reference;
        entity.SortOrder = input.SortOrder;
        entity.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return (true, null);
    }

    public async Task<bool> SetDisabledAsync(
        Guid id,
        bool disabled,
        string actorEmail,
        CancellationToken cancellationToken = default)
    {
        if (!await _access.CanManageModelsAsync(actorEmail))
            return false;

        var entity = await _db.CapabilityLookups.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity == null)
            return false;

        entity.IsActive = !disabled;
        entity.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
