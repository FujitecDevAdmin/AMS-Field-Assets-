using AMS.Modules.ServiceLevel.Domain;
using AMS.Modules.ServiceLevel.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.ServiceLevel.Features.SetSlaEscalations;

/// <summary>
/// Set a policy's escalation ladder. Catalogue: SLA Policy Setup.
/// </summary>
/// <remarks>
/// The whole ladder at once. Levels are ordered and each is defined against the
/// ones around it — level 2 firing before level 1 is meaningless — so saving
/// them one at a time would let a ladder exist that nobody would have written
/// down.
/// </remarks>
public sealed class SetSlaEscalationsHandler(
    ServiceLevelDbContext db,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<SetSlaEscalationsCommand, SetSlaEscalationsResponse>
{
    public async Task<Result<SetSlaEscalationsResponse>> HandleAsync(
        SetSlaEscalationsCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.SlaPolicies.AnyAsync(p => p.Id == request.Id, ct))
        {
            return Error.NotFound("SlaPolicy", request.Id);
        }

        var invalid = Validate(request.Levels);
        if (invalid is not null)
        {
            return invalid;
        }

        var existing = await db.SlaEscalations
            .Where(e => e.SlaPolicyId == request.Id)
            .ToListAsync(ct);

        db.SlaEscalations.RemoveRange(existing);

        // Saved before the new rows go in. UX_SlaEscalation_PolicyTypeLevel is
        // on (policy, type, level), and a delete and an insert of the same
        // level in one batch collide - EF has no reason to order them.
        await db.SaveChangesAsync(ct);

        var now = clock.UtcNow;

        foreach (var level in request.Levels)
        {
            db.SlaEscalations.Add(new SlaEscalation
            {
                SlaPolicyId = request.Id,
                EscalationType = level.EscalationType,
                Level = level.Level,
                ThresholdPercent = level.ThresholdPercent,
                RecipientType = level.RecipientType,
                // Meaningless unless the recipient is a fixed address, and
                // CK_SlaEscalation_CustomAddress only requires it for Custom.
                // Storing one anyway would leave an address nobody writes to.
                RecipientAddress = level.RecipientType == EscalationRecipient.Custom
                    ? level.RecipientAddress
                    : null,
                Channel = level.Channel,
                IsEnabled = true,
                CreatedOnUtc = now,
                CreatedBy = currentUser.Username,
            });
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

        return new SetSlaEscalationsResponse(
            request.Id,
            request.Levels.Count(l => l.EscalationType == EscalationType.Response),
            request.Levels.Count(l => l.EscalationType == EscalationType.Resolution));
    }

    private static Error? Validate(IReadOnlyList<SetSlaEscalationsCommand.Rung> levels)
    {
        var seen = new HashSet<(string, int)>();

        foreach (var level in levels)
        {
            if (!EscalationType.Allowed.Contains(level.EscalationType, StringComparer.Ordinal))
            {
                return Error.Validation(
                    "SlaEscalation.UnknownType",
                    $"Escalation type must be one of {string.Join(", ", EscalationType.Allowed)}.");
            }

            if (level.Level is < 1 or > 4)
            {
                return Error.Validation(
                    "SlaEscalation.Level",
                    "An escalation level is 1 through 4.");
            }

            if (!seen.Add((level.EscalationType, level.Level)))
            {
                return Error.Validation(
                    "SlaEscalation.DuplicateLevel",
                    $"{level.EscalationType} level {level.Level} appears twice.");
            }

            if (level.ThresholdPercent is < 1 or > 1000)
            {
                return Error.Validation(
                    "SlaEscalation.Threshold",
                    "A threshold is 1 to 1000 per cent of the target.");
            }

            if (!EscalationRecipient.Allowed.Contains(level.RecipientType, StringComparer.Ordinal))
            {
                return Error.Validation(
                    "SlaEscalation.UnknownRecipient",
                    $"Recipient must be one of {string.Join(", ", EscalationRecipient.Allowed)}.");
            }

            if (level.RecipientType == EscalationRecipient.Custom
                && string.IsNullOrWhiteSpace(level.RecipientAddress))
            {
                return Error.Validation(
                    "SlaEscalation.CustomAddress",
                    "A Custom recipient needs an address.");
            }

            if (!EscalationChannel.Allowed.Contains(level.Channel, StringComparer.Ordinal))
            {
                return Error.Validation(
                    "SlaEscalation.UnknownChannel",
                    $"Channel must be one of {string.Join(", ", EscalationChannel.Allowed)}.");
            }
        }

        // Thresholds have to climb. Level 2 firing before level 1 is not a
        // ladder, and the worker walks them in level order.
        foreach (var group in levels.GroupBy(l => l.EscalationType))
        {
            var ordered = group.OrderBy(l => l.Level).ToList();

            for (var i = 1; i < ordered.Count; i++)
            {
                if (ordered[i].ThresholdPercent <= ordered[i - 1].ThresholdPercent)
                {
                    return Error.Validation(
                        "SlaEscalation.ThresholdOrder",
                        $"{group.Key} level {ordered[i].Level} must fire later than level "
                        + $"{ordered[i - 1].Level}.");
                }
            }
        }

        return null;
    }
}
