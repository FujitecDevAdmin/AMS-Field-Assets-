using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Verification.Domain;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Features.SubmitVerification;

/// <summary>
/// Record a sighting or a bulk count. Catalogue: the mobile capture.
/// </summary>
/// <remarks>
/// <para>
/// The endpoint a phone posts to, possibly hours after the capture and
/// possibly more than once. Everything here follows from that.
/// </para>
/// <para>
/// <b>The two duplicate cases are told apart.</b> R2-21 exists for it: the
/// phone generates <c>ClientCaptureId</c> at capture and sends the same value
/// on every retry, so a repeat from the same device hits
/// <c>UX_PhysicalVerification_ClientCapture</c> and a genuine clash hits
/// <c>UX_PhysicalVerification_OnePerUnitAssetPerCycle</c>. Both are SQL 2601
/// and they deserve different words: "you already sent this, here it is again"
/// versus "somebody else verified this asset first". Calling every retry a
/// conflict is how technicians learn to ignore conflicts.
/// </para>
/// <para>
/// <b>The phone's time is kept.</b> <c>VerifiedOnUtc</c> comes from the
/// device, because the capture happened when the technician was standing in
/// front of the asset, not when the signal came back.
/// </para>
/// </remarks>
public sealed class SubmitVerificationHandler(
    VerificationDbContext db,
    IAssetSnapshot assets,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SubmitVerificationCommand, SubmitVerificationResponse>
{
    public async Task<Result<SubmitVerificationResponse>> HandleAsync(
        SubmitVerificationCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!WorkingCondition.Allowed.Contains(request.WorkingCondition, StringComparer.Ordinal))
        {
            return Error.Validation(
                "Verification.UnknownCondition",
                $"Condition must be one of {string.Join(", ", WorkingCondition.Allowed)}.");
        }

        var asset = await assets.GetAsync(request.AssetId, ct);
        if (asset is null)
        {
            return Error.NotFound("Asset", request.AssetId);
        }

        var cycle = await db.PhysicalVerificationCycles
            .SingleOrDefaultAsync(c => c.IsActive, ct);

        if (cycle is null)
        {
            return Error.Validation(
                "VerificationCycle.NoneOpen",
                "No verification cycle is open. Ask an administrator to start one.");
        }

        // Checked before the insert as well as after. The phone may have been
        // out of signal for a day, and being told "you already sent this" is a
        // better answer than a round trip that ends in a constraint violation.
        if (request.ClientCaptureId is { } captureId)
        {
            var already = await db.PhysicalVerifications
                .AsNoTracking()
                .SingleOrDefaultAsync(v => v.ClientCaptureId == captureId, ct);

            if (already is not null)
            {
                return Describe(already, asset, wasAlreadyRecorded: true);
            }
        }

        var invalid = ValidateCount(request, asset);
        if (invalid is not null)
        {
            return invalid;
        }

        var now = clock.UtcNow;

        var verification = new PhysicalVerification
        {
            PhysicalVerificationCycleId = cycle.Id,
            AssetId = request.AssetId,
            ClientCaptureId = request.ClientCaptureId,
            IsBulkCount = request.IsBulkCount,
            CountedQuantity = request.CountedQuantity,
            ExpectedQuantitySnapshot = request.ExpectedQuantitySnapshot,
            ScannedQrValue = request.ScannedQrValue,
            // A tag that does not name the asset it is stuck to. Recorded
            // rather than refused: the technician is standing in front of the
            // thing, and the tag being wrong is the finding.
            HasQrMismatch = HasMismatch(request.ScannedQrValue, asset.AssetNumber),
            WorkingCondition = request.WorkingCondition,
            SerialVerified = request.SerialVerified,
            GpsLatitude = request.GpsLatitude,
            GpsLongitude = request.GpsLongitude,
            PhotoPath = request.PhotoPath,
            // What the phone saw, falling back to what the register says. A
            // bulk line counted at a branch is counted THERE, and the register
            // has no single location for it.
            LocationId = request.LocationId ?? asset.CurrentLocationId,
            HolderEmployeeId = request.HolderEmployeeId ?? asset.CurrentEmployeeId,
            VerifiedByUserId = currentUser.Id,
            // The device's clock, not the server's: the capture happened when
            // the technician was standing in front of the asset.
            VerifiedOnUtc = request.VerifiedOnUtc ?? now,
            Remarks = request.Remarks,
            CreatedOnUtc = now,
            CreatedBy = currentUser.Username,
        };

        db.PhysicalVerifications.Add(verification);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            // Two phones sending the same capture at once get here rather than
            // through the check above. The translator names the index, which is
            // what tells the two cases apart.
            var error = sqlErrors.Translate(sql.Number, sql.Message);

            if (error?.Code == "Verification.AlreadyCaptured"
                && request.ClientCaptureId is { } raced)
            {
                var existing = await db.PhysicalVerifications
                    .AsNoTracking()
                    .SingleOrDefaultAsync(v => v.ClientCaptureId == raced, ct);

                if (existing is not null)
                {
                    return Describe(existing, asset, wasAlreadyRecorded: true);
                }
            }

            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return Describe(verification, asset, wasAlreadyRecorded: false);
    }

    /// <summary>
    /// The rules a count obeys that a sighting does not.
    /// </summary>
    /// <remarks>
    /// <c>CK_PhysicalVerification_BulkHasCount</c> says the first of these as a
    /// 500. The rest are things the schema cannot see: a bulk line counted
    /// nowhere would collide with every other count of the same line, because
    /// the uniqueness rule for a count is per PLACE.
    /// </remarks>
    private static Error? ValidateCount(SubmitVerificationCommand request, AssetSnapshot asset)
    {
        if (!request.IsBulkCount)
        {
            return request.CountedQuantity is not null
                ? Error.Validation(
                    "Verification.CountOnSighting",
                    "A quantity belongs to a bulk count, not to a sighting.")
                : null;
        }

        if (request.CountedQuantity is null)
        {
            return Error.Validation(
                "Verification.CountRequired",
                "A bulk count without a number is not a count.");
        }

        if (request.LocationId is null && asset.CurrentLocationId is null)
        {
            // UX_PhysicalVerification_OneBulkCountPerPlacePerCycle is on
            // (cycle, asset, location). With no location, counting the same
            // line at four branches would look like one place counted four
            // times.
            return Error.Validation(
                "Verification.CountNeedsPlace",
                "A bulk count has to say where it was counted.");
        }

        return !asset.IsBulk
            ? Error.Validation(
                "Verification.NotBulk",
                $"{asset.AssetNumber} is a single asset. Sight it rather than counting it.")
            : null;
    }

    /// <summary>
    /// Whether the scanned tag belongs to a different asset.
    /// </summary>
    /// <remarks>
    /// A tag is compared case-insensitively and with surrounding whitespace
    /// ignored, because a QR reader returns what is printed and printers add
    /// neither. Anything else is a mismatch worth recording.
    /// </remarks>
    private static bool HasMismatch(string? scanned, string assetNumber) =>
        !string.IsNullOrWhiteSpace(scanned)
        && !string.Equals(scanned.Trim(), assetNumber.Trim(), StringComparison.OrdinalIgnoreCase);

    private static SubmitVerificationResponse Describe(
        PhysicalVerification verification,
        AssetSnapshot asset,
        bool wasAlreadyRecorded) =>
        new(
            verification.Id,
            verification.AssetId,
            asset.AssetNumber,
            verification.WorkingCondition,
            verification.HasQrMismatch,
            verification.CountedQuantity is { } counted
                && verification.ExpectedQuantitySnapshot is { } expected
                    ? counted - expected
                    : null,
            wasAlreadyRecorded);
}
