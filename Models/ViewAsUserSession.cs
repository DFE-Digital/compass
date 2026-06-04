namespace Compass.Models;

/// <summary>Session payload when a Central Operations Admin is viewing Compass as another user.</summary>
public sealed class ViewAsUserSession
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}
