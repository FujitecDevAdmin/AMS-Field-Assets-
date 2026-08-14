using AMS.Modules.Allocations.Persistence;
using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Allocations.Tests;

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
/// A database carrying BOTH the Allocations and Assets schemas.
/// </summary>
/// <remarks>
/// Allocating writes to <c>[Allocations]</c> and its timeline line to
/// <c>[Assets]</c>. Two schemas in one database is the production arrangement,
/// and building the fixture any other way would make the timeline tests prove
/// something the application does not do.
/// </remarks>
public sealed class AllocationsFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_AllocationsTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    /// <summary>
    /// The same registrations <see cref="AllocationsModuleExtensions"/> makes.
    /// </summary>
    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_AssetAllocation_OneActivePerAsset", "Allocation.AssetAlreadyIssued",
            "That asset is already issued to somebody.")
        .Register("UX_AssetAcknowledgement_Allocation", "Acknowledgement.AlreadyExists",
            "That allocation already has an acknowledgement.")
        .Register("UX_AssetHandover_OneOpenPerAsset", "Handover.AlreadyInStore",
            "That asset is already sitting in a branch store.")
        .Register("UX_AssetHandover_OnePerAllocation", "Handover.AlreadyRecorded",
            "That allocation has already been handed over.")
        .Register("UX_AssetSiteMapping_OneActivePerAsset", "SiteMapping.AlreadyOnSite",
            "That asset is already at a customer site.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using (var assets = NewAssetsContext())
        {
            await assets.Database.MigrateAsync();
        }

        await using var allocations = NewContext();
        await allocations.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public AllocationsDbContext NewContext() =>
        new(new DbContextOptionsBuilder<AllocationsDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", AllocationsDbContext.SchemaName))
            .Options);

    public AssetsDbContext NewAssetsContext() =>
        new(new DbContextOptionsBuilder<AssetsDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", AssetsDbContext.SchemaName))
            .Options);

    /// <summary>An asset to allocate, with the lookups it needs.</summary>
    public async Task<int> AddAssetAsync(string assetNumber)
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
            IsDeleted = false,
            ConcurrencyStamp = Guid.NewGuid(),
            CreatedOnUtc = Clock.UtcNow,
            CreatedBy = "test",
        };
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        return asset.Id;
    }

    /// <summary>The timeline entries written against one asset, in order.</summary>
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
            DELETE FROM [Allocations].[AssetReturnImage];
            DELETE FROM [Allocations].[AllocationReturnReversal];
            DELETE FROM [Allocations].[AssetHandover];
            DELETE FROM [Allocations].[AssetAcknowledgement];
            DELETE FROM [Allocations].[AssetAllocationApproval];
            DELETE FROM [Allocations].[AssetAllocation];
            DELETE FROM [Allocations].[AssetSiteMapping];
            DELETE FROM [Allocations].[CustomerSite];
            """);

        await ExecuteAsync("DELETE FROM [Assets].[AssetEvent];");
        await ExecuteAsync("ALTER TABLE [Assets].[Asset] SET (SYSTEM_VERSIONING = OFF);");
        await ExecuteAsync("DELETE FROM [Assets].[Asset]; DELETE FROM [Assets].[AssetHistory];");
        await ExecuteAsync("""
            ALTER TABLE [Assets].[Asset]
                SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Assets].[AssetHistory]));
            """);
        await ExecuteAsync("DELETE FROM [Assets].[AssetStatus]; DELETE FROM [Assets].[AssetType];");

        CurrentUser.Id = 1;
        CurrentUser.EmployeeId = null;
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

[CollectionDefinition(nameof(AllocationsCollectionDefinition))]
public sealed class AllocationsCollectionDefinition : ICollectionFixture<AllocationsFixture>;
