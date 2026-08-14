namespace AMS.Modules.Identity.PublicApi.Identity;

/// <summary>Enough about a user to write to them and to name them in a record.</summary>
/// <param name="UserId">Identity.User.</param>
/// <param name="EmployeeId">The employee behind the account, when there is one.</param>
/// <param name="DisplayName">What to call them.</param>
/// <param name="Email">Where to write. Null when the account has no address.</param>
/// <remarks>
/// Deliberately four fields. A consumer that needs a user's roles, branches or
/// login history is asking Identity to be its data layer, and the answer is a
/// slice in Identity rather than a wider contract here.
/// </remarks>
public sealed record UserContact(int UserId, int? EmployeeId, string DisplayName, string? Email);
