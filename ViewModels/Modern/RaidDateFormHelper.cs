using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Compass.ViewModels.Modern;

public static class RaidDateFormHelper
{
    public static void SplitDateParts(DateTime? d, out int? day, out int? month, out int? year)
    {
        if (d.HasValue)
        {
            var v = d.Value;
            day = v.Day;
            month = v.Month;
            year = v.Year;
        }
        else
        {
            day = null;
            month = null;
            year = null;
        }
    }

    /// <summary>All three parts empty → null. Any part set → all must be set and form a valid calendar date.</summary>
    public static bool TryOptionalDate(
        int? day,
        int? month,
        int? year,
        string modelStateKey,
        ModelStateDictionary modelState,
        out DateTime? result)
    {
        result = null;
        var any = day.HasValue || month.HasValue || year.HasValue;
        if (!any)
            return true;
        if (!day.HasValue || !month.HasValue || !year.HasValue)
        {
            modelState.AddModelError(modelStateKey, "Enter the complete date.");
            return false;
        }

        try
        {
            result = new DateTime(year.Value, month.Value, day.Value, 0, 0, 0, DateTimeKind.Utc);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            modelState.AddModelError(modelStateKey, "Enter a valid date.");
            return false;
        }
    }

    /// <summary>All three parts required; forms a valid calendar date (UTC midnight).</summary>
    public static bool TryRequiredDate(
        int? day,
        int? month,
        int? year,
        string modelStateKey,
        ModelStateDictionary modelState,
        out DateTime? result)
    {
        result = null;
        var any = day.HasValue || month.HasValue || year.HasValue;
        if (!any)
        {
            modelState.AddModelError(modelStateKey, "Enter the date.");
            return false;
        }

        if (!day.HasValue || !month.HasValue || !year.HasValue)
        {
            modelState.AddModelError(modelStateKey, "Enter the complete date.");
            return false;
        }

        try
        {
            result = new DateTime(year.Value, month.Value, day.Value, 0, 0, 0, DateTimeKind.Utc);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            modelState.AddModelError(modelStateKey, "Enter a valid date.");
            return false;
        }
    }
}
