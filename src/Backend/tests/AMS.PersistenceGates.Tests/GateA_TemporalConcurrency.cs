using Microsoft.EntityFrameworkCore;

namespace AMS.PersistenceGates.Tests;

/// <summary>One row of the stand-in temporal table.</summary>
public sealed class GateAsset
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// Maps <see cref="GateAsset"/> exactly as docs/03 §4 prescribes for the five
/// system-versioned tables: no RowVersion, period start as the concurrency
/// token.
/// </summary>
public sealed class GateAssetsContext(DbContextOptions<GateAssetsContext> options) : DbContext(options)
{
    public DbSet<GateAsset> Assets => Set<GateAsset>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity = modelBuilder.Entity<GateAsset>();

        entity.ToTable("GateAsset", "Assets", table => table.IsTemporal(temporal =>
        {
            temporal.HasPeriodStart("SysStartTime");
            temporal.HasPeriodEnd("SysEndTime");
            temporal.UseHistoryTable("GateAssetHistory", "Assets");
        }));

        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();

        // The line under test. R2-1 removed RowVersion from these tables
        // because SQL Server forbids it, and nominated SysStartTime instead.
        entity.Property<DateTime>("SysStartTime")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();
    }
}

/// <summary>
/// GATE A — docs/03DATABASEEFCORESTANDARDS.md §4.
/// </summary>
/// <remarks>
/// <para>
/// <b>THIS GATE FAILED.</b> R2-1 nominated SysStartTime as the concurrency
/// token for the five system-versioned tables, on the stated premise that "it
/// is regenerated on every UPDATE". It is not. SQL Server stamps the period
/// start from the TRANSACTION start time, and the Windows system clock
/// advances in ticks of roughly 1-15 ms, so two updates inside one tick get
/// the SAME SysStartTime.
/// </para>
/// <para>
/// Measured: 20 of 20 insert-then-update pairs left SysStartTime unchanged; a
/// 50 ms delay changed it every time. A stale token that still matches means
/// the second writer's UPDATE succeeds and the first writer's change is lost
/// with no exception anywhere - precisely what optimistic concurrency exists
/// to prevent.
/// </para>
/// <para>
/// These tests document the boundary instead of hiding it. Delete them when
/// the five tables carry a real concurrency column.
/// </para>
/// </remarks>
[Collection(nameof(SqlServerCollectionDefinition))]
public sealed class GateATemporalConcurrency(SqlServerFixture fixture)
{
    /// <summary>The gap that reliably advances the period start on this platform.</summary>
    private static readonly TimeSpan ClockTick = TimeSpan.FromMilliseconds(50);

    [Fact]
    public async Task Test1_stale_token_is_rejected_only_once_the_clock_has_ticked()
    {
        var id = await SeedAsync("Laptop");

        await using var first = NewContext();
        await using var second = NewContext();

        var a = await first.Assets.SingleAsync(x => x.Id == id);
        var b = await second.Assets.SingleAsync(x => x.Id == id);

        // Without these delays the test is a coin toss. That is the finding,
        // not a flaky test: detection depends on the wall clock.
        await Task.Delay(ClockTick);

        a.Name = "Laptop (first writer)";
        await first.SaveChangesAsync();

        await Task.Delay(ClockTick);

        b.Name = "Laptop (second writer)";

        await Should.ThrowAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    [Fact]
    public async Task Test1b_inside_one_clock_tick_a_lost_update_goes_undetected()
    {
        // The defect itself, pinned down so it cannot be forgotten.
        var id = await SeedAsync("Tablet");

        await using var first = NewContext();
        await using var second = NewContext();

        var a = await first.Assets.SingleAsync(x => x.Id == id);
        var b = await second.Assets.SingleAsync(x => x.Id == id);

        var loadedByB = second.Entry(b).Property<DateTime>("SysStartTime").OriginalValue;

        a.Name = "written by the first writer";
        await first.SaveChangesAsync();

        var afterFirstWrite = await fixture.ScalarAsync<DateTime>(
            $"SELECT [SysStartTime] FROM [Assets].[GateAsset] WHERE [Id] = {id};");

        if (afterFirstWrite != loadedByB)
        {
            return; // the clock ticked; detection works, and Test1 covers that
        }

        // Same tick: b's token still matches, so this write is allowed through
        // and silently overwrites the first. No exception. No 412. No trace.
        b.Name = "written by the second writer";
        await second.SaveChangesAsync();

        var survivor = await fixture.ScalarAsync<string>(
            $"SELECT [Name] FROM [Assets].[GateAsset] WHERE [Id] = {id};");

        survivor.ShouldBe(
            "written by the second writer",
            "documenting the defect: within one clock tick the second writer wins silently");
    }

    [Fact]
    public async Task Test2_fresh_token_succeeds()
    {
        var id = await SeedAsync("Monitor");

        await using var context = NewContext();
        var asset = await context.Assets.SingleAsync(x => x.Id == id);

        asset.Name = "Monitor (renamed)";
        var affected = await context.SaveChangesAsync();

        affected.ShouldBe(1);
    }

    [Fact]
    public async Task Test3_token_is_refreshed_after_save()
    {
        // The one that decides whether a second save in the same request works.
        // If EF does not read the new period value back, the tracked entity
        // keeps the old one and the next save throws a false 412.
        var id = await SeedAsync("Printer");

        await using var context = NewContext();
        var asset = await context.Assets.SingleAsync(x => x.Id == id);

        var loaded = context.Entry(asset).Property<DateTime>("SysStartTime").CurrentValue;

        await Task.Delay(ClockTick);

        asset.Name = "Printer (renamed)";
        await context.SaveChangesAsync();

        var afterSave = context.Entry(asset).Property<DateTime>("SysStartTime").CurrentValue;
        var inDatabase = await fixture.ScalarAsync<DateTime>(
            $"SELECT [SysStartTime] FROM [Assets].[GateAsset] WHERE [Id] = {id};");

        afterSave.ShouldNotBe(loaded, "the period start must move when the row is updated");
        afterSave.ShouldBe(inDatabase, "the tracked token must match what SQL Server stored");
    }

    [Fact]
    public async Task Test4_two_updates_in_one_transaction_both_succeed()
    {
        var id = await SeedAsync("Scanner");

        await using var context = NewContext();
        await using var transaction = await context.Database.BeginTransactionAsync();

        var asset = await context.Assets.SingleAsync(x => x.Id == id);

        asset.Name = "Scanner (first edit)";
        await context.SaveChangesAsync();

        asset.Name = "Scanner (second edit)";
        await context.SaveChangesAsync();

        await transaction.CommitAsync();

        var finalName = await fixture.ScalarAsync<string>(
            $"SELECT [Name] FROM [Assets].[GateAsset] WHERE [Id] = {id};");

        finalName.ShouldBe("Scanner (second edit)");
    }

    private GateAssetsContext NewContext() =>
        new(new DbContextOptionsBuilder<GateAssetsContext>()
            .UseSqlServer(fixture.ConnectionString)
            .Options);

    private async Task<int> SeedAsync(string name)
    {
        await using var context = NewContext();
        var asset = new GateAsset { Name = name };
        context.Assets.Add(asset);
        await context.SaveChangesAsync();
        return asset.Id;
    }
}
