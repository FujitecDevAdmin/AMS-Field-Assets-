using AMS.SharedKernel.Abstractions;

namespace AMS.Modules.Identity.Domain;

/// <summary>
/// A branch this user can see. Read together with
/// <see cref="User.HasAllBranches"/>, which short-circuits the whole list.
/// </summary>
public sealed class UserBranch : IGrantable
{
    public int UserId { get; set; }

    /// <summary><c>Organization.Branch</c> — id only, no foreign key.</summary>
    public int BranchId { get; set; }

    /// <summary>
    /// At most one per user, enforced by the filtered unique index
    /// <c>UX_UserBranch_OnePrimary</c> rather than by a check in code.
    /// </summary>
    public bool IsPrimary { get; set; }

    public DateTime GrantedOnUtc { get; set; }

    public string? GrantedBy { get; set; }
}
