namespace Compass.Models;

public class ProjectContactDto
{
    public string Role { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? RoleDescription { get; set; }
    public int SortOrder { get; set; } = 1;
}
