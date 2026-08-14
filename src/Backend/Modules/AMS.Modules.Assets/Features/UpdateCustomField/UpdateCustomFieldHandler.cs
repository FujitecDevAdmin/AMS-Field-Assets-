using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Features.UpdateCustomField;

/// <summary>Edit a custom field definition or retire it.</summary>
/// <remarks>
/// <c>FieldName</c> and <c>FieldType</c> are deliberately not editable.
/// Captured values are stored against the field, so renaming it would orphan
/// them and changing Text to Number would leave values that no longer parse.
/// Retire the field and define a new one; the old values keep their meaning.
/// </remarks>
public sealed class UpdateCustomFieldHandler(
    AssetsDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors) : IRequestHandler<UpdateCustomFieldCommand, UpdateCustomFieldResponse>
{
    public async Task<Result<UpdateCustomFieldResponse>> HandleAsync(
        UpdateCustomFieldCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var definition = await db.CustomFieldDefinitions
            .SingleOrDefaultAsync(f => f.Id == request.Id, ct);
        if (definition is null)
        {
            return Error.NotFound("CustomFieldDefinition", request.Id);
        }

        definition.DisplayLabel = request.DisplayLabel;
        definition.IsRequired = request.IsRequired;
        definition.MinValue = request.MinValue;
        definition.MaxValue = request.MaxValue;
        definition.ValidationRegex = request.ValidationRegex;
        definition.DefaultValue = request.DefaultValue;
        definition.DisplayOrder = request.DisplayOrder;
        definition.IsActive = request.IsActive;
        definition.ModifiedOnUtc = clock.UtcNow;
        definition.ModifiedBy = currentUser.Username;

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

        return new UpdateCustomFieldResponse(
            definition.Id, definition.DisplayLabel, definition.IsActive);
    }
}
