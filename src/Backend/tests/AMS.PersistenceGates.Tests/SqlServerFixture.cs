using Microsoft.Data.SqlClient;

namespace AMS.PersistenceGates.Tests;

/// <summary>
/// A scratch database on the local SQL Server Express instance, rebuilt from
/// nothing at the start of every run.
/// </summary>
/// <remarks>
/// Deliberately NOT an in-memory or SQLite provider. Both gates are about what
/// SQL Server actually does — how it stamps a period column, and whether two
/// DbContexts can share one transaction. A provider that fakes either would
/// prove nothing and would do it convincingly.
/// </remarks>
public sealed class SqlServerFixture : IAsyncLifetime
{
    private const string Instance = @".\SQLEXPRESS2022";
    private const string Database = "AMS_PersistenceGates";

    public string ConnectionString { get; } =
        $"Server={Instance};Database={Database};Integrated Security=true;"
        + "TrustServerCertificate=true;MultipleActiveResultSets=true";

    private static string MasterConnectionString =>
        $"Server={Instance};Database=master;Integrated Security=true;TrustServerCertificate=true";

    public async ValueTask InitializeAsync()
    {
        await DropDatabaseAsync();

        await ExecuteOnMasterAsync($"CREATE DATABASE [{Database}];");

        // The temporal table is declared exactly as the design script declares
        // its five: period columns generated always, system versioning on, a
        // named history table. Fewer columns, identical mechanism.
        await ExecuteAsync(
            """
            CREATE SCHEMA [Assets];
            """);

        await ExecuteAsync(
            """
            CREATE TABLE [Assets].[GateAsset] (
                [Id]           int           NOT NULL IDENTITY,
                [Name]         nvarchar(100) NOT NULL,
                [SysStartTime] datetime2     GENERATED ALWAYS AS ROW START NOT NULL,
                [SysEndTime]   datetime2     GENERATED ALWAYS AS ROW END   NOT NULL,
                CONSTRAINT [PK_GateAsset] PRIMARY KEY ([Id]),
                PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime])
            ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Assets].[GateAssetHistory]));
            """);

        // Gate C: the same temporal shape, but carrying R2-22's token.
        await ExecuteAsync(
            """
            CREATE TABLE [Assets].[StampedAsset] (
                [Id]               int              NOT NULL IDENTITY,
                [Name]             nvarchar(100)    NOT NULL,
                [ConcurrencyStamp] uniqueidentifier NOT NULL
                    CONSTRAINT [DF_StampedAsset_ConcurrencyStamp] DEFAULT (NEWID()),
                [SysStartTime]     datetime2        GENERATED ALWAYS AS ROW START NOT NULL,
                [SysEndTime]       datetime2        GENERATED ALWAYS AS ROW END   NOT NULL,
                CONSTRAINT [PK_StampedAsset] PRIMARY KEY ([Id]),
                PERIOD FOR SYSTEM_TIME ([SysStartTime], [SysEndTime])
            ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [Assets].[StampedAssetHistory]));
            """);

        // Two schemas standing in for two modules, for the rule 4a gate.
        await ExecuteAsync(
            """
            CREATE SCHEMA [Allocations];
            """);

        await ExecuteAsync(
            """
            CREATE TABLE [Allocations].[GateHandover] (
                [Id]      int           NOT NULL IDENTITY,
                [Remarks] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_GateHandover] PRIMARY KEY ([Id])
            );
            """);

        await ExecuteAsync(
            """
            CREATE TABLE [Assets].[GateAssetEvent] (
                [Id]          int           NOT NULL IDENTITY,
                [Description] nvarchar(500) NOT NULL,
                CONSTRAINT [PK_GateAssetEvent] PRIMARY KEY ([Id])
            );
            """);
    }

    public async ValueTask DisposeAsync() => await DropDatabaseAsync();

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
        await ExecuteOnMasterAsync(
            $"""
             IF DB_ID('{Database}') IS NOT NULL
             BEGIN
                 ALTER DATABASE [{Database}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                 DROP DATABASE [{Database}];
             END
             """);

    private static async Task ExecuteOnMasterAsync(string sql)
    {
        await using var connection = new SqlConnection(MasterConnectionString);
        await connection.OpenAsync();
        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();
    }
}

[CollectionDefinition(nameof(SqlServerCollectionDefinition))]
public sealed class SqlServerCollectionDefinition : ICollectionFixture<SqlServerFixture>;
