using AMS.Modules.Discovery.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Tests;

/// <summary>A clock the tests own.</summary>
public sealed class TestClock : IClock
{
    public static readonly DateTime Default = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow { get; set; } = Default;

    public void Advance(TimeSpan by) => UtcNow = UtcNow.Add(by);

    /// <summary>Back to the start, so one test cannot move another's clock.</summary>
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

/// <summary>The Discovery schema, built by the module's own migrations.</summary>
public sealed class DiscoveryFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_DiscoveryTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    /// <summary>The same registrations the module makes.</summary>
    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_DiscoveredDevice_Machine", "DiscoveredDevice.AlreadyKnown",
            "That machine has already been discovered.")
        .Register("UX_AssetInstalledSoftware_Install", "InstalledSoftware.AlreadyRecorded",
            "That installation is already recorded against this asset.")
        .Register("UX_SoftwareCatalog_Name", "SoftwareCatalog.NameTaken",
            "That title is already in the catalogue.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using var context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public DiscoveryDbContext NewContext() =>
        new(new DbContextOptionsBuilder<DiscoveryDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", DiscoveryDbContext.SchemaName))
            .Options);

    public async Task ResetAsync()
    {
        Clock.Reset();

        await ExecuteAsync("""
            DELETE FROM [Discovery].[AssetInstalledSoftware];
            DELETE FROM [Discovery].[AssetHealthHistory];
            DELETE FROM [Discovery].[AssetHealth];
            DELETE FROM [Discovery].[SoftwareCatalog];
            DELETE FROM [Discovery].[DiscoveredDevice];
            DELETE FROM [Discovery].[AgentApiKey];
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

[CollectionDefinition(nameof(DiscoveryCollectionDefinition))]
public sealed class DiscoveryCollectionDefinition : ICollectionFixture<DiscoveryFixture>;
