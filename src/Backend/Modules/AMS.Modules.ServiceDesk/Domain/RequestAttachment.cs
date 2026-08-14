namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[RequestAttachment]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class RequestAttachment
{
    public int Id { get; set; }

    public int ServiceRequestId { get; set; }

    public int? RequestEmailId { get; set; }

    public required string AttachmentType { get; set; }

    public required string FilePath { get; set; }

    public string? FileName { get; set; }

    public string? ContentType { get; set; }

    public long? SizeBytes { get; set; }

    public int? UploadedByUserId { get; set; }

    public DateTime UploadedOnUtc { get; set; }
}
