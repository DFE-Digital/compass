using Compass.Models.ServiceDataModels;
using Compass.ViewModels.Modern.ServiceDataModels;

namespace Compass.Services.ServiceDataModels;

public interface IServiceDataModelAdminService
{
    Task<ServiceDataModelListViewModel> ListModelsAsync(string actorEmail);

    Task<Guid> CreateModelAsync(
        string stableKey,
        string name,
        string? description,
        string? ownerDisplayName,
        string? ownerEmail,
        string? classification,
        bool isRepeatable,
        string actorEmail);

    Task<ServiceDataModelDetailViewModel?> GetModelDetailAsync(Guid modelId, string actorEmail);

    Task<bool> UpdateModelSettingsAsync(Guid modelId, ServiceDataModelSettingsInput input, string actorEmail);

    Task<Guid?> EnsureDraftVersionAsync(Guid modelId, string actorEmail);

    Task<Guid?> AddGroupAsync(Guid modelId, ServiceDataModelGroupInput input, string actorEmail);

    Task<bool> UpdateGroupAsync(Guid groupId, ServiceDataModelGroupInput input, string actorEmail);

    Task<bool> SetGroupDisabledAsync(Guid groupId, bool isDisabled, string actorEmail);

    Task<bool> ReorderGroupsAsync(Guid modelId, IReadOnlyList<Guid> orderedGroupIds, string actorEmail);

    Task<Guid?> AddFieldAsync(Guid groupId, ServiceDataModelFieldInput input, string actorEmail);

    Task<bool> UpdateFieldAsync(Guid fieldId, ServiceDataModelFieldInput input, string actorEmail);

    Task<bool> SetFieldDisabledAsync(Guid fieldId, bool isDisabled, string actorEmail);

    Task<bool> ReorderFieldsAsync(Guid groupId, IReadOnlyList<Guid> orderedFieldIds, string actorEmail);

    Task<Guid?> AddFieldOptionAsync(Guid fieldId, ServiceDataModelFieldOptionInput input, string actorEmail);

    Task<bool> SetApplicabilityAsync(Guid modelId, ServiceDataModelApplicabilityInput input, string actorEmail);

    Task<ServiceDataModelApplicabilityPreviewViewModel?> PreviewApplicabilityAsync(Guid modelId);

    Task<bool> PublishVersionAsync(Guid modelId, ServiceDataModelPublishInput input, string actorEmail);

    Task<bool> RetireModelAsync(Guid modelId, string actorEmail);

    Task<Guid?> SeedDefaultServiceCensusDraftAsync(string actorEmail);
}
