using System.Text;
using Microsoft.Graph.Models;

namespace Compass.Services;

public static class GraphUserNameFormatter
{
    public static string FormatFriendlyName(User? graphUser, string? fallbackEmail = null)
    {
        if (graphUser == null)
            return fallbackEmail?.Trim() ?? string.Empty;

        return FormatFriendlyName(
            graphUser.GivenName,
            graphUser.Surname,
            graphUser.DisplayName,
            fallbackEmail ?? graphUser.Mail ?? graphUser.UserPrincipalName);
    }

    public static string FormatFriendlyName(
        string? givenName,
        string? surname,
        string? displayName,
        string? fallbackEmail = null)
    {
        var friendlyFromParts = JoinNameParts(
            NormaliseNamePart(givenName),
            NormaliseNamePart(surname));

        if (!string.IsNullOrWhiteSpace(friendlyFromParts))
            return friendlyFromParts;

        var parsedDisplayName = TryParseLastFirstDisplayName(displayName);
        if (!string.IsNullOrWhiteSpace(parsedDisplayName))
            return parsedDisplayName;

        if (!string.IsNullOrWhiteSpace(displayName))
            return displayName.Trim();

        return fallbackEmail?.Trim() ?? string.Empty;
    }

    private static string? TryParseLastFirstDisplayName(string? displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            return null;

        var trimmed = displayName.Trim();
        var commaIndex = trimmed.IndexOf(',');
        if (commaIndex <= 0 || commaIndex >= trimmed.Length - 1)
            return null;

        var surname = trimmed[..commaIndex];
        var remainder = trimmed[(commaIndex + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(surname) || string.IsNullOrWhiteSpace(remainder))
            return null;

        var friendly = JoinNameParts(
            NormaliseNamePart(remainder),
            NormaliseNamePart(surname));

        return string.IsNullOrWhiteSpace(friendly) ? null : friendly;
    }

    private static string JoinNameParts(params string?[] parts)
    {
        return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string? NormaliseNamePart(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        var builder = new StringBuilder(trimmed.Length);
        var lower = trimmed.ToLowerInvariant();
        var capitaliseNext = true;

        foreach (var character in lower)
        {
            if (char.IsLetter(character))
            {
                builder.Append(capitaliseNext ? char.ToUpperInvariant(character) : character);
                capitaliseNext = false;
                continue;
            }

            builder.Append(character);
            capitaliseNext = character is ' ' or '-' or '\'';
        }

        return builder.ToString();
    }
}