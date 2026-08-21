using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Organization.PublicApi.Organization;
using AMS.Modules.Verification.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Tests;

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

/// <summary>
/// The register, as far as verification is concerned.
/// </summary>
/// <remarks>
/// A stub. What an asset is called and whether it is a bulk line are Assets'
/// answers, tested there; what this module has to get right is what it does
/// with them.
/// </remarks>
public sealed class FakeAssetSnapshot : IAssetSnapshot
{
    private readonly Dictionary<int, AssetSnapshot> _assets = new()
    {
        [10] = new AssetSnapshot(10, "AMS-000010", 500, 1, null, null, false, ImportedBranch: "Branch 1"),
        [11] = new AssetSnapshot(11, "AMS-000011", null, 1, null, null, false, ImportedBranch: "Branch 1"),
        [20] = new AssetSnapshot(20, "AMS-000020", null, null, null, null, true, ImportedBranch: "Branch 1"),
        [21] = new AssetSnapshot(21, "AMS-000021", null, 1, null, null, true, ImportedBranch: "Branch 1"),
    };

    public Task<AssetSnapshot?> GetAsync(int assetId, CancellationToken ct) =>
        Task.FromResult(_assets.TryGetValue(assetId, out var asset) ? asset : null);

    public Task<IReadOnlyList<AssetSnapshot>> GetManyAsync(
        IReadOnlyCollection<int> assetIds,
        CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AssetSnapshot>>(
            _assets.Values.Where(asset => assetIds.Contains(asset.AssetId)).ToArray());

    public Task RecordPhysicalCheckAsync(int assetId, DateTime verifiedOnUtc, CancellationToken ct) =>
        Task.CompletedTask;

    public Task<AssetSnapshot?> FindByScanCodeAsync(string scanCode, CancellationToken ct) =>
        Task.FromResult(_assets.Values.FirstOrDefault(asset =>
            string.Equals(asset.AssetNumber, scanCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(asset.QrCodeValue, scanCode, StringComparison.OrdinalIgnoreCase)
            || string.Equals(asset.BarcodeValue, scanCode, StringComparison.OrdinalIgnoreCase)));

    public void Add(AssetSnapshot asset) => _assets[asset.AssetId] = asset;

    public Task<int> CountByImportedBranchesAsync(
        IReadOnlyCollection<int> branchIds,
        IReadOnlyCollection<string> branchAliases,
        CancellationToken ct) =>
        Task.FromResult(_assets.Count);

    public Task<IReadOnlyList<AssetSnapshot>> ListByImportedBranchesAsync(
        IReadOnlyCollection<int> branchIds,
        IReadOnlyCollection<string> branchAliases,
        CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<AssetSnapshot>>(_assets.Values.ToArray());
}

/// <summary>Branch Master records used by verification tests.</summary>
public sealed class FakeBranchDirectory : IBranchDirectory
{
    public Task<string?> TimeZoneOfAsync(int branchId, CancellationToken ct) =>
        Task.FromResult<string?>(branchId == 1 ? "India Standard Time" : null);

    public Task<bool> IsActiveAsync(int branchId, CancellationToken ct) =>
        Task.FromResult(branchId == 1);

    public Task<IReadOnlyList<BranchReference>> FindActiveAsync(
        IReadOnlyCollection<int> branchIds,
        CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BranchReference>>(branchIds.Contains(1)
            ? [new BranchReference(1, "B1", "Branch 1")]
            : []);

    public Task<IReadOnlyList<BranchReference>> ListActiveAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<BranchReference>>(
            [new BranchReference(1, "B1", "Branch 1")]);
}

/// <summary>The Verification schema, built by the module's own migrations.</summary>
public sealed class VerificationFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_VerificationTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    public FakeAssetSnapshot Assets { get; } = new();

    public FakeBranchDirectory Branches { get; } = new();

    /// <summary>The same registrations the module makes.</summary>
    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_PhysicalVerificationCycle_Name", "VerificationCycle.NameTaken",
            "A cycle with that name already exists.")
        .Register("UX_PhysicalVerification_ClientCapture", "Verification.AlreadyCaptured",
            "That capture has already been recorded.")
        .Register("UX_PhysicalVerification_OnePerUnitAssetPerCycle", "Verification.AlreadyVerified",
            "Somebody has already verified this asset in the current cycle.")
        .Register("UX_PhysicalVerification_OneBulkCountPerPlacePerCycle", "Verification.AlreadyCounted",
            "This line has already been counted at that location in the current cycle.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using var context = NewContext();
        await context.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public VerificationDbContext NewContext() =>
        new(new DbContextOptionsBuilder<VerificationDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", VerificationDbContext.SchemaName))
            .Options);

    public async Task ResetAsync()
    {
        Clock.Reset();
        CurrentUser.Id = 1;

        await ExecuteAsync("""
            DELETE FROM [Verification].[PhysicalVerification];
            DELETE FROM [Verification].[PhysicalVerificationAssignment];
            DELETE FROM [Verification].[PhysicalVerificationCycleLocation];
            DELETE FROM [Verification].[PhysicalVerificationCycle];
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

[CollectionDefinition(nameof(VerificationCollectionDefinition))]
public sealed class VerificationCollectionDefinition : ICollectionFixture<VerificationFixture>;
