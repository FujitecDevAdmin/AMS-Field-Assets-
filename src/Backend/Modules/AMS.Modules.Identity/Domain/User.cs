using AMS.SharedKernel.Abstractions;

namespace AMS.Modules.Identity.Domain;

/// <summary>
/// A login. There is exactly one of these tables — field asset administrators
/// are an ordinary user holding the field-asset capabilities, because a second
/// identity store means a second password policy and a second place to forget
/// to disable somebody who has left.
/// </summary>
public sealed class User : IAuditable
{
    public int Id { get; set; }

    public required string Username { get; set; }

    public required string DisplayName { get; set; }

    /// <summary>Hashed, never in clear.</summary>
    public required string PasswordHash { get; set; }

    public string? Email { get; set; }

    /// <summary>
    /// <c>Organization.Employee</c> — id only. No foreign key and no
    /// navigation property: that is another module's schema (01 §2 rule 2).
    /// </summary>
    public int? EmployeeId { get; set; }

    public bool MustChangePassword { get; set; }

    public bool IsLocked { get; set; }

    public int FailedLoginAttempts { get; set; }

    public DateTime? LastLoginOnUtc { get; set; }

    /// <summary>Head office. When true, <c>UserBranch</c> rows are not consulted.</summary>
    public bool HasAllBranches { get; set; }

    public bool IsActive { get; set; }

    public bool MfaEnabled { get; set; }

    /// <summary>
    /// Protected with ASP.NET Data Protection, purpose
    /// <c>AMS.Identity.MfaSecret</c>. Excluded from audit, from logging, and
    /// from every projection that feeds a grid (03 §8).
    /// </summary>
    public byte[]? MfaSecretEncrypted { get; set; }

    public DateTime? MfaEnrolledOnUtc { get; set; }

    public bool MfaEnrollmentRequired { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Concurrency token. Not a temporal table, so this is a real rowversion.
    /// </summary>
    /// <remarks>
    /// Non-nullable on purpose. R2-14 declared every remaining RowVersion
    /// column NOT NULL because the value is always generated, and a nullable
    /// property here maps to a nullable column — which the schema-parity check
    /// catches, having been written for exactly this class of slip.
    /// </remarks>
    public byte[] RowVersion { get; set; } = [];
}
