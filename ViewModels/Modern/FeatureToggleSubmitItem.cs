namespace Compass.ViewModels.Modern;

/// <summary>Posted from Feature settings form (<c>featureToggles[i].*</c>).</summary>
public class FeatureToggleSubmitItem
{
    public string Code { get; set; } = "";

    /// <summary><see cref="Models.FeatureAccessMode"/> as int (0=Off, 1=On for all, 2=On for some).</summary>
    public int AccessMode { get; set; } = 1;

    public List<int> AllowedUserIds { get; set; } = new();

    public List<int> AllowedGroupIds { get; set; } = new();
}
