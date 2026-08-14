namespace AMS.Modules.Assets.Domain;

/// <summary>
/// Mirrors <c>[Assets].[AssetSoftwareDetail]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AssetSoftwareDetail
{
    public int AssetId { get; set; }

    public string? OperatingSystem { get; set; }

    public string? OperatingSystemBuild { get; set; }

    public string? Architecture { get; set; }

    public string? OfficeVersion { get; set; }

    public string? Antivirus { get; set; }

    public byte[]? OsKeyEncrypted { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
