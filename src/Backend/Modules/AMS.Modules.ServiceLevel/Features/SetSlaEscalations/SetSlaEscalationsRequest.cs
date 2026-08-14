namespace AMS.Modules.ServiceLevel.Features.SetSlaEscalations;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SetSlaEscalationsRequest(
    IReadOnlyList<SetSlaEscalationsRequest.Rung> Levels)
{
    /// <summary>One rung of the ladder, as the setup screen sends it.</summary>
    public sealed record Rung(
        string EscalationType,
        int Level,
        int ThresholdPercent,
        string RecipientType,
        string? RecipientAddress,
        string? Channel);
}
