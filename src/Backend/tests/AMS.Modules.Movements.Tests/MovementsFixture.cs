using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.Modules.Movements.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Movements.Tests;

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

/// <summary>A database carrying BOTH the Movements and Assets schemas.</summary>
public sealed class MovementsFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_MovementsTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_MovementBatch_Number", "MovementBatch.NumberTaken",
            "That consignment number is already in use.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using (var assets = NewAssetsContext())
        {
            await assets.Database.MigrateAsync();
        }

        await using var movements = NewContext();
        await movements.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public MovementsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<MovementsDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", MovementsDbContext.SchemaName))
            .Options);

    public AssetsDbContext NewAssetsContext() =>
        new(new DbContextOptionsBuilder<AssetsDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", AssetsDbContext.SchemaName))
            .Options);

    /// <summary>An asset to despatch, starting at the given branch.</summary>
    public async Task<int> AddAssetAsync(string assetNumber, int? locationId = null)
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
            CurrentLocationId = locationId,
            IsDeleted = false,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedOnUtc = Clock.UtcNow,
            CreatedBy = "test",
        };
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        return asset.Id;
    }

    /// <summary>Where the register says an asset currently is.</summary>
    public async Task<int?> LocationOfAsync(int assetId)
    {
        await using var context = NewAssetsContext();
        return await context.Assets
            .Where(a => a.Id == assetId)
            .Select(a => a.CurrentLocationId)
            .SingleAsync();
    }

    /// <summary>The timeline entries against one asset, in order.</summary>
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
        await ExecuteAsync("""
            DELETE FROM [Movements].[AssetMovement];
            DELETE FROM [Movements].[MovementBatch];
            """);

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

[CollectionDefinition(nameof(MovementsCollectionDefinition))]
public sealed class MovementsCollectionDefinition : ICollectionFixture<MovementsFixture>;
