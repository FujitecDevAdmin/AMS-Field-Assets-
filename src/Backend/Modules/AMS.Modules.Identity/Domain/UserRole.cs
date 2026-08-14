using AMS.SharedKernel.Abstractions;

namespace AMS.Modules.Identity.Domain;

/// <summary>A user holds a role. Many to many, both ends inside this schema.</summary>
public sealed class UserRole : IGrantable
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

    public DateTime GrantedOnUtc { get; set; }

    public string? GrantedBy { get; set; }
}
