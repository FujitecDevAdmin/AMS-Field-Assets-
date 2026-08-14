using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.GetAssetTypeCustomFields;

/// <summary>
/// The custom fields defined for one asset type, with their dropdown options. Catalogue: Define custom fields.
/// </summary>
public sealed record GetAssetTypeCustomFieldsQuery(
    int AssetTypeId,
    bool IncludeInactive) : IQuery<GetAssetTypeCustomFieldsResponse>;
