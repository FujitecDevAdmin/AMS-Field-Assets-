using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.SetAssetCustomValues;

/// <summary>
/// Fill in the custom fields defined for this asset's type. Catalogue: Fill
/// custom fields.
/// </summary>
/// <remarks>
/// The whole set at once. A required field left blank has to fail the save, and
/// that is not a judgement any single-field endpoint could make.
/// </remarks>
public sealed class SetAssetCustomValuesHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SetAssetCustomValuesCommand, SetAssetCustomValuesResponse>
{
    public async Task<Result<SetAssetCustomValuesResponse>> HandleAsync(
        SetAssetCustomValuesCommand request,
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

        var definitions = await db.CustomFieldDefinitions
            .AsNoTracking()
            .Where(f => f.AssetTypeId == asset.AssetTypeId && f.IsActive)
            .ToListAsync(ct);
        var byId = definitions.ToDictionary(f => f.Id);

        // A field from a DIFFERENT type would otherwise be stored happily: the
        // unique index is on (AssetId, CustomFieldDefinitionId) and neither
        // column knows what type the asset is.
        foreach (var entry in request.Values)
        {
            if (!byId.ContainsKey(entry.CustomFieldDefinitionId))
            {
                return Error.Validation(
                    "CustomField.NotOnThisType",
                    "One of those fields is not defined for this asset's type.");
            }
        }

        var supplied = request.Values.ToDictionary(v => v.CustomFieldDefinitionId);

        foreach (var definition in definitions)
        {
            supplied.TryGetValue(definition.Id, out var entry);
            var error = await ValidateAsync(definition, entry, ct);
            if (error is not null)
            {
                return error;
            }
        }

        var existing = await db.AssetCustomValues
            .Where(v => v.AssetId == asset.Id)
            .ToListAsync(ct);
        var existingByField = existing.ToDictionary(v => v.CustomFieldDefinitionId);

        var saved = 0;
        foreach (var entry in request.Values)
        {
            var isEmpty = string.IsNullOrWhiteSpace(entry.Value)
                          && entry.ValueNumber is null
                          && entry.ValueDate is null
                          && entry.OptionId is null;

            if (existingByField.TryGetValue(entry.CustomFieldDefinitionId, out var row))
            {
                if (isEmpty)
                {
                    // Clearing a field removes the row rather than storing four
                    // nulls, so "has a value" stays a question about existence.
                    db.AssetCustomValues.Remove(row);
                    continue;
                }
            }
            else
            {
                if (isEmpty)
                {
                    continue;
                }

                row = new AssetCustomValue
                {
                    AssetId = asset.Id,
                    CustomFieldDefinitionId = entry.CustomFieldDefinitionId,
                };
                db.AssetCustomValues.Add(row);
            }

            row.Value = string.IsNullOrWhiteSpace(entry.Value) ? null : entry.Value.Trim();
            row.ValueNumber = entry.ValueNumber;
            row.ValueDate = entry.ValueDate;
            row.OptionId = entry.OptionId;
            row.UpdatedOnUtc = clock.UtcNow;
            row.UpdatedBy = currentUser.Username;
            saved++;
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

        return new SetAssetCustomValuesResponse(asset.Id, saved);
    }

    private async Task<Error?> ValidateAsync(
        CustomFieldDefinition definition,
        SetAssetCustomValuesCommand.Entry? entry,
        CancellationToken ct)
    {
        var isEmpty = entry is null
                      || (string.IsNullOrWhiteSpace(entry.Value)
                          && entry.ValueNumber is null
                          && entry.ValueDate is null
                          && entry.OptionId is null);

        if (isEmpty)
        {
            return definition.IsRequired
                ? Error.Validation(
                    "CustomField.Required", $"'{definition.DisplayLabel}' is required.")
                : null;
        }

        switch (definition.FieldType)
        {
            case "Number":
            case "Percentage":
                if (entry!.ValueNumber is null)
                {
                    return Error.Validation(
                        "CustomField.NumberExpected", $"'{definition.DisplayLabel}' takes a number.");
                }

                if (definition.MinValue is { } min && entry.ValueNumber < min)
                {
                    return Error.Validation(
                        "CustomField.BelowMinimum",
                        $"'{definition.DisplayLabel}' cannot be less than {min}.");
                }

                if (definition.MaxValue is { } max && entry.ValueNumber > max)
                {
                    return Error.Validation(
                        "CustomField.AboveMaximum",
                        $"'{definition.DisplayLabel}' cannot be more than {max}.");
                }

                break;

            case "Date":
                if (entry!.ValueDate is null)
                {
                    return Error.Validation(
                        "CustomField.DateExpected", $"'{definition.DisplayLabel}' takes a date.");
                }

                break;

            case "Dropdown":
                if (entry!.OptionId is not { } optionId)
                {
                    return Error.Validation(
                        "CustomField.OptionExpected",
                        $"'{definition.DisplayLabel}' takes one of its listed options.");
                }

                // Checked against THIS field's options: an option id from
                // another dropdown is a valid row and the wrong answer.
                var belongs = await db.CustomFieldOptions.AnyAsync(
                    o => o.Id == optionId
                         && o.CustomFieldDefinitionId == definition.Id
                         && o.IsActive,
                    ct);
                if (!belongs)
                {
                    return Error.Validation(
                        "CustomField.UnknownOption",
                        $"That is not one of the options for '{definition.DisplayLabel}'.");
                }

                break;

            default:
                // Text and Boolean both arrive as text.
                if (string.IsNullOrWhiteSpace(entry!.Value))
                {
                    return Error.Validation(
                        "CustomField.TextExpected", $"'{definition.DisplayLabel}' takes text.");
                }

                if (definition.ValidationRegex is { } pattern
                    && !System.Text.RegularExpressions.Regex.IsMatch(
                        entry.Value.Trim(), pattern,
                        System.Text.RegularExpressions.RegexOptions.None,
                        TimeSpan.FromMilliseconds(100)))
                {
                    // Timed out rather than trusted: the pattern is typed by an
                    // administrator, and a catastrophically backtracking regex
                    // on a 7,000-row import would take the process with it.
                    return Error.Validation(
                        "CustomField.PatternMismatch",
                        $"'{definition.DisplayLabel}' is not in the expected format.");
                }

                break;
        }

        return null;
    }
}
