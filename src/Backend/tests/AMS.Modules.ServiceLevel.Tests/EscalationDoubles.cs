using AMS.Modules.Identity.PublicApi.Identity;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.ServiceDesk.PublicApi.ServiceDesk;

namespace AMS.Modules.ServiceLevel.Tests;

/// <summary>
/// ServiceDesk's tickets, as far as the escalation monitor is concerned.
/// </summary>
/// <remarks>
/// A stub, and the point of the contract. Whether a ticket is open, has a
/// policy and has due dates is ServiceDesk's question, tested there against its
/// own schema; what ServiceLevel has to get right is when a rung fires and who
/// it reaches.
/// </remarks>
public sealed class FakeSlaWatchList : ISlaWatchList
{
    private readonly List<SlaWatchTicket> _tickets = [];
    private readonly Dictionary<int, List<int>> _leads = [];

    /// <summary>Every timeline entry the monitor wrote back.</summary>
    public List<(int TicketId, string Text)> Notes { get; } = [];

    public FakeSlaWatchList With(SlaWatchTicket ticket)
    {
        _tickets.Add(ticket);

        return this;
    }

    public FakeSlaWatchList WithTeamLeads(int teamId, params int[] userIds)
    {
        _leads[teamId] = [.. userIds];

        return this;
    }

    public void Reset()
    {
        _tickets.Clear();
        _leads.Clear();
        Notes.Clear();
    }

    public Task<IReadOnlyList<SlaWatchTicket>> OpenTicketsAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<SlaWatchTicket>>([.. _tickets]);

    public Task<IReadOnlyList<int>> TeamLeadsAsync(int supportTeamId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<int>>(
            _leads.TryGetValue(supportTeamId, out var leads) ? leads : []);

    public Task NoteEscalationAsync(int ticketId, string text, CancellationToken ct)
    {
        Notes.Add((ticketId, text));

        return Task.CompletedTask;
    }
}

/// <summary>Identity's people, as a list the test writes.</summary>
public sealed class FakeUserDirectory : IUserDirectory
{
    private readonly List<(UserContact Contact, string? Capability, int? BranchId)> _entries = [];

    public FakeUserDirectory With(
        int userId,
        string name,
        string? email,
        int? employeeId = null,
        string? capability = null,
        int? branchId = null)
    {
        _entries.Add((new UserContact(userId, employeeId, name, email), capability, branchId));

        return this;
    }

    public Task<UserContact?> FindAsync(int userId, CancellationToken ct) =>
        Task.FromResult(_entries
            .Where(e => e.Contact.UserId == userId)
            .Select(e => e.Contact)
            .FirstOrDefault());

    public Task<UserContact?> ForEmployeeAsync(int employeeId, CancellationToken ct) =>
        Task.FromResult(_entries
            .Where(e => e.Contact.EmployeeId == employeeId)
            .Select(e => e.Contact)
            .FirstOrDefault());

    public Task<IReadOnlyList<UserContact>> InRoleAsync(int roleId, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UserContact>>([]);

    public Task<IReadOnlyList<UserContact>> WithCapabilityAsync(
        string capabilityName,
        int? branchId,
        CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<UserContact>>(
            [.. _entries
                .Where(e => e.Capability == capabilityName)
                .Where(e => branchId is null || e.BranchId is null || e.BranchId == branchId)
                .Select(e => e.Contact)]);
}

/// <summary>Organization's reporting line, as a dictionary.</summary>
public sealed class FakeEmployeeDirectory : IEmployeeDirectory
{
    private readonly Dictionary<int, int> _managers = [];

    public FakeEmployeeDirectory Reports(int employeeId, int toManagerEmployeeId)
    {
        _managers[employeeId] = toManagerEmployeeId;

        return this;
    }

    public Task<int?> ManagerOfAsync(int employeeId, CancellationToken ct) =>
        Task.FromResult(_managers.TryGetValue(employeeId, out var manager) ? manager : (int?)null);

    public Task<int?> BranchOfAsync(int employeeId, CancellationToken ct) =>
        Task.FromResult<int?>(null);
}

/// <summary>The outbox, as a list of what it was asked to send.</summary>
public sealed class FakeNotifier : INotifier
{
    private long _nextId = 9000;

    public List<OutboundEmail> Queued { get; } = [];

    public List<(int UserId, string Text)> Notified { get; } = [];

    public void Reset()
    {
        Queued.Clear();
        Notified.Clear();
    }

    public Task<long> QueueEmailAsync(OutboundEmail message, CancellationToken ct)
    {
        Queued.Add(message);

        return Task.FromResult(_nextId++);
    }

    public Task NotifyAsync(int userId, string text, string? deepLink, CancellationToken ct)
    {
        Notified.Add((userId, text));

        return Task.CompletedTask;
    }

    public Task NotifyManyAsync(
        IEnumerable<int> userIds,
        string text,
        string? deepLink,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(userIds);

        foreach (var userId in userIds.Distinct())
        {
            Notified.Add((userId, text));
        }

        return Task.CompletedTask;
    }
}
