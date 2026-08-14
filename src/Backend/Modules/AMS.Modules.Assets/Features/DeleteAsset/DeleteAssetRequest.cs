namespace AMS.Modules.Assets.Features.DeleteAsset;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record DeleteAssetRequest(
    string? Reason);
