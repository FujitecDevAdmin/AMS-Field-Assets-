using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.GetAssetTypeCustomFields;

/// <summary>
/// The custom fields defined for one asset type, with their dropdown options.
/// Catalogue: Define custom fields.
/// </summary>
/// <remarks>
/// One round trip, options included. The asset form cannot render a Dropdown
/// without its values, so returning the definitions and then making the client
/// fetch options per field would turn one screen into N+1 requests.
/// </remarks>
public sealed class GetAssetTypeCustomFieldsHandler(AssetsDbContext db)
    : IRequestHandler<GetAssetTypeCustomFieldsQuery, GetAssetTypeCustomFieldsResponse>
{
    public async Task<Result<GetAssetTypeCustomFieldsResponse>> HandleAsync(
        GetAssetTypeCustomFieldsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.AssetTypes.AnyAsync(t => t.Id == request.AssetTypeId, ct))
        {
            return Error.NotFound("AssetType", request.AssetTypeId);
        }

        var definitions = db.CustomFieldDefinitions
            .AsNoTracking()
            .Where(f => f.AssetTypeId == request.AssetTypeId);

        if (!request.IncludeInactive)
        {
            definitions = definitions.Where(f => f.IsActive);
        }

        var rows = await definitions
            .OrderBy(f => f.DisplayOrder)
            .ThenBy(f => f.FieldName)
            .Select(f => new GetAssetTypeCustomFieldsResponse.Row(
                f.Id,
                f.FieldName,
                f.DisplayLabel,
                f.FieldType,
                f.IsRequired,
                f.MinValue,
                f.MaxValue,
                f.ValidationRegex,
                f.DefaultValue,
                f.DisplayOrder,
                f.IsActive,
                db.CustomFieldOptions
                    .Where(o => o.CustomFieldDefinitionId == f.Id && o.IsActive)
                    .OrderBy(o => o.DisplayOrder)
                    .ThenBy(o => o.OptionValue)
                    .Select(o => o.OptionValue)
                    .ToList()))
            .ToListAsync(ct);

        return new GetAssetTypeCustomFieldsResponse(request.AssetTypeId, rows);
    }
}
