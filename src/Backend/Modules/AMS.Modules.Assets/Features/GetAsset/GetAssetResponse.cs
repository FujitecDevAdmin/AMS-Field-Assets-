namespace AMS.Modules.Assets.Features.GetAsset;

/// <summary>
/// Everything the detail screen renders, in one round trip.
/// </summary>
/// <remarks>
/// One request rather than seven. The screen cannot draw anything useful until
/// it has the core row, the detail record that applies and the custom fields,
/// so splitting them would only guarantee a half-drawn page.
/// </remarks>
/// <param name="Asset">The register row itself.</param>
/// <param name="HardwareDetail">Null unless the asset type tracks hardware.</param>
/// <param name="SoftwareDetail">Null unless the asset type tracks software.</param>
/// <param name="PurchaseDetail">Null until somebody records a purchase.</param>
/// <param name="VehicleDetail">Null unless the asset type tracks vehicles.</param>
/// <param name="InstrumentDetail">Null unless the asset type tracks calibration.</param>
/// <param name="FinanceDetail">Null unless the caller may read book values AND SAP has synced some.</param>
/// <param name="CustomValues">One entry per field defined for the asset's type, value included.</param>
public sealed record GetAssetResponse(
    GetAssetResponse.Core Asset,
    GetAssetResponse.Hardware? HardwareDetail,
    GetAssetResponse.Software? SoftwareDetail,
    GetAssetResponse.Purchase? PurchaseDetail,
    GetAssetResponse.Vehicle? VehicleDetail,
    GetAssetResponse.Instrument? InstrumentDetail,
    GetAssetResponse.Finance? FinanceDetail,
    IReadOnlyList<GetAssetResponse.CustomValue> CustomValues)
{
    /// <summary>The register row, with the lookups resolved.</summary>
    /// <param name="Id">The asset.</param>
    /// <param name="AssetNumber">Unique.</param>
    /// <param name="AssetName">As stored.</param>
    /// <param name="SerialNumber">Null on bulk lines and on anything without one.</param>
    /// <param name="AssetTypeId">What the thing is.</param>
    /// <param name="TypeName">Its name, for the header.</param>
    /// <param name="AssetClassId">How the accounts see it. Null until classified.</param>
    /// <param name="ClassName">Its name, or null.</param>
    /// <param name="AssetStatusId">Where it is in its life.</param>
    /// <param name="StatusName">Its name, for the header.</param>
    /// <param name="Make">Promoted onto the asset in Revision 3.</param>
    /// <param name="Model">As above.</param>
    /// <param name="CurrentLocationId">Branch, id only — Organization is another module.</param>
    /// <param name="CurrentEmployeeId">Holder, id only.</param>
    /// <param name="DepartmentId">Id only.</param>
    /// <param name="CostCenter">As stored.</param>
    /// <param name="AcquisitionDate">When it was acquired, if known.</param>
    /// <param name="QrCodeValue">The printed tag value.</param>
    /// <param name="BarcodeValue">The printed barcode.</param>
    /// <param name="IsBulk">Whether this line is counted rather than issued.</param>
    /// <param name="Quantity">Always 1 for a unit asset.</param>
    /// <param name="UnitOfMeasure">Null unless the line is bulk.</param>
    /// <param name="CapitalisedFromAssetId">The asset under construction this settled from.</param>
    /// <param name="SplitFromAssetId">The bulk line this was carved out of.</param>
    /// <param name="Remarks">Free text.</param>
    /// <param name="IsDeleted">Removed assets keep their record and their timeline.</param>
    /// <param name="LastPhysicalCheckOnUtc">When somebody last stood in front of it.</param>
    /// <param name="TracksHardware">Whether the screen should show the hardware section.</param>
    /// <param name="TracksSoftware">As above, for software.</param>
    /// <param name="TracksVehicle">As above, for vehicles.</param>
    /// <param name="TracksCalibration">As above, for calibration.</param>
    public sealed record Core(
        int Id,
        string AssetNumber,
        string AssetName,
        string? SerialNumber,
        int AssetTypeId,
        string TypeName,
        int? AssetClassId,
        string? ClassName,
        int AssetStatusId,
        string StatusName,
        string? Make,
        string? Model,
        int? CurrentLocationId,
        int? CurrentEmployeeId,
        int? DepartmentId,
        string? CostCenter,
        DateOnly? AcquisitionDate,
        string? QrCodeValue,
        string? BarcodeValue,
        bool IsBulk,
        decimal Quantity,
        string? UnitOfMeasure,
        int? CapitalisedFromAssetId,
        int? SplitFromAssetId,
        string? Remarks,
        bool IsDeleted,
        DateTime? LastPhysicalCheckOnUtc,
        bool TracksHardware,
        bool TracksSoftware,
        bool TracksVehicle,
        bool TracksCalibration);

    /// <summary>The hardware detail record.</summary>
    public sealed record Hardware(
        string? Hostname,
        string? ChassisType,
        string? Processor,
        int? MemoryGb,
        int? StorageGb,
        string? MonitorModel,
        string? MonitorSerialNumber,
        string? MacAddress,
        string? IpAddress);

    /// <summary>The software detail record. The OS key is never returned.</summary>
    /// <remarks>
    /// <c>OsKeyEncrypted</c> is deliberately absent. It is stored encrypted so
    /// that reading the table does not reveal it, and putting it on a detail
    /// screen would undo that for the sake of a field nobody reads off a page.
    /// </remarks>
    public sealed record Software(
        string? OperatingSystem,
        string? OperatingSystemBuild,
        string? Architecture,
        string? OfficeVersion,
        string? Antivirus);

    /// <summary>How it was bought.</summary>
    public sealed record Purchase(
        int? VendorId,
        string? PurchaseOrderNumber,
        string? InvoiceNumber,
        DateOnly? PurchaseDate,
        decimal? PurchaseCost,
        DateOnly? WarrantyStartDate,
        DateOnly? WarrantyEndDate);

    /// <summary>The vehicle detail record.</summary>
    public sealed record Vehicle(
        string RegistrationNumber,
        string? ChassisNumber,
        string? EngineNumber,
        string? FuelType,
        DateOnly? FitnessExpiryDate,
        DateOnly? PucExpiryDate,
        DateOnly? InsuranceExpiryDate,
        int? OdometerKm);

    /// <summary>The calibration record.</summary>
    public sealed record Instrument(
        DateOnly? CalibrationStartDate,
        DateOnly? CalibrationEndDate,
        int? CalibrationFrequencyMonths,
        string? CalibrationAgency,
        string? CertificateNumber,
        string? MeasurementRange,
        string? AccuracyClass);

    /// <summary>Book values, mirrored from SAP and read-only everywhere in AMS.</summary>
    /// <remarks>
    /// There is no command that writes these. SAP S/4HANA owns the arithmetic,
    /// and two systems calculating one number is how one asset ends up with two
    /// answers on two reports.
    /// </remarks>
    /// <param name="GrossValue">As last synced.</param>
    /// <param name="AccumulatedDepreciation">As last synced.</param>
    /// <param name="NetBookValue">As last synced.</param>
    /// <param name="DepreciationMethod">StraightLine, WrittenDownValue or None.</param>
    /// <param name="DepreciationPercent">The rate SAP applies.</param>
    /// <param name="UsefulLifeMonths">Over how long.</param>
    /// <param name="AucReference">The asset under construction it was capitalised from.</param>
    /// <param name="LastSyncedOnUtc">
    /// When these numbers were last true. Null means the row was keyed or
    /// imported rather than synced, and the screen should say so rather than
    /// imply the figures are current.
    /// </param>
    public sealed record Finance(
        decimal? GrossValue,
        decimal? AccumulatedDepreciation,
        decimal? NetBookValue,
        string? DepreciationMethod,
        decimal? DepreciationPercent,
        int? UsefulLifeMonths,
        string? AucReference,
        DateTime? LastSyncedOnUtc);

    /// <summary>One custom field and the value captured against it.</summary>
    /// <param name="CustomFieldDefinitionId">The field.</param>
    /// <param name="FieldName">Its key.</param>
    /// <param name="DisplayLabel">What the form shows.</param>
    /// <param name="FieldType">Text, Number, Percentage, Date, Boolean or Dropdown.</param>
    /// <param name="IsRequired">Whether the form may be saved without it.</param>
    /// <param name="Value">The text value, or null.</param>
    /// <param name="ValueNumber">The numeric value, or null.</param>
    /// <param name="ValueDate">The date value, or null.</param>
    /// <param name="OptionId">The chosen dropdown option, or null.</param>
    /// <param name="Options">Every option, so the picker can render without a second request.</param>
    public sealed record CustomValue(
        int CustomFieldDefinitionId,
        string FieldName,
        string DisplayLabel,
        string FieldType,
        bool IsRequired,
        string? Value,
        decimal? ValueNumber,
        DateOnly? ValueDate,
        int? OptionId,
        IReadOnlyList<CustomValueOption> Options);

    /// <summary>One selectable dropdown value.</summary>
    /// <param name="Id">The option.</param>
    /// <param name="OptionValue">What it says.</param>
    public sealed record CustomValueOption(int Id, string OptionValue);
}
