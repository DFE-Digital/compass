namespace Compass.Models;

/// <summary>How a product area feature is enabled in Admin → Feature settings.</summary>
public enum FeatureAccessMode
{
    /// <summary>Hidden and blocked for everyone (subject to other checks).</summary>
    Off = 0,

    /// <summary>Available to all signed-in users (previous default "On").</summary>
    OnForAll = 1,

    /// <summary>Only users explicitly listed may access; everyone else is treated as off for nav and gates.</summary>
    OnForSome = 2
}
