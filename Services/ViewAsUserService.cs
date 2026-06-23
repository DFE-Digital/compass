using System.Text.Json;
using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Services;

public sealed class ViewAsUserService : IViewAsUserService
{
    public const string SessionKey = "Compass.ViewAsUser";

    private readonly CompassDbContext _db;
    private readonly IPermissionService _permissions;

    public ViewAsUserService(CompassDbContext db, IPermissionService permissions)
    {
        _db = db;
        _permissions = permissions;
    }

    public Task<bool> CanEnableViewAsAsync(string realUserEmail) =>
        _permissions.IsCentralOperationsAdminOrSuperAdminAsync(realUserEmail);

    public ViewAsUserSession? GetActive(HttpContext httpContext)
    {
        var raw = httpContext.Session.GetString(SessionKey);
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        try
        {
            return JsonSerializer.Deserialize<ViewAsUserSession>(raw);
        }
        catch (JsonException)
        {
            httpContext.Session.Remove(SessionKey);
            return null;
        }
    }

    public bool IsActive(HttpContext httpContext) => GetActive(httpContext) != null;

    public async Task<ViewAsUserSession?> SetActiveAsync(HttpContext httpContext, int userId, string realUserEmail)
    {
        if (!await CanEnableViewAsAsync(realUserEmail))
            return null;

        if (userId <= 0)
            return null;

        var target = await _db.Users.AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.Id, u.Name, u.Email })
            .FirstOrDefaultAsync();

        if (target == null || string.IsNullOrWhiteSpace(target.Email))
            return null;

        var session = new ViewAsUserSession
        {
            UserId = target.Id,
            Name = string.IsNullOrWhiteSpace(target.Name) ? target.Email : target.Name.Trim(),
            Email = target.Email.Trim()
        };

        httpContext.Session.SetString(SessionKey, JsonSerializer.Serialize(session));
        return session;
    }

    public void ClearActive(HttpContext httpContext) =>
        httpContext.Session.Remove(SessionKey);
}
