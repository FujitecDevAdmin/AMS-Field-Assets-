using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.Modules.Transfers.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Transfers.Tests;

/// <summary>A clock the tests own.</summary>
public sealed class TestClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);
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

/// <summary>A database carrying BOTH the Transfers and Assets schemas.</summary>
public sealed class TransfersFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_TransfersTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    /// <summary>Empty, exactly as the module registers it: this schema has no unique indexes.</summary>
    public SqlErrorTranslator SqlErrors { get; } = new();

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using (var assets = NewAssetsContext())
        {
            await assets.Database.MigrateAsync();
        }

        await using var transfers = NewContext();
        await transfers.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public TransfersDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TransfersDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", TransfersDbContext.SchemaName))
            .Options);

    public AssetsDbContext NewAssetsContext() =>
        new(new DbContextOptionsBuilder<AssetsDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", AssetsDbContext.SchemaName))
            .Options);

    /// <summary>An asset with a known starting custody.</summary>
    public async Task<int> AddAssetAsync(
        string assetNumber,
        int? employeeId = null,
        int? locationId = null,
        int? departmentId = null,
        string? costCenter = null)
    {
        await using var context = NewAssetsContext();

        var type = new AssetType
        {
            TypeName = $"Laptops {Guid.NewGuid():N}",
            IsActive = true,
            CreatedOnUtc = Clock.UtcNow,
            CreatedBy = "test",
        };
        var status = new AssetStatus
        {
            StatusName = $"In Stock {Guid.NewGuid():N}",
            IsActive = true,
            IsTerminal = false,
            CreatedOnUtc = Clock.UtcNow,
            CreatedBy = "test",
        };
        context.AssetTypes.Add(type);
        context.AssetStatuses.Add(status);
        await context.SaveChangesAsync();

        var asset = new Asset
        {
            AssetNumber = assetNumber,
            AssetName = "A laptop",
            AssetTypeId = type.Id,
            AssetStatusId = status.Id,
            CurrentEmployeeId = employeeId,
            CurrentLocationId = locationId,
            DepartmentId = departmentId,
            CostCenter = costCenter,
            IsDeleted = false,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedOnUtc = Clock.UtcNow,
            CreatedBy = "test",
        };
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        return asset.Id;
    }

    /// <summary>The asset's custody as the register now has it.</summary>
    public async Task<(int? Employee, int? Location, int? Department, string? CostCenter)>
        CustodyOfAsync(int assetId)
    {
        await using var context = NewAssetsContext();
        var a = await context.Assets.AsNoTracking().SingleAsync(x => x.Id == assetId);
        return (a.CurrentEmployeeId, a.CurrentLocationId, a.DepartmentId, a.CostCenter);
    }

    public async Task<string[]> TimelineOfAsync(int assetId)
    {
        await using var context = NewAssetsContext();
        return await context.AssetEvents
            .Where(e => e.AssetId == assetId)
            .OrderBy(e => e.Id)
            .Select(e => e.EventType)
            .ToArrayAsync();
    }

    public async Task ResetAsync()
    {
        await ExecuteAsync("DELETE FROM [Transfers].[AssetTransferRequest];");
        await ExecuteAsync("DELETE FROM [Assets].[AssetEvent];");
        await ExecuteAsync("ALTER TABLE [Assets].[Asset] SET (SYSTEM_VERSIONING = OFF);");
        await ExecuteAsync("DELETE FROM [Assets].[Asset]; DELETE FROM [Assets].[AssetHistory];");
        await ExecuteAsync("""
            ALTER TABLE [Assets].[Asset]
                SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Assets].[AssetHistory]));
            """);
        await ExecuteAsync("DELETE FROM [Assets].[AssetStatus]; DELETE FROM [Assets].[AssetType];");

        CurrentUser.HasAllBranches = true;
        CurrentUser.BranchIds = new HashSet<int>();
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

[CollectionDefinition(nameof(TransfersCollectionDefinition))]
public sealed class TransfersCollectionDefinition : ICollectionFixture<TransfersFixture>;
