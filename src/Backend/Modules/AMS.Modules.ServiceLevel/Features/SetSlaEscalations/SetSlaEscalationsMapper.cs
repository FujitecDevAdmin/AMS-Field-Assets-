using AMS.Modules.ServiceLevel.Domain;

namespace AMS.Modules.ServiceLevel.Features.SetSlaEscalations;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SetSlaEscalationsMapper
{
    public static SetSlaEscalationsCommand ToCommand(SetSlaEscalationsRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SetSlaEscalationsCommand(
            id,
            [.. request.Levels.Select(l => new SetSlaEscalationsCommand.Rung(
                l.EscalationType.Trim(),
                l.Level,
                l.ThresholdPercent,
                l.RecipientType.Trim(),
                string.IsNullOrWhiteSpace(l.RecipientAddress) ? null : l.RecipientAddress.Trim(),
                string.IsNullOrWhiteSpace(l.Channel) ? EscalationChannel.Email : l.Channel.Trim()))]);
    }
}
