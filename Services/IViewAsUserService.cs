using Compass.Models;

namespace Compass.Services;

public interface IViewAsUserService
{
    Task<bool> CanEnableViewAsAsync(string realUserEmail);

    ViewAsUserSession? GetActive(HttpContext httpContext);

    bool IsActive(HttpContext httpContext);

    Task<ViewAsUserSession?> SetActiveAsync(HttpContext httpContext, int userId, string realUserEmail);

    void ClearActive(HttpContext httpContext);
}
