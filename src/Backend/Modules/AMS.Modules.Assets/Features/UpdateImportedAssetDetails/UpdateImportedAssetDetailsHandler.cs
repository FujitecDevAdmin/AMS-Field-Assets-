using System.Globalization;
using System.Text.Json;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.UpdateImportedAssetDetails;

public sealed class UpdateImportedAssetDetailsHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<UpdateImportedAssetDetailsCommand, UpdateImportedAssetDetailsResponse>
{
    public async Task<Result<UpdateImportedAssetDetailsResponse>> HandleAsync(
        UpdateImportedAssetDetailsCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var asset = await db.Assets.SingleOrDefaultAsync(
            item => item.Id == request.AssetId && !item.IsDeleted,
            ct);
        if (asset is null)
        {
            return Error.NotFound("Asset", request.AssetId);
        }

        var existing = Deserialize(asset.ImportedDataJson);
        foreach (var (name, value) in request.Fields)
        {
            if (!IsProtected(name))
            {
                existing[name.Trim()] = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            }
        }

        asset.ImportedDataJson = JsonSerializer.Serialize(existing);
        asset.SerialNumber = Limit(Value(existing, "ManufactureSerialNumber"), 100);
        asset.Make = Limit(Value(existing, "Make"), 100);
        asset.Model = Limit(Value(existing, "Model"), 100);
        asset.CostCenter = Limit(Value(existing, "Cost Centre"), 40);
        asset.AcquisitionDate = Date(Value(existing, "First Acquisition Date"));
        asset.ModifiedOnUtc = clock.UtcNow;
        asset.ModifiedBy = currentUser.Username;

        await db.SaveChangesAsync(ct);
        return new UpdateImportedAssetDetailsResponse(asset.Id, asset.ImportedDataJson);
    }

    private static Dictionary<string, string?> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
        try
        {
            var values = JsonSerializer.Deserialize<Dictionary<string, string?>>(json);
            return values is null
                ? new(StringComparer.OrdinalIgnoreCase)
                : new(values, StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static bool IsProtected(string name)
    {
        var normalized = name.Trim().Replace(" ", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
        return normalized is "ASSETNO" or "ASSETNUMBER" or "ASSETNAME"
            || normalized.Contains("ERP", StringComparison.Ordinal)
            || normalized.Contains("HOST", StringComparison.Ordinal);
    }

    private static string? Value(Dictionary<string, string?> values, string key) =>
        values.TryGetValue(key, out var value) ? value : null;

    private static string? Limit(string? value, int length) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim()[..Math.Min(value.Trim().Length, length)];

    private static DateOnly? Date(string? text)
    {
        if (double.TryParse(text, NumberStyles.Any, CultureInfo.InvariantCulture, out var serial))
        {
            return DateOnly.FromDateTime(DateTime.FromOADate(serial));
        }

        return DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var value)
            ? DateOnly.FromDateTime(value)
            : null;
    }
}
