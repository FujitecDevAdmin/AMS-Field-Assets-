namespace AMS.Modules.Assets.Features.GetAsset;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record GetAssetRequest(
    int Id);
