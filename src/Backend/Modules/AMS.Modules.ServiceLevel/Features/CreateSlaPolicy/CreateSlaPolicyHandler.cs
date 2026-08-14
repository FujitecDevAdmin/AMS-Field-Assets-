using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;

/// <summary>Add an SLA policy. Catalogue: SLA Policy Setup.</summary>
/// <remarks>
/// Created active, which means it immediately claims its priority. That is the
/// point of the screen — a policy nobody has switched on judges nothing — and
/// UX_SlaPolicy_ActivePriority makes a second live policy for the same priority
/// a 409 rather than a coin toss.
/// </remarks>
public sealed class CreateSlaPolicyHandler(
    ServiceLevelDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<CreateSlaPolicyCommand, CreateSlaPolicyResponse>
{
    public async Task<Result<CreateSlaPolicyResponse>> HandleAsync(
        CreateSlaPolicyCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var invalid = SlaPolicyRules.Validate(
            request.Priority, request.ResponseTargetMinutes, request.ResolutionTargetMinutes);

        if (invalid is not null)
        {
            return invalid;
        }

        var policy = new SlaPolicy
        {
            PolicyName = request.PolicyName,
            Description = request.Description,
            Priority = request.Priority,
            ResponseTargetMinutes = request.ResponseTargetMinutes,
            ResolutionTargetMinutes = request.ResolutionTargetMinutes,
            RespectOperationalHours = request.RespectOperationalHours,
            RespectHolidays = request.RespectHolidays,
            RespectWeekends = request.RespectWeekends,
            NearDueWarningMinutes = request.NearDueWarningMinutes,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.SlaPolicies.Add(policy);

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

        return new CreateSlaPolicyResponse(policy.Id, policy.PolicyName, policy.Priority);
    }
}

/// <summary>Rules the create and update slices share.</summary>
public static class SlaPolicyRules
{
    /// <summary>Everything the CHECK constraints would reject, refused by name.</summary>
    public static Error? Validate(string priority, int responseMinutes, int resolutionMinutes)
    {
        if (!SlaPriority.Allowed.Contains(priority, StringComparer.Ordinal))
        {
            return Error.Validation(
                "SlaPolicy.UnknownPriority",
                $"Priority must be one of {string.Join(", ", SlaPriority.Allowed)}.");
        }

        if (responseMinutes <= 0 || resolutionMinutes <= 0)
        {
            return Error.Validation(
                "SlaPolicy.Targets",
                "Both targets must be more than zero minutes.");
        }

        // A response target longer than the resolution target is always a typo,
        // and it silently makes every ticket look compliant.
        return responseMinutes > resolutionMinutes
            ? Error.Validation(
                "SlaPolicy.ResponseBeyondResolution",
                "The response target cannot be longer than the resolution target.")
            : null;
    }
}
