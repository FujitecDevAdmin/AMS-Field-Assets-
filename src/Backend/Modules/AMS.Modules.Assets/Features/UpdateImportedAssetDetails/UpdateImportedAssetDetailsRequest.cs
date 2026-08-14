namespace AMS.Modules.Assets.Features.UpdateImportedAssetDetails;

public sealed record UpdateImportedAssetDetailsRequest(IReadOnlyDictionary<string, string?> Fields);
