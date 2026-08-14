using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.DefineCustomField;

/// <summary>
/// Add a custom field to an asset type. Catalogue: Define custom fields — type,
/// required flag, range and dropdown options.
/// </summary>
public sealed class DefineCustomFieldHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<DefineCustomFieldCommand, DefineCustomFieldResponse>
{
    /// <summary>
    /// The six types <c>CK_CustomFieldDefinition_Type</c> allows (R2-26).
    /// </summary>
    /// <remarks>
    /// Checked here as well as in the database so a mistyped type comes back as
    /// a message beside the field rather than a 500 the user cannot act on.
    /// The CHECK is what makes it true; this is what makes it kind.
    /// </remarks>
    private static readonly string[] FieldTypes =
        ["Text", "Number", "Percentage", "Date", "Boolean", "Dropdown"];

    public async Task<Result<DefineCustomFieldResponse>> HandleAsync(
        DefineCustomFieldCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.AssetTypes.AnyAsync(t => t.Id == request.AssetTypeId, ct))
        {
            return Error.NotFound("AssetType", request.AssetTypeId);
        }

        if (!FieldTypes.Contains(request.FieldType, StringComparer.Ordinal))
        {
            return Error.Validation(
                "CustomField.UnknownType",
                $"Field type must be one of {string.Join(", ", FieldTypes)}.");
        }

        var isDropdown = string.Equals(request.FieldType, "Dropdown", StringComparison.Ordinal);
        var options = request.Options
            .Select(o => o.Trim())
            .Where(o => o.Length > 0)
            .ToList();

        // A Dropdown with no values renders as an empty picker the user cannot
        // satisfy - and if the field is also required, an asset of this type
        // can never be saved at all.
        if (isDropdown && options.Count == 0)
        {
            return Error.Validation(
                "CustomField.DropdownNeedsOptions",
                "A Dropdown field needs at least one option.");
        }

        if (!isDropdown && options.Count > 0)
        {
            return Error.Validation(
                "CustomField.OptionsNotAllowed",
                $"Options belong to a Dropdown field, not a {request.FieldType} one.");
        }

        if (options.Distinct(StringComparer.OrdinalIgnoreCase).Count() != options.Count)
        {
            return Error.Validation(
                "CustomField.DuplicateOption", "Two dropdown options cannot have the same value.");
        }

        var definition = new CustomFieldDefinition
        {
            AssetTypeId = request.AssetTypeId,
            FieldName = request.FieldName,
            DisplayLabel = request.DisplayLabel,
            FieldType = request.FieldType,
            IsRequired = request.IsRequired,
            MinValue = request.MinValue,
            MaxValue = request.MaxValue,
            ValidationRegex = request.ValidationRegex,
            DefaultValue = request.DefaultValue,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.CustomFieldDefinitions.Add(definition);

        // Two saves, one transaction. There is no navigation property from
        // option to definition — 03 §2 keeps entities persistence-faithful and
        // the design script has no such column — so EF cannot order the inserts
        // itself and the options need an Id that only the first save produces.
        //
        // The transaction is what makes that safe: a Dropdown that exists
        // without the values it promised is a field no asset can be saved
        // against, and it would survive every retry.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        try
        {
            await db.SaveChangesAsync(ct);

            for (var i = 0; i < options.Count; i++)
            {
                db.CustomFieldOptions.Add(new CustomFieldOption
                {
                    CustomFieldDefinitionId = definition.Id,
                    OptionValue = options[i],
                    DisplayOrder = i,
                    IsActive = true,
                    CreatedOnUtc = clock.UtcNow,
                    CreatedBy = currentUser.Username,
                });
            }

            if (options.Count > 0)
            {
                await db.SaveChangesAsync(ct);
            }

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            await transaction.RollbackAsync(ct);

            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new DefineCustomFieldResponse(
            definition.Id, definition.FieldName, definition.FieldType, options);
    }
}
