namespace AMS.Modules.Notifications.Domain;

/// <summary>
/// Mirrors <c>[Notifications].[EmailSetting]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class EmailSetting
{
    public int Id { get; set; }

    public required string ProfileName { get; set; }

    public required string Host { get; set; }

    public int Port { get; set; }

    public bool UseSsl { get; set; }

    public required string FromAddress { get; set; }

    public string? Username { get; set; }

    public byte[]? SmtpPasswordEncrypted { get; set; }

    public bool IsDefault { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
