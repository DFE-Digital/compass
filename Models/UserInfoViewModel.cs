namespace Compass.Models;

public class UserInfoViewModel : BaseViewModel
{
    public bool IsAuthenticated { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? ObjectId { get; set; }
    public string? TenantId { get; set; }
    public string? UserPrincipalName { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<KeyValuePair<string, string>> Claims { get; set; } = new();
}

