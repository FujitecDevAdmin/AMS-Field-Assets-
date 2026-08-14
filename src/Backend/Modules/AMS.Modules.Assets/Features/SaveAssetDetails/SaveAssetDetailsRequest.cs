namespace AMS.Modules.Assets.Features.SaveAssetDetails;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SaveAssetDetailsRequest(
    SaveAssetDetailsCommand.HardwareInput? Hardware,
    SaveAssetDetailsCommand.SoftwareInput? Software,
    SaveAssetDetailsCommand.PurchaseInput? Purchase,
    SaveAssetDetailsCommand.VehicleInput? Vehicle,
    SaveAssetDetailsCommand.InstrumentInput? Instrument);
