using AMS.Modules.Identity.PublicApi.Identity;
using AMS.Modules.Organization.PublicApi.Organization;

namespace AMS.Modules.ServiceDesk.Tests;

/// <summary>
/// Identity's directory, as far as ServiceDesk is concerned: a list the test
/// writes and the resolver reads.
/// </summary>
/// <remarks>
/// A stub and not the real <c>UserDirectory</c>, because ServiceDesk's tests
/// have no Identity database and should not have one. The contract is the
/// boundary; whether Identity answers it correctly is Identity's own tests'
/// question. That is the point of rule 3 — this module is testable without
/// standing up the other two.
/// </remarks>
public sealed class FakeUserDirectory : IUserDirectory
{
    private readonly List<(UserContact Contact, int? RoleId, string? Capability, int? BranchId)>
        entries = [];

    /// <summary>Adds somebody, optionally in a role and holding a capability.</summary>
    public FakeUserDirectory With(
        int userId,
        string name,
        string? email,
        int? employeeId = null,
        int? roleId = null,
        string? capability = null,
        int? branchId = null)
    {
        entries.Add((new UserContact(userId, employeeId, name, email), roleId, capability, branchId));

        return this;
    }

    public Task<UserContact?> FindAsync(int userId, CancellationToken ct) =>
        Task.FromResult(entries
            .Where(e => e.Contact.UserId == userId)
            .Select(e => e.Contact)
            .FirstOrDefault());

    public Task<UserContact?> ForEmployeeAsync(int employeeId, CancellationToken ct) =>
        Task.FromResult(entries
            .Where(e => e.Contact.EmployeeId == employeeId)
            .Select(e => e.Contact)
            .FirstOrDefault());

    public Task<IReadOnlyList<UserContact>> InRoleAsync(int roleId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UserContact>>(
            [.. entries.Where(e => e.RoleId == roleId).Select(e => e.Contact)]);

    public Task<IReadOnlyList<UserContact>> WithCapabilityAsync(
        string capabilityName,
        int? branchId,
        CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UserContact>>(
            [.. entries
                .Where(e => e.Capability == capabilityName)
                .Where(e => branchId is null || e.BranchId is null || e.BranchId == branchId)
                .Select(e => e.Contact)]);
}

/// <summary>Organization's reporting line, as a dictionary.</summary>
public sealed class FakeEmployeeDirectory : IEmployeeDirectory
{
    private readonly Dictionary<int, int> managers = [];
    private readonly Dictionary<int, int> branches = [];

    public FakeEmployeeDirectory Reports(int employeeId, int toManagerEmployeeId)
    {
        managers[employeeId] = toManagerEmployeeId;

        return this;
    }

    public FakeEmployeeDirectory At(int employeeId, int branchId)
    {
        branches[employeeId] = branchId;

        return this;
    }

    public Task<int?> ManagerOfAsync(int employeeId, CancellationToken ct) =>
        Task.FromResult(managers.TryGetValue(employeeId, out var manager) ? manager : (int?)null);

    public Task<int?> BranchOfAsync(int employeeId, CancellationToken ct) =>
        Task.FromResult(branches.TryGetValue(employeeId, out var branch) ? branch : (int?)null);
}
