using Compass.Models.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

/// <summary>
/// Resolves which model version owns the live/admin structure (themes and questions).
/// Published wins so respondents and the overview share the same definition after publish.
/// </summary>
public static class ServiceDataModelStructureVersion
{
    public static ServiceDataModelVersion? Resolve(
        ServiceDataModelVersion? published,
        ServiceDataModelVersion? draft) =>
        published ?? draft;

    public static bool AllowsEdits(ServiceDataModelLifecycleStatus status) =>
        status is ServiceDataModelLifecycleStatus.Draft
            or ServiceDataModelLifecycleStatus.Published;
}
