using AMS.Modules.Contracts.Persistence;
using AMS.Modules.Contracts.Reminders;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Tests;

/// <summary>A clock the tests own.</summary>
public sealed class TestClock : IClock
{
    /// <summary>A Wednesday morning, well inside a working week.</summary>
    public static readonly DateTime Default = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow { get; set; } = Default;

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);

    /// <summary>
    /// Back to the start.
    /// </summary>
    /// <remarks>
    /// The fixture is shared across every test class in the collection, so a
    /// test that moves the clock leaves it moved — and the pair only fail
    /// together, in an order that depends on how the runner feels.
    /// </remarks>
    public void Reset() => UtcNow = Default;
}

/// <summary>A caller the tests own.</summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public int Id { get; set; } = 1;

    public string Username { get; set; } = "test-admin";

    public int? EmployeeId { get; set; }

    public bool HasAllBranches { get; set; } = true;

    public IReadOnlySet<int> BranchIds { get; set; } = new HashSet<int>();

    public IReadOnlySet<string> Capabilities { get; set; } = new HashSet<string>();
}

/// <summary>Organization's vendors, as far as this module is concerned.</summary>
public sealed class FakeVendorDirectory : IVendorDirectory
{
    private readonly Dictionary<int, VendorContact> _vendors = new()
    {
        [1] = new VendorContact("Acme Systems", "R Kumar", "support@acme.example"),
        [2] = new VendorContact("No Contact Ltd", null, null),
    };

    public Task<VendorContact?> FindAsync(int vendorId, CancellationToken ct) =>
        Task.FromResult(_vendors.TryGetValue(vendorId, out var vendor) ? vendor : null);

    public Task<bool> IsActiveAsync(int vendorId, CancellationToken ct) =>
        Task.FromResult(_vendors.ContainsKey(vendorId));
}

/// <summary>The outbox, as a list of what it was asked to send.</summary>
public sealed class FakeNotifier : INotifier
{
    private long _nextId = 7000;

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

/// <summary>The Contracts schema, built by the module's own migrations.</summary>
public sealed class ContractsFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_ContractsTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    public FakeVendorDirectory Vendors { get; } = new();

    public FakeNotifier Notifier { get; } = new();

    /// <summary>
    /// A real protector over an ephemeral key ring.
    /// </summary>
    /// <remarks>
    /// Not a stub: what matters is that a key written by the create slice can
    /// be read back, and a pass-through fake would prove nothing about that.
    /// </remarks>
    public LicenceKeyProtector Protector { get; } =
        new(DataProtectionProvider.Create("AMS.Contracts.Tests"));

    /// <summary>The same registrations the module makes.</summary>
    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_Contract_Number", "Contract.NumberTaken",
            "A contract with that number already exists.")
        .Register("UX_ContractAsset_NoDuplicates", "Contract.AssetAlreadyCovered",
            "That asset is already covered by this contract.")
        .Register("UX_ContractReminderLog_OncePerThreshold", "ContractReminder.AlreadySent",
            "That reminder has already gone out for this expiry date.")
        .Register("UX_ContractReminderSetting_Default", "ContractReminder.DefaultExists",
            "The organisation already has a reminder at that many days.")
        .Register("UX_ContractReminderSetting_PerContract", "ContractReminder.WindowExists",
            "This contract already has a reminder at that many days.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using var context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public ContractsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<ContractsDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", ContractsDbContext.SchemaName))
            .Options);

    public async Task ResetAsync()
    {
        Clock.Reset();
        Notifier.Reset();

        await ExecuteAsync("""
            DELETE FROM [Contracts].[ContractReminderLog];
            DELETE FROM [Contracts].[ContractReminderSetting];
            DELETE FROM [Contracts].[ContractDocument];
            DELETE FROM [Contracts].[ContractAsset];
            DELETE FROM [Contracts].[Contract];
            """);
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task DropDatabaseAsync() => await ExecuteOnMasterAsync($"""
        IF DB_ID('{Database}') IS NOT NULL
        BEGIN
            ALTER DATABASE [{Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
            DROP DATABASE [{Database}];
        END
        """);

    private static async Task ExecuteOnMasterAsync(string sql)
    {
        await using var connection = new SqlConnection(
            $"Server={Instance};Database=master;Integrated Security=true;TrustServerCertificate=true");
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(nameof(ContractsCollectionDefinition))]
public sealed class ContractsCollectionDefinition : ICollectionFixture<ContractsFixture>;
