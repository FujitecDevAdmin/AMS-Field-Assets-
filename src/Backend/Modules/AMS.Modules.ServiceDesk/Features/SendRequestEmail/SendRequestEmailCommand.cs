using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SendRequestEmail;

/// <summary>
/// Send e-mail from a ticket. Catalogue: Send e-mail on Request Detail.
/// </summary>
public sealed record SendRequestEmailCommand(
    int Id,
    string ToAddresses,
    string? CcAddresses,
    string Subject,
    string Body,
    bool IsHtml) : ICommand<SendRequestEmailResponse>;
