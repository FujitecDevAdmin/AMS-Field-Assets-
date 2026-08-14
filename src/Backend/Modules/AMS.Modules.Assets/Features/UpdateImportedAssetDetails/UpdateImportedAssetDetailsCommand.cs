using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.UpdateImportedAssetDetails;

public sealed record UpdateImportedAssetDetailsCommand(
    int AssetId,
    IReadOnlyDictionary<string, string?> Fields) : ICommand<UpdateImportedAssetDetailsResponse>;
