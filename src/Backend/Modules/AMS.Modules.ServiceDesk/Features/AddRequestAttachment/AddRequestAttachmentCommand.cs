using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.AddRequestAttachment;

/// <summary>
/// Record a file against a ticket. Catalogue: Attachments.
/// </summary>
public sealed record AddRequestAttachmentCommand(
    int Id,
    string AttachmentType,
    string FilePath,
    string? FileName,
    string? ContentType,
    long? SizeBytes) : ICommand<AddRequestAttachmentResponse>;
