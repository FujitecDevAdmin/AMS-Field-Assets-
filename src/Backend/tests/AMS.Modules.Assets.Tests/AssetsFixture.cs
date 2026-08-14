using AMS.Modules.Assets.Domain;
using AMS.Modules.Assets.Persistence;
using AMS.Modules.Organization.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Tests;

/// <summary>A clock the tests own.</summary>
public sealed class TestClock : IClock
{
    public DateTime UtcNow { get; set; } = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);
}

/// <summary>The signed-in user the handlers stamp rows with.</summary>
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
/// A database carrying BOTH the Assets and Organization schemas.
/// </summary>
/// <remarks>
/// Two schemas in one database is not a test convenience — it is the
/// production arrangement. Fifteen modules, fifteen schemas, one database, one
/// connection string (01 §1). Building the fixture any other way would make
/// the cross-module transaction tests prove something the application does
/// not do.
/// </remarks>
public sealed class AssetsFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_AssetsTests";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    public TestClock Clock { get; } = new();

    public TestCurrentUser CurrentUser { get; } = new();

    /// <summary>
    /// The same registrations <see cref="AssetsModuleExtensions"/> makes.
    /// </summary>
    /// <remarks>
    /// Duplicated deliberately rather than reached for through the real
    /// container: <c>SqlErrorRegistrationTests</c> compares this against the
    /// LIVE schema, so if the two ever drift the guard fails and says which
    /// index is missing. Sharing one instance would let both be wrong together.
    /// </remarks>
    public SqlErrorTranslator SqlErrors { get; } = new SqlErrorTranslator()
        .Register("UX_AssetType_Name", "AssetType.NameTaken",
            "An asset type with that name already exists.")
        .Register("UX_AssetStatus_Name", "AssetStatus.NameTaken",
            "A status with that name already exists.")
        .Register("UX_AssetClass_Code", "AssetClass.CodeTaken",
            "An asset class with that code already exists.")
        .Register("UX_AssetClass_Name", "AssetClass.NameTaken",
            "An asset class with that name already exists.")
        .Register("UX_AssetClass_OneAuc", "AssetClass.AucExists",
            "There is already an assets-under-construction class.")
        .Register("UX_ChartOfAccount_Code", "ChartOfAccount.CodeTaken",
            "A chart-of-account code with that value already exists.")
        .Register("UX_CustomFieldDefinition_TypeField", "CustomField.NameTaken",
            "That asset type already has a field with this name.")
        .Register("UX_CustomFieldOption_Value", "CustomField.DuplicateOption",
            "Two dropdown options cannot have the same value.")
        .Register("UX_AssetCustomValue_AssetField", "CustomField.ValueAlreadySet",
            "That custom field already has a value on this asset.")
        .Register("UX_Asset_Number", "Asset.NumberTaken",
            "An asset with that number already exists.")
        .Register("UX_Asset_QrCode", "Asset.QrCodeTaken",
            "That QR code is already on another asset.")
        .Register("UX_Asset_SapNumber", "Asset.SapNumberTaken",
            "That SAP asset number is already on another asset.")
        .Register("UX_AssetVehicleDetail_Registration", "Vehicle.RegistrationTaken",
            "That registration number is already on another vehicle.")
        .Register("UX_AssetDepreciationEntry_AssetYear", "Depreciation.YearAlreadySynced",
            "That asset already has a depreciation row for this financial year.")
        .Register("UX_AssetHolding_AssetLocation", "Holding.AlreadyAtLocation",
            "That asset already has a holding at this branch.")
        .Register("UX_AssetHolding_AssetSite", "Holding.AlreadyAtSite",
            "That asset already has a holding at this customer site.");

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();
        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        await using (var assets = NewAssetsContext())
        {
            await assets.Database.MigrateAsync();
        }

        await using var organization = NewOrganizationContext();
        await organization.Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

    public AssetsDbContext NewAssetsContext() =>
        new(new DbContextOptionsBuilder<AssetsDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", AssetsDbContext.SchemaName))
            .Options);

    /// <summary>An Assets context bound to a connection somebody else owns (rule 4a).</summary>
    public static AssetsDbContext NewAssetsContext(SqlConnection connection) =>
        new(new DbContextOptionsBuilder<AssetsDbContext>().UseSqlServer(connection).Options);

    public OrganizationDbContext NewOrganizationContext() =>
        new(new DbContextOptionsBuilder<OrganizationDbContext>()
            .UseSqlServer(ConnectionString, sql => sql.MigrationsHistoryTable(
                "__EFMigrationsHistory", OrganizationDbContext.SchemaName))
            .Options);

    /// <summary>An Organization context on the same shared connection.</summary>
    public static OrganizationDbContext NewOrganizationContext(SqlConnection connection) =>
        new(new DbContextOptionsBuilder<OrganizationDbContext>().UseSqlServer(connection).Options);

    /// <summary>An asset to hang timeline entries on, with the lookups it needs.</summary>
    public async Task<int> AddAssetAsync(string assetNumber = "AST-0001")
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
            StatusName = $"In Use {Guid.NewGuid():N}",
            IsActive = true,
            IsTerminal = false,
            CreatedOnUtc = Clock.UtcNow,
            CreatedBy = "test",
        };

        context.AssetTypes.Add(type);
        context.AssetStatuses.Add(status);
        await context.SaveChangesAsync();

        // Quantity is left unset deliberately. R3 gave the column a DEFAULT of 1
        // and the configuration a matching HasDefaultValueSql, so EF omits it
        // from the INSERT while it holds the CLR default - which is the only
        // reason a plain `new Asset { ... }` still satisfies
        // CK_Asset_QuantityPositive. AddAssetAsync_defaults_a_unit_asset_to_one
        // is the test that keeps that true.
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

    public async Task ResetAsync()
    {
        // R3 order: everything that points at [Asset] goes first. All of these
        // are ON DELETE NO ACTION on purpose - financial and custody evidence
        // must not vanish because somebody deleted the asset - so a reset that
        // gets the order wrong fails loudly rather than quietly cascading.
        await ExecuteAsync("""
            DELETE FROM [Assets].[AssetEvent];
            DELETE FROM [Assets].[AssetDepreciationEntry];
            DELETE FROM [Assets].[AssetFinance];
            DELETE FROM [Assets].[AssetHolding];
            DELETE FROM [Assets].[AssetDisposal];
            DELETE FROM [Assets].[AssetVehicleDetail];
            DELETE FROM [Assets].[AssetInstrumentDetail];
            """);
        await ExecuteAsync("ALTER TABLE [Assets].[Asset] SET (SYSTEM_VERSIONING = OFF);");
        await ExecuteAsync("DELETE FROM [Assets].[Asset];");
        await ExecuteAsync("DELETE FROM [Assets].[AssetHistory];");
        await ExecuteAsync("""
            ALTER TABLE [Assets].[Asset]
                SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Assets].[AssetHistory]));
            """);
        // Custom fields hang off AssetType and their values off both, so they
        // go before the lookups. Every one of these is NO ACTION on purpose.
        await ExecuteAsync("""
            DELETE FROM [Assets].[AssetCustomValue];
            DELETE FROM [Assets].[CustomFieldOption];
            DELETE FROM [Assets].[CustomFieldDefinition];
            DELETE FROM [Assets].[AssetHardwareDetail];
            DELETE FROM [Assets].[AssetSoftwareDetail];
            DELETE FROM [Assets].[AssetPurchaseDetail];
            """);
        await ExecuteAsync("""
            DELETE FROM [Assets].[AssetStatus];
            DELETE FROM [Assets].[AssetType];
            DELETE FROM [Assets].[AssetClass];
            DELETE FROM [Assets].[ChartOfAccount];
            """);

        await ExecuteAsync("ALTER TABLE [Organization].[Employee] SET (SYSTEM_VERSIONING = OFF);");
        await ExecuteAsync("DELETE FROM [Organization].[Employee];");
        await ExecuteAsync("DELETE FROM [Organization].[EmployeeHistory];");
        await ExecuteAsync("""
            ALTER TABLE [Organization].[Employee]
                SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Organization].[EmployeeHistory]));
            """);
    }

    public async Task ExecuteAsync(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }

    public async Task<T?> ScalarAsync<T>(string sql)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        var value = await command.ExecuteScalarAsync();
        return value is null or DBNull ? default : (T)value;
    }

    private static async Task DropDatabaseAsync() =>
        await ExecuteOnMasterAsync($"""
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

[CollectionDefinition(nameof(AssetsCollectionDefinition))]
public sealed class AssetsCollectionDefinition : ICollectionFixture<AssetsFixture>;
