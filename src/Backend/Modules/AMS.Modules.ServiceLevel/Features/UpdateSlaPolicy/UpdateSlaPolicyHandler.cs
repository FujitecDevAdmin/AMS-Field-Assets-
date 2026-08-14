using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

using AMS.Modules.ServiceLevel.Features.CreateSlaPolicy;

namespace AMS.Modules.ServiceLevel.Features.UpdateSlaPolicy;

/// <summary>Edit an SLA policy or retire it. Catalogue: SLA Policy Setup.</summary>
/// <remarks>
/// <para>
/// The priority is not editable. Moving a policy from High to Critical would
/// change which tickets it judges without changing anything anybody can see on
/// the ticket, and every SLA report spanning the change would be measuring two
/// different things under one name. Retire it and add another.
/// </para>
/// <para>
/// Editing the targets IS allowed, and the table is system-versioned so what
/// the policy said on any past date is still readable:
/// <c>FOR SYSTEM_TIME AS OF</c>. That is why the history table exists — an SLA
/// report for last quarter has to be able to use last quarter's targets.
/// </para>
/// </remarks>
public sealed class UpdateSlaPolicyHandler(
    ServiceLevelDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<UpdateSlaPolicyCommand, UpdateSlaPolicyResponse>
{
    public async Task<Result<UpdateSlaPolicyResponse>> HandleAsync(
        UpdateSlaPolicyCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var policy = await db.SlaPolicies.SingleOrDefaultAsync(p => p.Id == request.Id, ct);
        if (policy is null)
        {
            return Error.NotFound("SlaPolicy", request.Id);
        }

        var invalid = SlaPolicyRules.Validate(
            policy.Priority, request.ResponseTargetMinutes, request.ResolutionTargetMinutes);

        if (invalid is not null)
        {
            return invalid;
        }

        policy.PolicyName = request.PolicyName;
        policy.Description = request.Description;
        policy.ResponseTargetMinutes = request.ResponseTargetMinutes;
        policy.ResolutionTargetMinutes = request.ResolutionTargetMinutes;
        policy.RespectOperationalHours = request.RespectOperationalHours;
        policy.RespectHolidays = request.RespectHolidays;
        policy.RespectWeekends = request.RespectWeekends;
        policy.NearDueWarningMinutes = request.NearDueWarningMinutes;
        policy.IsActive = request.IsActive;
        policy.ModifiedOnUtc = clock.UtcNow;
        policy.ModifiedBy = currentUser.Username;

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

        return new UpdateSlaPolicyResponse(policy.Id, policy.PolicyName, policy.IsActive);
    }
}
