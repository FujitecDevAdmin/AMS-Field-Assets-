using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.CreateAssetStatus;

/// <summary>
/// Add an asset status. Catalogue: Status lookup maintenance.
/// </summary>
public sealed record CreateAssetStatusCommand(
    string StatusName,
    bool IsTerminal,
    int DisplayOrder) : ICommand<CreateAssetStatusResponse>;
