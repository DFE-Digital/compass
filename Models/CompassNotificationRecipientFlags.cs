namespace Compass.Models;

/// <summary>Bitmask for default recipients on Compass reminder emails (stored on <see cref="CompassNotificationSetting"/>).</summary>
[Flags]
public enum CompassNotificationRecipientFlags
{
    None = 0,
    /// <summary>FIPS service owner when the subject is linked to a product/service.</summary>
    FipsServiceOwner = 1,
    /// <summary>Primary work contact on the work item.</summary>
    PrimaryWorkContact = 2,
    /// <summary>Central Operations distribution (from application configuration when sending).</summary>
    CentralOps = 4,
    /// <summary>Risk owner or issue owner / creator on the RAID record.</summary>
    RiskIssueOwnerOrCreator = 8,
    /// <summary>PMO contact(s) on the work item.</summary>
    PmoContact = 16,
    /// <summary>User who created the work item.</summary>
    WorkItemCreator = 32,
}
