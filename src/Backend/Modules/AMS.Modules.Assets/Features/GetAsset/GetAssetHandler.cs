using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.GetAsset;

/// <summary>One asset in full. Catalogue screen: Asset Detail and Timeline.</summary>
/// <remarks>
/// Deleted assets ARE returned. History points at them, so following a link
/// from a movement or a ticket to a removed asset must show the record and say
/// it was removed, not produce a 404 that reads like the link is broken.
/// </remarks>
public sealed class GetAssetHandler(AssetsDbContext db, ICurrentUser currentUser)
    : IRequestHandler<GetAssetQuery, GetAssetResponse>
{
    public async Task<Result<GetAssetResponse>> HandleAsync(
        GetAssetQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var core = await db.Assets
            .AsNoTracking()
            .Where(a => a.Id == request.Id)
            .Select(a => new
            {
                Asset = a,
                Type = db.AssetTypes.Single(t => t.Id == a.AssetTypeId),
                ClassName = db.AssetClasses
                    .Where(c => c.Id == a.AssetClassId).Select(c => c.ClassName).FirstOrDefault(),
                StatusName = db.AssetStatuses
                    .Where(s => s.Id == a.AssetStatusId).Select(s => s.StatusName).Single(),
            })
            .SingleOrDefaultAsync(ct);

        if (core is null)
        {
            return Error.NotFound("Asset", request.Id);
        }

        var a = core.Asset;
        var type = core.Type;

        if (!currentUser.HasAllBranches
            && a.CurrentLocationId is { } branch
            && !currentUser.BranchIds.Contains(branch))
        {
            // A 404 and not a 403: telling somebody an asset exists at a branch
            // they cannot see is itself a disclosure.
            return Error.NotFound("Asset", request.Id);
        }

        var detail = new GetAssetResponse.Core(
            a.Id, a.AssetNumber, a.AssetName, a.SerialNumber,
            a.AssetTypeId, type.TypeName,
            a.AssetClassId, core.ClassName,
            a.AssetStatusId, core.StatusName,
            a.Make, a.Model,
            a.CurrentLocationId, a.CurrentEmployeeId, a.DepartmentId, a.CostCenter,
            a.AcquisitionDate, a.QrCodeValue, a.BarcodeValue,
            a.IsBulk, a.Quantity, a.UnitOfMeasure,
            a.CapitalisedFromAssetId, a.SplitFromAssetId,
            a.Remarks, a.IsDeleted, a.LastPhysicalCheckOnUtc,
            type.TracksHardware, type.TracksSoftware, type.TracksVehicle, type.TracksCalibration);

        // Each section is fetched only when the type says it applies, so a chair
        // does not cost four pointless round trips.
        var hardware = type.TracksHardware
            ? await db.AssetHardwareDetails.AsNoTracking()
                .Where(h => h.AssetId == a.Id)
                .Select(h => new GetAssetResponse.Hardware(
                    h.Hostname, h.ChassisType, h.Processor, h.MemoryGb, h.StorageGb,
                    h.MonitorModel, h.MonitorSerialNumber, h.MacAddress, h.IpAddress))
                .SingleOrDefaultAsync(ct)
            : null;

        var software = type.TracksSoftware
            ? await db.AssetSoftwareDetails.AsNoTracking()
                .Where(s => s.AssetId == a.Id)
                .Select(s => new GetAssetResponse.Software(
                    s.OperatingSystem, s.OperatingSystemBuild, s.Architecture,
                    s.OfficeVersion, s.Antivirus))
                .SingleOrDefaultAsync(ct)
            : null;

        var vehicle = type.TracksVehicle
            ? await db.AssetVehicleDetails.AsNoTracking()
                .Where(v => v.AssetId == a.Id)
                .Select(v => new GetAssetResponse.Vehicle(
                    v.RegistrationNumber, v.ChassisNumber, v.EngineNumber, v.FuelType,
                    v.FitnessExpiryDate, v.PucExpiryDate, v.InsuranceExpiryDate, v.OdometerKm))
                .SingleOrDefaultAsync(ct)
            : null;

        var instrument = type.TracksCalibration
            ? await db.AssetInstrumentDetails.AsNoTracking()
                .Where(i => i.AssetId == a.Id)
                .Select(i => new GetAssetResponse.Instrument(
                    i.CalibrationStartDate, i.CalibrationEndDate, i.CalibrationFrequencyMonths,
                    i.CalibrationAgency, i.CertificateNumber, i.MeasurementRange, i.AccuracyClass))
                .SingleOrDefaultAsync(ct)
            : null;

        // Purchase applies to anything that was bought, which is everything.
        var purchase = await db.AssetPurchaseDetails.AsNoTracking()
            .Where(p => p.AssetId == a.Id)
            .Select(p => new GetAssetResponse.Purchase(
                p.VendorId, p.PurchaseOrderNumber, p.InvoiceNumber, p.PurchaseDate,
                p.PurchaseCost, p.WarrantyStartDate, p.WarrantyEndDate))
            .SingleOrDefaultAsync(ct);

        // Book values are behind their own capability, and there is no matching
        // .manage anywhere: SAP owns the arithmetic.
        var finance = currentUser.Capabilities.Contains(Capabilities.Assets.FinanceView)
            ? await db.AssetFinances.AsNoTracking()
                .Where(f => f.AssetId == a.Id)
                .Select(f => new GetAssetResponse.Finance(
                    f.GrossValue, f.AccumulatedDepreciation, f.NetBookValue,
                    f.DepreciationMethod, f.DepreciationPercent, f.UsefulLifeMonths,
                    f.AucReference, f.LastSyncedOnUtc))
                .SingleOrDefaultAsync(ct)
            : null;

        // Every field defined for the type, whether or not it has a value yet:
        // the form has to render the empty ones too.
        var customValues = await db.CustomFieldDefinitions
            .AsNoTracking()
            .Where(f => f.AssetTypeId == a.AssetTypeId && f.IsActive)
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.FieldName)
            .Select(f => new GetAssetResponse.CustomValue(
                f.Id,
                f.FieldName,
                f.DisplayLabel,
                f.FieldType,
                f.IsRequired,
                db.AssetCustomValues.Where(v => v.AssetId == a.Id && v.CustomFieldDefinitionId == f.Id)
                    .Select(v => v.Value).FirstOrDefault(),
                db.AssetCustomValues.Where(v => v.AssetId == a.Id && v.CustomFieldDefinitionId == f.Id)
                    .Select(v => v.ValueNumber).FirstOrDefault(),
                db.AssetCustomValues.Where(v => v.AssetId == a.Id && v.CustomFieldDefinitionId == f.Id)
                    .Select(v => v.ValueDate).FirstOrDefault(),
                db.AssetCustomValues.Where(v => v.AssetId == a.Id && v.CustomFieldDefinitionId == f.Id)
                    .Select(v => v.OptionId).FirstOrDefault(),
                db.CustomFieldOptions.Where(o => o.CustomFieldDefinitionId == f.Id && o.IsActive)
                    .OrderBy(o => o.DisplayOrder)
                    .ThenBy(o => o.OptionValue)
                    .Select(o => new GetAssetResponse.CustomValueOption(o.Id, o.OptionValue))
                    .ToList()))
            .ToListAsync(ct);

        return new GetAssetResponse(
            detail, hardware, software, purchase, vehicle, instrument, finance, customValues);
    }
}
