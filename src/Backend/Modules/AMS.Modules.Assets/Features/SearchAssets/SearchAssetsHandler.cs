using AMS.Modules.Assets.Persistence;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AMS.Modules.Assets.Features.SearchAssets;

/// <summary>The register grid. Catalogue screen: Asset Register.</summary>
/// <remarks>
/// <para>
/// Paged at the database. The live register is 7,413 rows and every one of them
/// is now in scope — Revision 3 made this the register for furniture, factory
/// equipment and vehicles as well as IT — so an unbounded list is not an option
/// (02 §8).
/// </para>
/// <para>
/// Branch scoping IS applied here, from <c>ICurrentUser</c>, because that is
/// where <c>ICurrentUser</c> says it belongs: "per request inside query
/// handlers, never as a global EF query filter". A model-level filter reading
/// request state behaves differently in the background jobs, where there is no
/// caller at all.
/// </para>
/// <para>
/// A branch administrator sees their own branches, and also every asset that
/// belongs to no branch — bulk lines have no single location, and an asset in
/// transit belongs to neither end. Hiding those would make stock invisible to
/// the only people who could act on it.
/// </para>
/// </remarks>
public sealed class SearchAssetsHandler(
    AssetsDbContext db,
    ICurrentUser currentUser,
    IBranchDirectory? branches = null)
    : IRequestHandler<SearchAssetsQuery, SearchAssetsResponse>
{
    public async Task<Result<SearchAssetsResponse>> HandleAsync(
        SearchAssetsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.Assets.AsNoTracking();

        // Deleted assets are hidden unless asked for. They are never physically
        // removed, because history points at them.
        if (!request.IncludeDeleted)
        {
            query = query.Where(a => !a.IsDeleted);
        }

        if (!currentUser.HasAllBranches)
        {
            var branches = currentUser.BranchIds;
            query = query.Where(a => a.CurrentLocationId == null
                                  || branches.Contains(a.CurrentLocationId.Value));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = $"%{request.Search}%";
            query = query.Where(a => EF.Functions.Like(a.AssetNumber, term)
                                  || EF.Functions.Like(a.AssetName, term)
                                  || (a.SerialNumber != null && EF.Functions.Like(a.SerialNumber, term))
                                  || (a.Make != null && EF.Functions.Like(a.Make, term))
                                  || (a.Model != null && EF.Functions.Like(a.Model, term)));
        }

        if (request.AssetTypeId.HasValue)
        {
            query = query.Where(a => a.AssetTypeId == request.AssetTypeId.Value);
        }

        if (request.AssetClassId.HasValue)
        {
            query = query.Where(a => a.AssetClassId == request.AssetClassId.Value);
        }

        if (request.AssetStatusId.HasValue)
        {
            query = query.Where(a => a.AssetStatusId == request.AssetStatusId.Value);
        }

        if (request.LocationId.HasValue)
        {
            var branchId = request.LocationId.Value;
            var selectedBranches = branches is null
                ? []
                : await branches.FindActiveAsync([branchId], ct);
            var aliases = selectedBranches
                .SelectMany(branch => new[] { branch.BranchCode, branch.BranchName })
                .Select(NormalizeBranch)
                .Where(value => value.Length > 0)
                .ToHashSet(StringComparer.Ordinal);

            var candidates = await db.Assets.AsNoTracking()
                .Where(asset => !asset.IsDeleted)
                .Select(asset => new
                {
                    asset.Id,
                    asset.CurrentLocationId,
                    asset.ImportedDataJson,
                    CustomBranch = db.AssetCustomValues
                        .Where(value => value.AssetId == asset.Id &&
                            db.CustomFieldDefinitions.Any(definition =>
                                definition.Id == value.CustomFieldDefinitionId &&
                                definition.IsActive &&
                                (definition.FieldName == "Branch" ||
                                 definition.DisplayLabel == "Branch")))
                        .Select(value => value.Value)
                        .FirstOrDefault(),
                })
                .ToListAsync(ct);

            var matchingAssetIds = candidates
                .Where(asset => asset.CurrentLocationId is { } currentLocationId
                    ? currentLocationId == branchId
                    : aliases.Contains(NormalizeBranch(
                        asset.CustomBranch ?? ImportedValue(asset.ImportedDataJson, "Branch"))))
                .Select(asset => asset.Id)
                .ToArray();
            query = query.Where(asset => matchingAssetIds.Contains(asset.Id));
        }

        if (request.EmployeeId.HasValue)
        {
            query = query.Where(a => a.CurrentEmployeeId == request.EmployeeId.Value);
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(a => a.DepartmentId == request.DepartmentId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.CostCenter))
        {
            query = query.Where(a => a.CostCenter != null && EF.Functions.Like(a.CostCenter, $"%{request.CostCenter}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.SapAssetNumber))
        {
            query = query.Where(a => a.SapAssetNumber != null && EF.Functions.Like(a.SapAssetNumber, $"%{request.SapAssetNumber}%"));
        }

        if (!string.IsNullOrWhiteSpace(request.SapPlant))
        {
            query = query.Where(a => a.SapPlant != null && EF.Functions.Like(a.SapPlant, $"%{request.SapPlant}%"));
        }

        if (request.AcquiredFrom.HasValue)
        {
            query = query.Where(a => a.AcquisitionDate >= request.AcquiredFrom.Value);
        }

        if (request.AcquiredTo.HasValue)
        {
            query = query.Where(a => a.AcquisitionDate <= request.AcquiredTo.Value);
        }

        if (request.IsBulk.HasValue)
        {
            query = query.Where(a => a.IsBulk == request.IsBulk.Value);
        }

        if (request.IsVerified.HasValue)
        {
            var verifiedAssetIds = await db.Database.SqlQueryRaw<int>(
                    "SELECT DISTINCT [AssetId] AS [Value] FROM [Verification].[PhysicalVerification]")
                .ToListAsync(ct);
            query = request.IsVerified.Value
                ? query.Where(asset => verifiedAssetIds.Contains(asset.Id))
                : query.Where(asset => !verifiedAssetIds.Contains(asset.Id));
        }

        var total = await query.CountAsync(ct);

        var rows = await query
            .OrderBy(a => a.AssetNumber)
            .Skip(request.Skip)
            .Take(request.Take)
            .Select(a => new SearchAssetsResponse.Row(
                a.Id,
                a.AssetNumber,
                a.AssetName,
                a.SerialNumber,
                db.AssetTypes.Where(t => t.Id == a.AssetTypeId).Select(t => t.TypeName).First(),
                db.AssetClasses.Where(c => c.Id == a.AssetClassId).Select(c => c.ClassName).FirstOrDefault(),
                db.AssetStatuses.Where(s => s.Id == a.AssetStatusId).Select(s => s.StatusName).First(),
                a.Make,
                a.Model,
                a.CurrentLocationId,
                a.CurrentEmployeeId,
                a.DepartmentId,
                a.CostCenter,
                a.QrCodeValue,
                a.BarcodeValue,
                a.ErpAssetNumber,
                a.SapAssetNumber,
                a.SapPlant,
                a.LastPhysicalCheckOnUtc,
                a.Remarks,
                a.ImportedDataJson,
                a.IsBulk,
                a.Quantity,
                a.UnitOfMeasure,
                a.AcquisitionDate,
                a.IsDeleted))
            .ToListAsync(ct);

        return new SearchAssetsResponse(rows, total);
    }

    private static string? ImportedValue(string? importedDataJson, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(importedDataJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(importedDataJson);
            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (string.Equals(property.Name, fieldName, StringComparison.OrdinalIgnoreCase))
                {
                    return property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString()
                        : property.Value.ToString();
                }
            }
        }
        catch (JsonException)
        {
            // Malformed legacy import data has no usable Branch value.
        }

        return null;
    }

    private static string NormalizeBranch(string? value) => string.Concat(
        (value ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();
}
