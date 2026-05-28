using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public sealed class HttpErrorEmailSettingsService : IHttpErrorEmailSettingsService
{
    private readonly CompassDbContext _db;

    public HttpErrorEmailSettingsService(CompassDbContext db) => _db = db;

    public async Task<HttpErrorEmailSettings> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        var row = await _db.HttpErrorEmailSettings
            .FirstOrDefaultAsync(s => s.Id == HttpErrorEmailSettings.SingletonId, cancellationToken);
        if (row != null)
            return row;

        row = new HttpErrorEmailSettings { Id = HttpErrorEmailSettings.SingletonId };
        _db.HttpErrorEmailSettings.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }

    public async Task<HttpErrorEmailSettings> SaveAsync(
        bool isEnabled,
        string? contactEmail,
        string? updatedByEmail,
        CancellationToken cancellationToken = default)
    {
        var row = await GetOrCreateAsync(cancellationToken);
        row.IsEnabled = isEnabled;
        row.ContactEmail = string.IsNullOrWhiteSpace(contactEmail) ? null : contactEmail.Trim();
        row.UpdatedAtUtc = DateTime.UtcNow;
        row.UpdatedByEmail = string.IsNullOrWhiteSpace(updatedByEmail) ? null : updatedByEmail.Trim();
        await _db.SaveChangesAsync(cancellationToken);
        return row;
    }
}
