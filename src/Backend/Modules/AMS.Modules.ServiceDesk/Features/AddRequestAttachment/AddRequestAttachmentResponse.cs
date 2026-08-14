namespace AMS.Modules.ServiceDesk.Features.AddRequestAttachment;

/// <summary>
/// The file, as listed on the ticket.
/// </summary>
/// <param name="Id">The attachment row.</param>
/// <param name="ServiceRequestId">The ticket.</param>
/// <param name="AttachmentType">Requester, Resolution or Email.</param>
/// <param name="FileName">What to show; FilePath is where it actually lives.</param>
public sealed record AddRequestAttachmentResponse(
    int Id,
    int ServiceRequestId,
    string AttachmentType,
    string? FileName);
