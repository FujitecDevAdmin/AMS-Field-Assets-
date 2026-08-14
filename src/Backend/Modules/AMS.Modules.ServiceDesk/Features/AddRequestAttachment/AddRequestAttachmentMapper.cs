using AMS.Modules.ServiceDesk.Domain;

namespace AMS.Modules.ServiceDesk.Features.AddRequestAttachment;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class AddRequestAttachmentMapper
{
    public static AddRequestAttachmentCommand ToCommand(AddRequestAttachmentRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AddRequestAttachmentCommand(
            id,
            string.IsNullOrWhiteSpace(request.AttachmentType) ? AttachmentKind.Requester : request.AttachmentType.Trim(),
            request.FilePath.Trim(),
            string.IsNullOrWhiteSpace(request.FileName) ? null : request.FileName.Trim(),
            string.IsNullOrWhiteSpace(request.ContentType) ? null : request.ContentType.Trim(),
            request.SizeBytes);
    }
}
