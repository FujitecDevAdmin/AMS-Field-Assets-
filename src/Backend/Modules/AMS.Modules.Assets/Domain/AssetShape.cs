using AMS.SharedKernel.Results;

namespace AMS.Modules.Assets.Domain;

/// <summary>
/// The rules about what shape an asset row may take, said once.
/// </summary>
/// <remarks>
/// <para>
/// Every rule here is <b>also</b> a CHECK constraint in
/// <c>AMS_Consolidated_Design_v2.sql</c>, and the constraint is what makes it
/// true — an importer writing raw SQL is bound by the database and by nothing
/// in this file. This exists so the person filling in a form gets a sentence
/// beside the field instead of a 500 carrying SQL Server's wording.
/// </para>
/// <para>
/// It lives in Domain and not in a validator because it needs the
/// <see cref="AssetType"/> row: whether a type can be issued to a person is
/// data, and FluentValidation runs before anything has been read.
/// </para>
/// </remarks>
public static class AssetShape
{
    /// <summary>
    /// Checks one asset's bulk/quantity/custody combination.
    /// Returns null when it is allowed.
    /// </summary>
    public static Error? Validate(
        bool isBulk,
        decimal quantity,
        string? unitOfMeasure,
        int? currentLocationId,
        int? currentEmployeeId,
        AssetType type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (quantity <= 0)
        {
            // CK_Asset_QuantityPositive.
            return Error.Validation("Asset.QuantityNotPositive", "Quantity must be more than zero.");
        }

        if (!isBulk && quantity != 1m)
        {
            // CK_Asset_UnitQuantityIsOne, and the load-bearing one: it is what
            // makes "every allocatable asset has Quantity = 1" a database proof,
            // so allocation, handover and verification keep working unchanged.
            return Error.Validation(
                "Asset.UnitQuantityIsOne",
                "An asset that is not a bulk line always has a quantity of 1. "
                + "Tick 'bulk line' to record a quantity.");
        }

        if (isBulk && string.IsNullOrWhiteSpace(unitOfMeasure))
        {
            // CK_Asset_BulkHasUom. A quantity with no unit is a number nobody
            // can act on: 495 of what?
            return Error.Validation(
                "Asset.BulkNeedsUnit",
                "A bulk line needs a unit of measure — Nos, Set, Metre.");
        }

        if (isBulk && (currentLocationId is not null || currentEmployeeId is not null))
        {
            // CK_Asset_BulkNotHeld. A bulk line has no single place: its custody
            // is a set of per-place balances in AssetHolding.
            return Error.Validation(
                "Asset.BulkNotHeld",
                "A bulk line is not held at one branch or by one person. "
                + "Record where the stock sits on the Bulk Stock screen instead.");
        }

        if (isBulk && type.IsAllocatable && !type.IsBulkDefault)
        {
            // Not a database rule — the database cannot see intent. An
            // allocatable type recorded in bulk is almost always somebody
            // ticking the wrong box on a laptop, and it would then be a laptop
            // nobody can issue.
            return Error.Validation(
                "Asset.TypeIsNotBulk",
                $"'{type.TypeName}' is issued to people, so it is registered one asset per row. "
                + "Change the asset type if you meant to record a quantity.");
        }

        if (!type.IsPhysical && currentLocationId is not null)
        {
            // A licence has no branch. Giving it one makes it appear on a
            // physical verification sheet somebody has to walk around with.
            return Error.Validation(
                "Asset.NotPhysical",
                $"'{type.TypeName}' is not a physical asset, so it has no branch.");
        }

        return null;
    }
}
