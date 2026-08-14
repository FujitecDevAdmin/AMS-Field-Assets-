namespace AMS.Modules.Identity.Domain;

/// <summary>
/// A single-use MFA recovery code. Hashed like a password and never stored in
/// clear, because a recovery code is a password that bypasses the second
/// factor.
/// </summary>
public sealed class UserRecoveryCode
{
    public long Id { get; set; }

    public int UserId { get; set; }

    public required string CodeHash { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    /// <summary>Set the moment it is used. Null means still available.</summary>
    public DateTime? UsedOnUtc { get; set; }
}
