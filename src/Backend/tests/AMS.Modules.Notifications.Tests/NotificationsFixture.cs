using AMS.Modules.Notifications.Persistence;
using AMS.Modules.Notifications.Sending;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Notifications.Tests;

/// <summary>A clock the tests own.</summary>
public sealed class TestClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);
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

/// <summary>
/// A mail server that does what the test tells it to.
/// </summary>
/// <remarks>
/// The seam earning its keep. What matters about the dispatcher is what it does
/// when sending does NOT work, and there is no other way to arrange that
/// reliably.
/// </remarks>
public sealed class FakeEmailTransport : IEmailTransport
{
    /// <summary>Everything it was asked to send, in order.</summary>
    public List<OutgoingMessage> Sent { get; } = [];

    /// <summary>The profile it was last handed.</summary>
    public EmailProfile? LastProfile { get; private set; }

    /// <summary>When set, every send throws this.</summary>
    public Exception? Fails { get; set; }

    /// <summary>Fails this many more times, then starts working.</summary>
    public int FailuresRemaining { get; set; }

    public void Reset()
    {
        Sent.Clear();
        LastProfile = null;
        Fails = null;
        FailuresRemaining = 0;
    }

    public Task SendAsync(EmailProfile profile, OutgoingMessage message, CancellationToken ct)
    {
        LastProfile = profile;

        if (FailuresRemaining > 0)
        {
            FailuresRemaining--;

            throw Fails ?? new InvalidOperationException("The mail server refused the connection.");
        }

        if (Fails is not null)
        {
            throw Fails;
        }

        Sent.Add(message);

        return Task.CompletedTask;
    }
}

/// <summary>The Notifications schema, built by the module's own migrations.</summary>
public sealed class NotificationsFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_NotificationsTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    public FakeEmailTransport Transport { get; } = new();

    /// <summary>
    /// A real protector over an ephemeral key ring.
    /// </summary>
    /// <remarks>
    /// Not a stub: the point of these tests is that a password written by the
    /// settings slice can be read back by the dispatcher, and a pass-through
    /// fake would prove nothing about that.
    /// </remarks>
    public SmtpPasswordProtector Protector { get; } =
        new(DataProtectionProvider.Create("AMS.Notifications.Tests"));

    /// <summary>The same registrations the module makes.</summary>
    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_EmailSetting_Name", "EmailSetting.NameTaken",
            "A profile with that name already exists.")
        .Register("UX_EmailSetting_OneDefault", "EmailSetting.DefaultExists",
            "Another profile is already the default. Clear that one first.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using var context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public NotificationsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<NotificationsDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", NotificationsDbContext.SchemaName))
            .Options);

    public async Task ResetAsync()
    {
        Transport.Reset();

        await ExecuteAsync("""
            DELETE FROM [Notifications].[EmailOutbox];
            DELETE FROM [Notifications].[EmailSetting];
            DELETE FROM [Notifications].[Notification];
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

[CollectionDefinition(nameof(NotificationsCollectionDefinition))]
public sealed class NotificationsCollectionDefinition : ICollectionFixture<NotificationsFixture>;
