using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.PersistenceGates.Tests;

/// <summary>Stands in for Allocations.AssetHandover.</summary>
public sealed class GateHandover
{
    public int Id { get; set; }

    public string Remarks { get; set; } = string.Empty;
}

/// <summary>Stands in for Assets.AssetEvent — a DIFFERENT module's table.</summary>
public sealed class GateAssetEvent
{
    public int Id { get; set; }

    public string Description { get; set; } = string.Empty;
}

public sealed class GateAllocationsContext(DbContextOptions<GateAllocationsContext> options) : DbContext(options)
{
    public DbSet<GateHandover> Handovers => Set<GateHandover>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Allocations");
        modelBuilder.Entity<GateHandover>().ToTable("GateHandover");
        modelBuilder.Entity<GateHandover>().Property(x => x.Remarks).HasMaxLength(500).IsRequired();
    }
}

public sealed class GateTimelineContext(DbContextOptions<GateTimelineContext> options) : DbContext(options)
{
    public DbSet<GateAssetEvent> Events => Set<GateAssetEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("Assets");
        modelBuilder.Entity<GateAssetEvent>().ToTable("GateAssetEvent");
        modelBuilder.Entity<GateAssetEvent>().Property(x => x.Description).HasMaxLength(500).IsRequired();
    }
}

/// <summary>
/// GATE B — docs/01ARCHITECTURE.md rule 4a.
/// </summary>
/// <remarks>
/// Rule 4 requires a timeline or outbox row to commit with the change it
/// describes. Those tables belong to other modules, and one DbContext maps one
/// schema, so the transaction has to span contexts. This proves that a shared
/// DbConnection plus UseTransaction does it — without MSDTC, which is the
/// thing that would make the whole approach unacceptable.
/// </remarks>
[Collection(nameof(SqlServerCollectionDefinition))]
public sealed class GateBCrossModuleTransaction(SqlServerFixture fixture)
{
    [Fact]
    public async Task Two_module_contexts_commit_together_on_one_connection()
    {
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var allocations = NewAllocations(connection);
        await using var timeline = NewTimeline(connection);

        await using var transaction = await connection.BeginTransactionAsync();
        await allocations.Database.UseTransactionAsync(transaction as SqlTransaction);
        await timeline.Database.UseTransactionAsync(transaction as SqlTransaction);

        allocations.Handovers.Add(new GateHandover { Remarks = "handed to branch store" });
        await allocations.SaveChangesAsync();

        timeline.Events.Add(new GateAssetEvent { Description = "HandedOver" });
        await timeline.SaveChangesAsync();

        await transaction.CommitAsync();

        (await CountHandoversAsync()).ShouldBe(1);
        (await CountEventsAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task A_failure_in_the_second_module_rolls_back_the_first()
    {
        // The half that matters. If this does not roll back, rule 4 is a
        // comforting sentence rather than a guarantee, and a handover can
        // exist with no timeline entry describing it.
        var handoversBefore = await CountHandoversAsync();
        var eventsBefore = await CountEventsAsync();

        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var allocations = NewAllocations(connection);
        await using var timeline = NewTimeline(connection);

        await using var transaction = await connection.BeginTransactionAsync();
        await allocations.Database.UseTransactionAsync(transaction as SqlTransaction);
        await timeline.Database.UseTransactionAsync(transaction as SqlTransaction);

        allocations.Handovers.Add(new GateHandover { Remarks = "this must not survive" });
        await allocations.SaveChangesAsync();

        // 501 characters into an nvarchar(500): the write fails at the database.
        timeline.Events.Add(new GateAssetEvent { Description = new string('x', 501) });
        await Should.ThrowAsync<DbUpdateException>(() => timeline.SaveChangesAsync());

        await transaction.RollbackAsync();

        (await CountHandoversAsync()).ShouldBe(handoversBefore, "the first module's write must not survive");
        (await CountEventsAsync()).ShouldBe(eventsBefore);
    }

    [Fact]
    public async Task The_shared_connection_is_used_by_both_contexts()
    {
        // If either context opened its own connection, the transaction above
        // would silently cover only one of them.
        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync();

        await using var allocations = NewAllocations(connection);
        await using var timeline = NewTimeline(connection);

        allocations.Database.GetDbConnection().ShouldBeSameAs(connection);
        timeline.Database.GetDbConnection().ShouldBeSameAs(connection);
    }

    private static GateAllocationsContext NewAllocations(SqlConnection connection) =>
        new(new DbContextOptionsBuilder<GateAllocationsContext>()
            .UseSqlServer(connection)
            .Options);

    private static GateTimelineContext NewTimeline(SqlConnection connection) =>
        new(new DbContextOptionsBuilder<GateTimelineContext>()
            .UseSqlServer(connection)
            .Options);

    private async Task<int> CountHandoversAsync() =>
        await fixture.ScalarAsync<int>("SELECT COUNT(*) FROM [Allocations].[GateHandover];");

    private async Task<int> CountEventsAsync() =>
        await fixture.ScalarAsync<int>("SELECT COUNT(*) FROM [Assets].[GateAssetEvent];");
}
