using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Compass.Helpers;

/// <summary>Binds GOV.UK Design System date inputs (day / month / year) to <see cref="DateTime"/> (UTC date).</summary>
public static class GovUkDateBinding
{
    public static void BindGovUkDate(
        ModelStateDictionary modelState,
        string modelKey,
        int? day,
        int? month,
        int? year,
        bool required,
        out DateTime? dateUtc)
    {
        dateUtc = null;
        var any = day.HasValue || month.HasValue || year.HasValue;
        if (!required && !any)
            return;

        if (!day.HasValue || !month.HasValue || !year.HasValue)
        {
            modelState.AddModelError(modelKey, required ? "Enter the full date." : "Enter the full date or leave all fields blank.");
            return;
        }

        if (day.Value is < 1 or > 31 || month.Value is < 1 or > 12 || year.Value is < 1000 or > 9999)
        {
            modelState.AddModelError(modelKey, "Enter a valid date.");
            return;
        }

        try
        {
            dateUtc = new DateTime(year.Value, month.Value, day.Value, 0, 0, 0, DateTimeKind.Utc);
        }
        catch
        {
            modelState.AddModelError(modelKey, "Enter a valid date.");
        }
    }
}
