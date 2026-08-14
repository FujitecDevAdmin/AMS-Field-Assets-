using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.SetSlaEscalations;

/// <summary>
/// Set a policy's escalation ladder, all of it at once. Catalogue: SLA Policy Setup.
/// </summary>
public sealed record SetSlaEscalationsCommand(
    int Id,
    IReadOnlyList<SetSlaEscalationsCommand.Rung> Levels) : ICommand<SetSlaEscalationsResponse>
{
    /// <summary>One rung of the ladder.</summary>
    /// <param name="EscalationType">Response or Resolution.</param>
    /// <param name="Level">1 through 4, per the handbook.</param>
    /// <param name="ThresholdPercent">
    /// ADDITIVE to the target: 100 means at the due time, 150 means half the
    /// target again past it. A percentage rather than absolute minutes, so one
    /// ladder can serve policies with different targets.
    /// </param>
    /// <param name="RecipientType">
    /// AssignedTechnician, TeamLead, BranchAdmin, Manager or Custom.
    /// </param>
    /// <param name="RecipientAddress">Required for Custom, and meaningless otherwise.</param>
    /// <param name="Channel">Email, InApp or Both.</param>
    public sealed record Rung(
        string EscalationType,
        int Level,
        int ThresholdPercent,
        string RecipientType,
        string? RecipientAddress,
        string Channel);
}
