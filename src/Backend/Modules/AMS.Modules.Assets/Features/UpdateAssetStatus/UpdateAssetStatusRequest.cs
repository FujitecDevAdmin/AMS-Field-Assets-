namespace AMS.Modules.Assets.Features.UpdateAssetStatus;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record UpdateAssetStatusRequest(
    string StatusName,
    bool IsTerminal,
    int? DisplayOrder,
    bool IsActive);
