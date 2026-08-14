namespace AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;

/// <summary>
/// The ticket, open and numbered.
/// </summary>
/// <param name="Id">The ticket.</param>
/// <param name="RequestNumber">TKT-2026-000123. Drawn from a sequence, never reset (R2-17).</param>
/// <param name="RequestKind">SupportTicket, AssetIssue or NewService.</param>
/// <param name="Status">Always Open. Assignment and the clock are separate decisions.</param>
public sealed record RaiseServiceRequestResponse(
    int Id,
    string RequestNumber,
    string RequestKind,
    string Status);
