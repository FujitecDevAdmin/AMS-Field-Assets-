using AMS.Modules.ServiceDesk.Domain;
using AMS.Modules.ServiceDesk.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceDesk.Features.CreateServiceTemplate;

/// <summary>
/// Add a template. Catalogue: pre-written requests with a default category,
/// priority and team.
/// </summary>
public sealed class CreateServiceTemplateHandler(
    ServiceDeskDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<CreateServiceTemplateCommand, CreateServiceTemplateResponse>
{
    public async Task<Result<CreateServiceTemplateResponse>> HandleAsync(
        CreateServiceTemplateCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!RequestKind.All.Contains(request.RequestKind, StringComparer.Ordinal))
        {
            return Error.Validation(
                "ServiceTemplate.UnknownKind",
                $"Request kind must be one of {string.Join(", ", RequestKind.All)}.");
        }

        if (!RequestPriority.All.Contains(request.DefaultPriority, StringComparer.Ordinal))
        {
            return Error.Validation(
                "ServiceTemplate.UnknownPriority",
                $"Priority must be one of {string.Join(", ", RequestPriority.All)}.");
        }

        var invalid = await ValidateReferencesAsync(
            db, request.RequestKind, request.RequestCategoryId, request.RequestSubCategoryId,
            request.DefaultSupportTeamId, ct);
        if (invalid is not null)
        {
            return invalid;
        }

        var template = new ServiceTemplate
        {
            TemplateName = request.TemplateName,
            RequestKind = request.RequestKind,
            RequestCategoryId = request.RequestCategoryId,
            RequestSubCategoryId = request.RequestSubCategoryId,
            DefaultPriority = request.DefaultPriority,
            DefaultSupportTeamId = request.DefaultSupportTeamId,
            SubjectTemplate = request.SubjectTemplate,
            DescriptionTemplate = request.DescriptionTemplate,
            RequiresAsset = request.RequiresAsset,
            DisplayOrder = request.DisplayOrder,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.ServiceTemplates.Add(template);

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

        return new CreateServiceTemplateResponse(template.Id, template.TemplateName);
    }

    /// <summary>
    /// Checks the three optional references, and that the sub-category belongs
    /// to the category.
    /// </summary>
    /// <remarks>
    /// Shared with UpdateServiceTemplate. A template that pre-fills a
    /// sub-category from a DIFFERENT category would produce a ticket
    /// classified two ways at once, and nothing in the schema forbids it: the
    /// two columns are independent FKs.
    /// </remarks>
    internal static async Task<Error?> ValidateReferencesAsync(
        ServiceDeskDbContext db,
        string requestKind,
        int? categoryId,
        int? subCategoryId,
        int? teamId,
        CancellationToken ct)
    {
        if (categoryId is { } category)
        {
            var categoryRow = await db.RequestCategories
                .Where(c => c.Id == category)
                .Select(c => new { c.CategoryType, c.IsActive })
                .SingleOrDefaultAsync(ct);

            if (categoryRow is null)
            {
                return Error.NotFound("RequestCategory", category);
            }

            if (!categoryRow.IsActive)
            {
                return Error.Validation("RequestCategory.Retired", "That category is retired.");
            }

            if (categoryRow.CategoryType != RequestCategoryType.ForRequestKind(requestKind))
            {
                return Error.Validation(
                    "ServiceTemplate.CategoryTypeMismatch",
                    "The category type does not match the template request kind.");
            }
        }

        if (teamId is { } team && !await db.SupportTeams.AnyAsync(t => t.Id == team, ct))
        {
            return Error.NotFound("SupportTeam", team);
        }

        if (subCategoryId is not { } subCategory)
        {
            return null;
        }

        var subCategoryRow = await db.RequestSubCategories
            .Where(s => s.Id == subCategory)
            .Select(s => new { s.RequestCategoryId, s.IsActive })
            .SingleOrDefaultAsync(ct);

        if (subCategoryRow is null)
        {
            return Error.NotFound("RequestSubCategory", subCategory);
        }

        if (categoryId is not null && subCategoryRow.RequestCategoryId != categoryId)
        {
            return Error.Validation(
                "ServiceTemplate.SubCategoryMismatch",
                "That sub-category belongs to a different category.");
        }

        if (!subCategoryRow.IsActive)
        {
            return Error.Validation("RequestSubCategory.Retired", "That sub-category is retired.");
        }

        return null;
    }
}
