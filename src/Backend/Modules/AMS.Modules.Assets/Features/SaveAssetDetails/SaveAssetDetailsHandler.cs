using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.SaveAssetDetails;

/// <summary>
/// Record the 1:1 detail that applies to this asset's type — hardware,
/// software, purchase, vehicle or calibration.
/// </summary>
/// <remarks>
/// Which sections are allowed is read from the asset type's behaviour flags,
/// not from a list here. A laptop cannot acquire a fitness certificate expiry
/// and a vehicle cannot acquire a processor, and refusing that is the whole
/// reason Revision 3 put the flags on the type rather than hardcoding IT.
/// </remarks>
public sealed class SaveAssetDetailsHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<SaveAssetDetailsCommand, SaveAssetDetailsResponse>
{
    public async Task<Result<SaveAssetDetailsResponse>> HandleAsync(
        SaveAssetDetailsCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await db.Assets
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == request.AssetId, ct);
        if (asset is null)
        {
            return Error.NotFound("Asset", request.AssetId);
        }

        if (asset.IsDeleted)
        {
            return Error.Validation(
                "Asset.Deleted", "That asset has been removed from the register.");
        }

        var type = await db.AssetTypes
            .AsNoTracking()
            .SingleAsync(t => t.Id == asset.AssetTypeId, ct);

        var refused = Refuse(request, type);
        if (refused is not null)
        {
            return refused;
        }

        var saved = new List<string>();

        if (request.Hardware is { } hardware)
        {
            var row = await db.AssetHardwareDetails
                .SingleOrDefaultAsync(h => h.AssetId == asset.Id, ct);
            if (row is null)
            {
                row = new AssetHardwareDetail { AssetId = asset.Id, CreatedOnUtc = clock.UtcNow, CreatedBy = currentUser.Username };
                db.AssetHardwareDetails.Add(row);
            }
            else
            {
                row.ModifiedOnUtc = clock.UtcNow;
                row.ModifiedBy = currentUser.Username;
            }

            row.Hostname = Clean(hardware.Hostname);
            row.ChassisType = Clean(hardware.ChassisType);
            row.Processor = Clean(hardware.Processor);
            row.MemoryGb = hardware.MemoryGb;
            row.StorageGb = hardware.StorageGb;
            row.MonitorModel = Clean(hardware.MonitorModel);
            row.MonitorSerialNumber = Clean(hardware.MonitorSerialNumber);
            row.MacAddress = Clean(hardware.MacAddress);
            row.IpAddress = Clean(hardware.IpAddress);
            saved.Add("Hardware");
        }

        if (request.Software is { } software)
        {
            var row = await db.AssetSoftwareDetails
                .SingleOrDefaultAsync(s => s.AssetId == asset.Id, ct);
            if (row is null)
            {
                row = new AssetSoftwareDetail { AssetId = asset.Id, CreatedOnUtc = clock.UtcNow, CreatedBy = currentUser.Username };
                db.AssetSoftwareDetails.Add(row);
            }
            else
            {
                row.ModifiedOnUtc = clock.UtcNow;
                row.ModifiedBy = currentUser.Username;
            }

            row.OperatingSystem = Clean(software.OperatingSystem);
            row.OperatingSystemBuild = Clean(software.OperatingSystemBuild);
            row.Architecture = Clean(software.Architecture);
            row.OfficeVersion = Clean(software.OfficeVersion);
            row.Antivirus = Clean(software.Antivirus);
            saved.Add("Software");
        }

        if (request.Purchase is { } purchase)
        {
            if (purchase.WarrantyEndDate is { } end && purchase.WarrantyStartDate is { } start
                && end < start)
            {
                return Error.Validation(
                    "Purchase.WarrantyWindow", "Warranty cover cannot end before it starts.");
            }

            var row = await db.AssetPurchaseDetails
                .SingleOrDefaultAsync(p => p.AssetId == asset.Id, ct);
            if (row is null)
            {
                row = new AssetPurchaseDetail { AssetId = asset.Id, CreatedOnUtc = clock.UtcNow, CreatedBy = currentUser.Username };
                db.AssetPurchaseDetails.Add(row);
            }
            else
            {
                row.ModifiedOnUtc = clock.UtcNow;
                row.ModifiedBy = currentUser.Username;
            }

            row.VendorId = purchase.VendorId;
            row.PurchaseOrderNumber = Clean(purchase.PurchaseOrderNumber);
            row.InvoiceNumber = Clean(purchase.InvoiceNumber);
            row.PurchaseDate = purchase.PurchaseDate;
            row.PurchaseCost = purchase.PurchaseCost;
            row.WarrantyStartDate = purchase.WarrantyStartDate;
            row.WarrantyEndDate = purchase.WarrantyEndDate;
            saved.Add("Purchase");
        }

        if (request.Vehicle is { } vehicle)
        {
            if (string.IsNullOrWhiteSpace(vehicle.RegistrationNumber))
            {
                return Error.Validation(
                    "Vehicle.RegistrationRequired", "A vehicle needs a registration number.");
            }

            var row = await db.AssetVehicleDetails
                .SingleOrDefaultAsync(v => v.AssetId == asset.Id, ct);
            if (row is null)
            {
                row = new AssetVehicleDetail
                {
                    AssetId = asset.Id,
                    RegistrationNumber = vehicle.RegistrationNumber.Trim(),
                    CreatedOnUtc = clock.UtcNow,
                    CreatedBy = currentUser.Username,
                };
                db.AssetVehicleDetails.Add(row);
            }
            else
            {
                row.ModifiedOnUtc = clock.UtcNow;
                row.ModifiedBy = currentUser.Username;
            }

            row.RegistrationNumber = vehicle.RegistrationNumber.Trim();
            row.ChassisNumber = Clean(vehicle.ChassisNumber);
            row.EngineNumber = Clean(vehicle.EngineNumber);
            row.FuelType = Clean(vehicle.FuelType);
            row.FitnessExpiryDate = vehicle.FitnessExpiryDate;
            row.PucExpiryDate = vehicle.PucExpiryDate;
            row.InsuranceExpiryDate = vehicle.InsuranceExpiryDate;
            row.OdometerKm = vehicle.OdometerKm;
            saved.Add("Vehicle");
        }

        if (request.Instrument is { } instrument)
        {
            // CK_AssetInstrumentDetail_Window says this too. Saying it here
            // turns a 500 into a message beside the field.
            if (instrument.CalibrationEndDate is { } end && instrument.CalibrationStartDate is { } start
                && end < start)
            {
                return Error.Validation(
                    "Instrument.CalibrationWindow",
                    "A calibration cannot expire before it was issued.");
            }

            var row = await db.AssetInstrumentDetails
                .SingleOrDefaultAsync(i => i.AssetId == asset.Id, ct);
            if (row is null)
            {
                row = new AssetInstrumentDetail { AssetId = asset.Id, CreatedOnUtc = clock.UtcNow, CreatedBy = currentUser.Username };
                db.AssetInstrumentDetails.Add(row);
            }
            else
            {
                row.ModifiedOnUtc = clock.UtcNow;
                row.ModifiedBy = currentUser.Username;
            }

            row.CalibrationStartDate = instrument.CalibrationStartDate;
            row.CalibrationEndDate = instrument.CalibrationEndDate;
            row.CalibrationFrequencyMonths = instrument.CalibrationFrequencyMonths;
            row.CalibrationAgency = Clean(instrument.CalibrationAgency);
            row.CertificateNumber = Clean(instrument.CertificateNumber);
            row.MeasurementRange = Clean(instrument.MeasurementRange);
            row.AccuracyClass = Clean(instrument.AccuracyClass);
            saved.Add("Instrument");
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new SaveAssetDetailsResponse(asset.Id, saved);
    }

    /// <summary>The section this asset's type does not track, if one was sent.</summary>
    private static Error? Refuse(SaveAssetDetailsCommand request, AssetType type)
    {
        if (request.Hardware is not null && !type.TracksHardware)
        {
            return NotTracked(type, "hardware");
        }

        if (request.Software is not null && !type.TracksSoftware)
        {
            return NotTracked(type, "software");
        }

        if (request.Vehicle is not null && !type.TracksVehicle)
        {
            return NotTracked(type, "vehicle");
        }

        if (request.Instrument is not null && !type.TracksCalibration)
        {
            return NotTracked(type, "calibration");
        }

        return null;
    }

    private static Error NotTracked(AssetType type, string kind) =>
        Error.Validation(
            "Asset.DetailNotTracked",
            $"'{type.TypeName}' does not track {kind} details. "
            + $"Turn that on for the asset type first if it should.");

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
