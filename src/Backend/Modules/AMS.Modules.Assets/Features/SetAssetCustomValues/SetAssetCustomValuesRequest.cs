namespace AMS.Modules.Assets.Features.SetAssetCustomValues;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SetAssetCustomValuesRequest(
    IReadOnlyList<SetAssetCustomValuesCommand.Entry>? Values);
