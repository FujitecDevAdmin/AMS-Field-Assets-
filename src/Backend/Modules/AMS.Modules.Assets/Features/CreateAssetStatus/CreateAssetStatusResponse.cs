namespace AMS.Modules.Assets.Features.CreateAssetStatus;

/// <summary>
/// The new status.
/// </summary>
/// <param name="Id">The new status.</param>
/// <param name="StatusName">Unique, trimmed.</param>
/// <param name="IsTerminal">A terminal status ends the asset's working life - Scrapped, Lost and Disposed are the seeded ones. An asset in a terminal status cannot be allocated again.</param>
public sealed record CreateAssetStatusResponse(
    int Id,
    string StatusName,
    bool IsTerminal);
