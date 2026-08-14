namespace AMS.Modules.ServiceDesk.Features.SendRequestEmail;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SendRequestEmailRequest(
    string ToAddresses,
    string? CcAddresses,
    string Subject,
    string Body,
    bool? IsHtml);
