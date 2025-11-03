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
    public List<GroupInfo> Groups { get; set; } = new();
    public List<ClaimInfo> Claims { get; set; } = new();
    public User? DatabaseUser { get; set; }
}

public class GroupInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemGroup { get; set; }
    public DateTime AssignedAt { get; set; }
}

public class ClaimInfo
{
    public string Type { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

