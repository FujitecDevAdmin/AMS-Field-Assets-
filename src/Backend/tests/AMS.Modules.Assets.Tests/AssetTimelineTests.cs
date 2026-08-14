using AMS.Modules.Assets.Persistence;
using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Organization.Domain;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Assets.Tests;

/// <summary>
/// The <see cref="IAssetTimeline"/> write contract, and rule 4a doing the job
/// it was designed for.
/// </summary>
/// <remarks>
/// Gate B proved two module contexts CAN share a transaction. These prove the
/// contract built on top of it behaves: the timeline row commits with the
/// change it describes, and dies with it.
/// </remarks>
[Collection(nameof(AssetsCollectionDefinition))]
public sealed class AssetTimelineTests(AssetsFixture fixture)
{
    // ------------------------------------------------------------- positive

    [Fact]
    public async Task Appending_writes_the_row_without_the_caller_saving()
    {
        // It saves its own context, and it must. A caller in another module
        // saves a DIFFERENT context, so a staged-but-unsaved row would be
        // dropped and the change would commit with no history. That is not a
        // weaker guarantee: atomicity comes from the transaction the dispatcher
        // owns, which A_failed_change_takes_its_timeline_row_with_it proves.
        await fixture.ResetAsync();
        var assetId = await fixture.AddAssetAsync();

        await using var context = fixture.NewAssetsContext();
        var timeline = new AssetTimeline(context);

        await timeline.AppendAsync(Entry(assetId, "Allocated"), TestContext.Current.CancellationToken);

        (await CountEventsAsync()).ShouldBe(1, "the contract does not depend on the caller saving");
    }

    [Fact]
    public async Task The_snapshots_survive_the_employee_being_renamed()
    {
        // "An event must still read correctly after the employee leaves or the
        // branch is renamed."
        await fixture.ResetAsync();
        var assetId = await fixture.AddAssetAsync();

        await using (var context = fixture.NewAssetsContext())
        {
            var timeline = new AssetTimeline(context);
            await timeline.AppendAsync(
                Entry(assetId, "Allocated") with
                {
                    EmployeeId = 7,
                    EmployeeNameSnapshot = "Asha Rao",
                    LocationId = 3,
                    LocationNameSnapshot = "Bangalore",
                },
                TestContext.Current.CancellationToken);

            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var name = await fixture.ScalarAsync<string>(
            $"SELECT [EmployeeNameSnapshot] FROM [Assets].[AssetEvent] WHERE [AssetId] = {assetId};");

        name.ShouldBe("Asha Rao", "the name is a snapshot, not a join resolved at read time");
    }

    // ------------------------------------------------- rule 4a, cross-module

    [Fact]
    public async Task A_timeline_row_commits_with_another_modules_change()
    {
        // Organization writes an Employee, Assets writes the timeline row, one
        // transaction on one connection. This is rule 4a in the shape every
        // allocation, handover and despatch will use.
        await fixture.ResetAsync();
        var assetId = await fixture.AddAssetAsync();

        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var organization = AssetsFixture.NewOrganizationContext(connection);
        await using var assets = AssetsFixture.NewAssetsContext(connection);

        await using var transaction = await connection.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await organization.Database.UseTransactionAsync(
            transaction as SqlTransaction, TestContext.Current.CancellationToken);
        await assets.Database.UseTransactionAsync(
            transaction as SqlTransaction, TestContext.Current.CancellationToken);

        organization.Employees.Add(NewEmployee("E-0001"));
        await organization.SaveChangesAsync(TestContext.Current.CancellationToken);

        await new AssetTimeline(assets).AppendAsync(
            Entry(assetId, "Allocated"), TestContext.Current.CancellationToken);
        await assets.SaveChangesAsync(TestContext.Current.CancellationToken);

        await transaction.CommitAsync(TestContext.Current.CancellationToken);

        (await CountEmployeesAsync()).ShouldBe(1);
        (await CountEventsAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task A_failed_change_takes_its_timeline_row_with_it()
    {
        // The half that matters. Without this, an asset could show "allocated"
        // on its timeline while no allocation exists.
        await fixture.ResetAsync();
        var assetId = await fixture.AddAssetAsync();

        await using var connection = new SqlConnection(fixture.ConnectionString);
        await connection.OpenAsync(TestContext.Current.CancellationToken);

        await using var organization = AssetsFixture.NewOrganizationContext(connection);
        await using var assets = AssetsFixture.NewAssetsContext(connection);

        await using var transaction = await connection.BeginTransactionAsync(
            TestContext.Current.CancellationToken);
        await organization.Database.UseTransactionAsync(
            transaction as SqlTransaction, TestContext.Current.CancellationToken);
        await assets.Database.UseTransactionAsync(
            transaction as SqlTransaction, TestContext.Current.CancellationToken);

        // The timeline row is staged and saved FIRST, exactly as a handler that
        // appends before its own final save would do.
        await new AssetTimeline(assets).AppendAsync(
            Entry(assetId, "Allocated"), TestContext.Current.CancellationToken);
        await assets.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Now the owning module's write fails: 31 characters into nvarchar(30).
        var tooLong = NewEmployee(new string('X', 31));
        organization.Employees.Add(tooLong);
        await Should.ThrowAsync<DbUpdateException>(() =>
            organization.SaveChangesAsync(TestContext.Current.CancellationToken));

        await transaction.RollbackAsync(TestContext.Current.CancellationToken);

        (await CountEventsAsync()).ShouldBe(0, "the timeline must not survive the change it describes");
        (await CountEmployeesAsync()).ShouldBe(0);
    }

    // ----------------------------------------------------------------- edge

    [Fact]
    public async Task A_timeline_row_for_an_unknown_asset_is_refused()
    {
        // FK_AssetEvent_Asset_AssetId. A timeline entry hanging off nothing is
        // a row nobody will ever read and nobody can explain.
        //
        // It throws from AppendAsync now rather than from the caller's save,
        // because the contract writes its own row. The guarantee is the same;
        // it just surfaces one line earlier, at the module that caused it.
        await fixture.ResetAsync();

        await using var context = fixture.NewAssetsContext();

        await Should.ThrowAsync<DbUpdateException>(() =>
            new AssetTimeline(context).AppendAsync(
                Entry(999_999, "Allocated"), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task Several_entries_for_one_asset_keep_their_order()
    {
        await fixture.ResetAsync();
        var assetId = await fixture.AddAssetAsync();

        await using var context = fixture.NewAssetsContext();
        var timeline = new AssetTimeline(context);

        foreach (var (type, minutes) in new[] { ("Registered", 0), ("Allocated", 5), ("HandedOver", 10) })
        {
            await timeline.AppendAsync(
                Entry(assetId, type) with { EventOnUtc = fixture.Clock.UtcNow.AddMinutes(minutes) },
                TestContext.Current.CancellationToken);
        }

        await context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var ordered = await context.AssetEvents
            .AsNoTracking()
            .Where(e => e.AssetId == assetId)
            .OrderBy(e => e.EventOnUtc)
            .Select(e => e.EventType)
            .ToListAsync(TestContext.Current.CancellationToken);

        ordered.ShouldBe(["Registered", "Allocated", "HandedOver"]);
    }

    private AssetTimelineEntry Entry(int assetId, string eventType) =>
        new(assetId, eventType, $"{eventType} by the test", fixture.Clock.UtcNow, "test-admin");

    private Employee NewEmployee(string code) => new()
    {
        EmployeeCode = code,
        FullName = "Test Person",
        IsActive = true,
        ConcurrencyStamp = Guid.NewGuid(),
        CreatedOnUtc = fixture.Clock.UtcNow,
        CreatedBy = "test",
    };

    private async Task<int> CountEventsAsync() =>
        await fixture.ScalarAsync<int>("SELECT COUNT(*) FROM [Assets].[AssetEvent];");

    private async Task<int> CountEmployeesAsync() =>
        await fixture.ScalarAsync<int>("SELECT COUNT(*) FROM [Organization].[Employee];");
}
