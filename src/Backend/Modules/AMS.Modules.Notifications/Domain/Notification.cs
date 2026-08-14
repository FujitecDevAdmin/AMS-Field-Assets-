namespace AMS.Modules.Notifications.Domain;

/// <summary>
/// Mirrors <c>[Notifications].[Notification]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class Notification
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public required string Text { get; set; }

    public string? DeepLink { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public DateTime? ReadOnUtc { get; set; }
}
