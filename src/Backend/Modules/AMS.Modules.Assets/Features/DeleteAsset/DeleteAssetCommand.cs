using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.DeleteAsset;

/// <summary>
/// Remove an asset from the register. Catalogue: Delete an asset - marked as deleted, never physically removed, so history keeps its meaning.
/// </summary>
public sealed record DeleteAssetCommand(
    int Id,
    string? Reason) : ICommand<DeleteAssetResponse>;
