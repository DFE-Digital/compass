using Compass.Models;

namespace Compass.Services;

public interface IGraphService
{
    Task<List<StaffMember>> SearchStaffAsync(string searchTerm, int maxResults = 20);
    Task<StaffMember?> GetStaffByEmailAsync(string email);
}

public class StaffMember
{
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? Department { get; set; }
}

