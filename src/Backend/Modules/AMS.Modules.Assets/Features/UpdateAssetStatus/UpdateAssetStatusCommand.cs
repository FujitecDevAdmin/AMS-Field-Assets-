using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.UpdateAssetStatus;

/// <summary>
/// Rename a status, reorder it, or retire it.
/// </summary>
public sealed record UpdateAssetStatusCommand(
    int Id,
    string StatusName,
    bool IsTerminal,
    int DisplayOrder,
    bool IsActive) : ICommand<UpdateAssetStatusResponse>;
