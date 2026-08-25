using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

using AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;

namespace AMS.Modules.ServiceDesk.Features.UpdateServiceTemplate;

/// <summary>Edit a template or retire it.</summary>
/// <remarks>
/// <c>RequestKind</c> is not editable. It decides which screen the template
/// appears on and whether an approval workflow applies, so changing it would
/// move a template between two different things a user was choosing between.
/// Retire it and make another.
/// </remarks>
public sealed class UpdateServiceTemplateHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<UpdateServiceTemplateCommand, UpdateServiceTemplateResponse>
{
    public async Task<Result<UpdateServiceTemplateResponse>> HandleAsync(
        UpdateServiceTemplateCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var template = await db.ServiceTemplates.SingleOrDefaultAsync(t => t.Id == request.Id, ct);
        if (template is null)
        {
            return Error.NotFound("ServiceTemplate", request.Id);
        }

        if (!RequestPriority.All.Contains(request.DefaultPriority, StringComparer.Ordinal))
        {
            return Error.Validation(
                "ServiceTemplate.UnknownPriority",
                $"Priority must be one of {string.Join(", ", RequestPriority.All)}.");
        }

        var invalid = await CreateServiceTemplateHandler.ValidateReferencesAsync(
            db, template.RequestKind, request.RequestCategoryId, request.RequestSubCategoryId,
            request.DefaultSupportTeamId, ct);
        if (invalid is not null)
        {
            return invalid;
        }

        template.TemplateName = request.TemplateName;
        template.RequestCategoryId = request.RequestCategoryId;
        template.RequestSubCategoryId = request.RequestSubCategoryId;
        template.DefaultPriority = request.DefaultPriority;
        template.DefaultSupportTeamId = request.DefaultSupportTeamId;
        template.SubjectTemplate = request.SubjectTemplate;
        template.DescriptionTemplate = request.DescriptionTemplate;
        template.RequiresAsset = request.RequiresAsset;
        template.DisplayOrder = request.DisplayOrder;
        template.IsActive = request.IsActive;
        template.ModifiedOnUtc = clock.UtcNow;
        template.ModifiedBy = currentUser.Username;

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

        return new UpdateServiceTemplateResponse(
            template.Id, template.TemplateName, template.IsActive);
    }
}
