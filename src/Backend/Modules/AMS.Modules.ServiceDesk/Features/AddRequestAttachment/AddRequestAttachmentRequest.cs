namespace AMS.Modules.ServiceDesk.Features.AddRequestAttachment;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record AddRequestAttachmentRequest(
    string? AttachmentType,
    string FilePath,
    string? FileName,
    string? ContentType,
    long? SizeBytes);
