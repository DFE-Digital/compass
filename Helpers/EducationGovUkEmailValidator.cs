namespace Compass.Helpers;

public static class EducationGovUkEmailValidator
{
    /// <summary>True if the address uses the <c>education.gov.uk</c> domain (single label, case-insensitive).</summary>
    public static bool IsAllowed(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var e = email.Trim();
        if (e.Length > 256)
            return false;

        var at = e.AsSpan().LastIndexOf('@');
        if (at < 1 || at == e.Length - 1)
            return false;

        var local = e[..at];
        if (string.IsNullOrWhiteSpace(local) || local.Contains("..", StringComparison.Ordinal) || local.Contains(' ', StringComparison.Ordinal))
            return false;

        var domain = e[(at + 1)..].Trim();
        return string.Equals(domain, "education.gov.uk", StringComparison.OrdinalIgnoreCase);
    }
}
