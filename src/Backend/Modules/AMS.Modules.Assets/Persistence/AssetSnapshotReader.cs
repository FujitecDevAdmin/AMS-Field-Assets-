using AMS.Modules.Assets.PublicApi;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Persistence;

/// <summary>
/// Reads the custody facts other modules ask for. See
/// <see cref="IAssetSnapshot"/> for why this exists.
/// </summary>
public sealed class AssetSnapshotReader(AssetsDbContext db) : IAssetSnapshot
{
    public async Task RecordPhysicalCheckAsync(
        int assetId,
        DateTime verifiedOnUtc,
        CancellationToken ct)
    {
        await db.Assets
            .Where(asset => asset.Id == assetId && !asset.IsDeleted
                && (asset.LastPhysicalCheckOnUtc == null
                    || asset.LastPhysicalCheckOnUtc < verifiedOnUtc))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(asset => asset.LastPhysicalCheckOnUtc, verifiedOnUtc), ct);
    }

    public async Task<AssetSnapshot?> GetAsync(int assetId, CancellationToken ct)
    {
        var asset = await db.Assets
            .AsNoTracking()
            .Where(a => a.Id == assetId && !a.IsDeleted)
            .Select(a => new
            {
                a.Id,
                a.AssetNumber,
                a.CurrentEmployeeId,
                a.CurrentLocationId,
                a.DepartmentId,
                a.CostCenter,
                a.IsBulk,
                a.AssetName,
                a.SerialNumber,
                a.Quantity,
                a.QrCodeValue,
                a.BarcodeValue,
                a.ImportedDataJson,
            })
            .SingleOrDefaultAsync(ct);

        return asset is null ? null : new AssetSnapshot(
            asset.Id, asset.AssetNumber, asset.CurrentEmployeeId, asset.CurrentLocationId,
            asset.DepartmentId, asset.CostCenter, asset.IsBulk, asset.AssetName,
            asset.SerialNumber, asset.Quantity,
            ImportedValue(asset.ImportedDataJson, "Branch"),
            ImportedValue(asset.ImportedDataJson, "Location"),
            asset.QrCodeValue, asset.BarcodeValue);
    }

    public async Task<IReadOnlyList<AssetSnapshot>> GetManyAsync(
        IReadOnlyCollection<int> assetIds,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(assetIds);
        if (assetIds.Count == 0)
        {
            return [];
        }

        var rows = await db.Assets.AsNoTracking()
            .Where(asset => assetIds.Contains(asset.Id) && !asset.IsDeleted)
            .Select(asset => new
            {
                asset.Id,
                asset.AssetNumber,
                asset.CurrentEmployeeId,
                asset.CurrentLocationId,
                asset.DepartmentId,
                asset.CostCenter,
                asset.IsBulk,
                asset.AssetName,
                asset.SerialNumber,
                asset.Quantity,
                asset.QrCodeValue,
                asset.BarcodeValue,
                asset.ImportedDataJson,
            })
            .ToListAsync(ct);

        return rows.Select(asset => new AssetSnapshot(
            asset.Id, asset.AssetNumber, asset.CurrentEmployeeId, asset.CurrentLocationId,
            asset.DepartmentId, asset.CostCenter, asset.IsBulk, asset.AssetName,
            asset.SerialNumber, asset.Quantity,
            ImportedValue(asset.ImportedDataJson, "Branch"),
            ImportedValue(asset.ImportedDataJson, "Location"),
            asset.QrCodeValue, asset.BarcodeValue)).ToArray();
    }

    public async Task<AssetSnapshot?> FindByScanCodeAsync(string scanCode, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(scanCode);

        var candidates = await db.Assets.AsNoTracking()
            .Where(asset => !asset.IsDeleted)
            .Select(asset => new
            {
                asset.Id,
                asset.AssetNumber,
                asset.CurrentEmployeeId,
                asset.CurrentLocationId,
                asset.DepartmentId,
                asset.CostCenter,
                asset.IsBulk,
                asset.AssetName,
                asset.SerialNumber,
                asset.Quantity,
                asset.QrCodeValue,
                asset.BarcodeValue,
                asset.ImportedDataJson,
            })
            .ToListAsync(ct);

        var match = candidates.FirstOrDefault(asset =>
            ScanMatches(scanCode, asset.AssetNumber)
            || ScanMatches(scanCode, asset.QrCodeValue)
            || ScanMatches(scanCode, asset.BarcodeValue));

        return match is null ? null : new AssetSnapshot(
            match.Id, match.AssetNumber, match.CurrentEmployeeId, match.CurrentLocationId,
            match.DepartmentId, match.CostCenter, match.IsBulk, match.AssetName,
            match.SerialNumber, match.Quantity,
            ImportedValue(match.ImportedDataJson, "Branch"),
            ImportedValue(match.ImportedDataJson, "Location"),
            match.QrCodeValue, match.BarcodeValue);
    }

    public async Task<int> CountByImportedBranchesAsync(
        IReadOnlyCollection<int> branchIds,
        IReadOnlyCollection<string> branchAliases,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(branchIds);
        ArgumentNullException.ThrowIfNull(branchAliases);
        var acceptedIds = branchIds.ToHashSet();

        var accepted = branchAliases.Select(NormalizeValue)
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        if (acceptedIds.Count == 0 && accepted.Count == 0)
        {
            return 0;
        }

        var candidates = await db.Assets.AsNoTracking()
            .Where(asset => !asset.IsDeleted)
            .Select(asset => new
            {
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

        return candidates.Count(candidate => candidate.CurrentLocationId is { } currentLocationId
            ? acceptedIds.Contains(currentLocationId)
            : accepted.Contains(NormalizeValue(
                candidate.CustomBranch ?? ImportedValue(candidate.ImportedDataJson, "Branch"))));
    }

    public async Task<IReadOnlyList<AssetSnapshot>> ListByImportedBranchesAsync(
        IReadOnlyCollection<int> branchIds,
        IReadOnlyCollection<string> branchAliases,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(branchIds);
        ArgumentNullException.ThrowIfNull(branchAliases);
        var acceptedIds = branchIds.ToHashSet();
        var accepted = branchAliases.Select(NormalizeValue)
            .Where(value => value.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        if (acceptedIds.Count == 0 && accepted.Count == 0)
        {
            return [];
        }

        var assets = await db.Assets.AsNoTracking()
            .Where(asset => !asset.IsDeleted)
            .OrderBy(asset => asset.AssetNumber)
            .Select(asset => new
            {
                asset.Id,
                asset.AssetNumber,
                asset.CurrentEmployeeId,
                asset.CurrentLocationId,
                asset.DepartmentId,
                asset.CostCenter,
                asset.IsBulk,
                asset.AssetName,
                asset.SerialNumber,
                asset.Quantity,
                asset.QrCodeValue,
                asset.BarcodeValue,
                asset.ImportedDataJson,
            })
            .ToListAsync(ct);

        return assets
            .Select(asset => new AssetSnapshot(
                asset.Id, asset.AssetNumber, asset.CurrentEmployeeId, asset.CurrentLocationId,
                asset.DepartmentId, asset.CostCenter, asset.IsBulk, asset.AssetName,
                asset.SerialNumber, asset.Quantity,
                ImportedValue(asset.ImportedDataJson, "Branch"),
                ImportedValue(asset.ImportedDataJson, "Location"),
                asset.QrCodeValue, asset.BarcodeValue))
            .Where(asset => asset.CurrentLocationId is { } currentLocationId
                ? acceptedIds.Contains(currentLocationId)
                : accepted.Contains(NormalizeValue(asset.ImportedBranch)))
            .ToArray();
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
            // A malformed legacy payload has no usable imported location.
        }

        return null;
    }

    private static string NormalizeValue(string? value) => string.Concat(
        (value ?? string.Empty).Where(char.IsLetterOrDigit)).ToUpperInvariant();

    private static bool ScanMatches(string scannedValue, string? registeredValue)
    {
        var scanned = NormalizeValue(scannedValue);
        var registered = NormalizeValue(registeredValue);
        return registered.Length > 0
            && (scanned == registered
                || (registered.Length >= 4 && scanned.Contains(registered, StringComparison.Ordinal)));
    }
}
