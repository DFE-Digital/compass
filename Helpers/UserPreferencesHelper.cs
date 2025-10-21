using Compass.Data;
using Compass.Models;
using Microsoft.EntityFrameworkCore;

namespace Compass.Helpers;

public static class UserPreferencesHelper
{
    public static async Task<List<string>> GetPreferredBusinessAreasAsync(CompassDbContext context, string? userEmail)
    {
        if (string.IsNullOrEmpty(userEmail))
        {
            return new List<string>();
        }

        // Case-insensitive email lookup
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
        if (user == null)
        {
            return new List<string>();
        }

        var preferences = await context.UserPreferences.FindAsync(user.Id);
        if (preferences == null || string.IsNullOrEmpty(preferences.PreferredBusinessAreas))
        {
            return new List<string>();
        }

        return preferences.PreferredBusinessAreas
            .Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(ba => ba.Trim())
            .Where(ba => !string.IsNullOrEmpty(ba))
            .ToList();
    }

    public static async Task SavePreferredBusinessAreasAsync(CompassDbContext context, string userEmail, List<string> businessAreas)
    {
        // Case-insensitive email lookup
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
        if (user == null)
        {
            return;
        }

        var preferences = await context.UserPreferences.FindAsync(user.Id);
        if (preferences == null)
        {
            preferences = new UserPreference
            {
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            };
            context.UserPreferences.Add(preferences);
        }

        preferences.PreferredBusinessAreas = businessAreas.Any() 
            ? string.Join(",", businessAreas) 
            : null;
        preferences.UpdatedAt = DateTime.UtcNow;

        await context.SaveChangesAsync();
    }
}

