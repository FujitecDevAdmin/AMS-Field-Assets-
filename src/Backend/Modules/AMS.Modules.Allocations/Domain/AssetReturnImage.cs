namespace AMS.Modules.Allocations.Domain;

/// <summary>
/// Mirrors <c>[Allocations].[AssetReturnImage]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetReturnImage
{
    public int Id { get; set; }

    public int AllocationId { get; set; }

    public int? HandoverId { get; set; }

    public required string ImagePath { get; set; }

    public string? Caption { get; set; }

    public string? ContentType { get; set; }

    public long? SizeBytes { get; set; }

    public int? UploadedByUserId { get; set; }

    public DateTime CapturedOnUtc { get; set; }
}
