using Compass.Models.Fips;

namespace Compass.Services.Fips;

public enum FipsCmdbDailySyncOutcome
{
    Disabled,
    FeatureOff,
    OutsideWindow,
    AlreadyRanThisSlot,
    Busy,
    Due,
    Completed,
    Failed
}

public interface IFipsCmdbDailySyncService
{
    Task<FipsCmdbDailySyncOutcome> RunDailySyncIfDueAsync(CancellationToken cancellationToken = default);
}
