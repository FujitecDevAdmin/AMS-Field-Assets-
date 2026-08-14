using AMS.SharedKernel.Abstractions;

namespace AMS.Modules.Identity.Domain;

/// <summary>
/// A named bundle of capabilities. Roles exist so an administrator can move a
/// capability between them without a release — which is exactly why no code
/// anywhere tests a role name.
/// </summary>
public sealed class Role : IAuditable
{
    public int Id { get; set; }

    public required string RoleName { get; set; }

    public string? Description { get; set; }

    /// <summary>A role the application depends on. Not deletable from the UI.</summary>
    public bool IsSystemRole { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
