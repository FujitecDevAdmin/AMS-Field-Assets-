using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.SaveAssetDetails;

/// <summary>
/// Record the 1:1 detail that applies to this asset's type — hardware, software, purchase, vehicle or calibration.
/// </summary>
/// <remarks>
/// One command rather than five, because the detail screen is one form. Which
/// sections it shows is decided by the asset type's behaviour flags, and the
/// handler refuses a section the type does not track — otherwise a laptop could
/// quietly acquire a fitness certificate expiry.
///
/// A null section means "leave it alone", not "delete it". Clearing a detail
/// record is not something the form can express, and it should not be: the
/// values are evidence.
/// </remarks>
public sealed record SaveAssetDetailsCommand(
    int AssetId,
    SaveAssetDetailsCommand.HardwareInput? Hardware,
    SaveAssetDetailsCommand.SoftwareInput? Software,
    SaveAssetDetailsCommand.PurchaseInput? Purchase,
    SaveAssetDetailsCommand.VehicleInput? Vehicle,
    SaveAssetDetailsCommand.InstrumentInput? Instrument) : ICommand<SaveAssetDetailsResponse>
{
    /// <summary>Applies where <c>AssetType.TracksHardware</c> is set.</summary>
    /// <param name="Hostname">Moved here from the asset itself in Revision 3.</param>
    /// <param name="ChassisType">Desktop, Laptop, Tower.</param>
    /// <param name="Processor">As reported, or as typed.</param>
    /// <param name="MemoryGb">Whole gigabytes.</param>
    /// <param name="StorageGb">Whole gigabytes.</param>
    /// <param name="MonitorModel">The attached monitor, if it is not a separate asset.</param>
    /// <param name="MonitorSerialNumber">As above.</param>
    /// <param name="MacAddress">Primary adapter.</param>
    /// <param name="IpAddress">Last known. Discovery overwrites this.</param>
    public sealed record HardwareInput(
        string? Hostname,
        string? ChassisType,
        string? Processor,
        int? MemoryGb,
        int? StorageGb,
        string? MonitorModel,
        string? MonitorSerialNumber,
        string? MacAddress,
        string? IpAddress);

    /// <summary>Applies where <c>AssetType.TracksSoftware</c> is set.</summary>
    /// <param name="OperatingSystem">Windows 11 Pro, and so on.</param>
    /// <param name="OperatingSystemBuild">The build string.</param>
    /// <param name="Architecture">x64, arm64.</param>
    /// <param name="OfficeVersion">The Office release, if any.</param>
    /// <param name="Antivirus">The product in use.</param>
    public sealed record SoftwareInput(
        string? OperatingSystem,
        string? OperatingSystemBuild,
        string? Architecture,
        string? OfficeVersion,
        string? Antivirus);

    /// <summary>How it was bought. Applies to anything.</summary>
    /// <param name="VendorId">Organization.Vendor, id only.</param>
    /// <param name="PurchaseOrderNumber">As raised.</param>
    /// <param name="InvoiceNumber">As received.</param>
    /// <param name="PurchaseDate">When it was bought.</param>
    /// <param name="PurchaseCost">What it cost. Not the book value — SAP owns that.</param>
    /// <param name="WarrantyStartDate">When cover began.</param>
    /// <param name="WarrantyEndDate">When cover ends. Reports read this.</param>
    public sealed record PurchaseInput(
        int? VendorId,
        string? PurchaseOrderNumber,
        string? InvoiceNumber,
        DateOnly? PurchaseDate,
        decimal? PurchaseCost,
        DateOnly? WarrantyStartDate,
        DateOnly? WarrantyEndDate);

    /// <summary>Applies where <c>AssetType.TracksVehicle</c> is set.</summary>
    /// <param name="RegistrationNumber">Unique across the fleet.</param>
    /// <param name="ChassisNumber">As on the registration certificate.</param>
    /// <param name="EngineNumber">As above.</param>
    /// <param name="FuelType">Petrol, Diesel, CNG, Electric.</param>
    /// <param name="FitnessExpiryDate">When the fitness certificate lapses.</param>
    /// <param name="PucExpiryDate">When the emissions certificate lapses.</param>
    /// <param name="InsuranceExpiryDate">
    /// The vehicle's own policy — not the blanket fire policy that covers the
    /// rest of the register, which is a Contract.
    /// </param>
    /// <param name="OdometerKm">Last reading.</param>
    public sealed record VehicleInput(
        string RegistrationNumber,
        string? ChassisNumber,
        string? EngineNumber,
        string? FuelType,
        DateOnly? FitnessExpiryDate,
        DateOnly? PucExpiryDate,
        DateOnly? InsuranceExpiryDate,
        int? OdometerKm);

    /// <summary>Applies where <c>AssetType.TracksCalibration</c> is set.</summary>
    /// <param name="CalibrationStartDate">When the current certificate was issued.</param>
    /// <param name="CalibrationEndDate">When it lapses. The due report reads this.</param>
    /// <param name="CalibrationFrequencyMonths">How often it must be redone.</param>
    /// <param name="CalibrationAgency">Who did it.</param>
    /// <param name="CertificateNumber">Their reference.</param>
    /// <param name="MeasurementRange">What the instrument reads across.</param>
    /// <param name="AccuracyClass">To what tolerance.</param>
    public sealed record InstrumentInput(
        DateOnly? CalibrationStartDate,
        DateOnly? CalibrationEndDate,
        int? CalibrationFrequencyMonths,
        string? CalibrationAgency,
        string? CertificateNumber,
        string? MeasurementRange,
        string? AccuracyClass);
}
