using AMS.Modules.Contracts.Domain;
using AMS.Modules.Contracts.Features.AddContractDocument;
using AMS.Modules.Contracts.Features.CreateContract;
using AMS.Modules.Contracts.Features.GetContract;
using AMS.Modules.Contracts.Features.RenewContract;
using AMS.Modules.Contracts.Features.SearchContracts;
using AMS.Modules.Contracts.Features.SetContractAssets;
using AMS.Modules.Contracts.Features.SetReminderWindows;
using AMS.Modules.Contracts.Features.UpdateContract;
using AMS.Modules.Contracts.Reminders;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Tests;

/// <summary>
/// Contracts, what they cover, when they run out, and who gets told.
/// </summary>
[Collection(nameof(ContractsCollectionDefinition))]
public sealed class ContractTests(ContractsFixture fixture)
{
    // ------------------------------------------------------- the contract

    [Fact]
    public async Task A_contract_can_be_recorded_with_what_it_covers()
    {
        // Saved with nothing covered and linked a minute later is a contract
        // that briefly covered nothing.
        await fixture.ResetAsync();

        var created = await CreateAsync("AMC-001", assetIds: [10, 11, 12]);

        created.IsSuccess.ShouldBeTrue();
        created.Value.AssetCount.ShouldBe(3);
        (await GetAsync(created.Value.Id)).Value.AssetIds.ShouldBe([10, 11, 12]);
    }

    [Fact]
    public async Task Two_contracts_cannot_share_a_number()
    {
        await fixture.ResetAsync();
        await CreateAsync("AMC-001");

        (await CreateAsync("AMC-001")).Error!.Code.ShouldBe("Contract.NumberTaken");
    }

    [Fact]
    public async Task A_contract_cannot_end_before_it_starts()
    {
        // CK_Contract_Window says the same thing, as a 500 naming a constraint.
        await fixture.ResetAsync();

        var result = await CreateAsync(
            "AMC-001",
            start: new DateOnly(2026, 12, 1),
            end: new DateOnly(2026, 1, 1));

        result.Error!.Code.ShouldBe("Contract.Window");
    }

    [Fact]
    public async Task A_type_outside_the_vocabulary_is_refused()
    {
        // There is no CHECK on the column, so this list is the only thing
        // keeping it a vocabulary rather than free text.
        await fixture.ResetAsync();

        (await CreateAsync("AMC-001", type: "Subscription")).Error!.Code
            .ShouldBe("Contract.UnknownType");
    }

    [Fact]
    public async Task A_vendor_that_does_not_exist_is_refused()
    {
        await fixture.ResetAsync();

        (await CreateAsync("AMC-001", vendorId: 987654)).Error!.Code.ShouldBe("Vendor.NotFound");
    }

    [Fact]
    public async Task A_licence_key_is_stored_protected_and_never_read_back()
    {
        // docs/03 §8: encrypted columns are excluded from any projection that
        // feeds a screen. The screen needs to know one is set, not what it is.
        await fixture.ResetAsync();

        var created = await CreateAsync("LIC-001", type: ContractType.Licence, licenceKey: "ABCD-1234");

        var detail = (await GetAsync(created.Value.Id)).Value;
        detail.HasLicenceKey.ShouldBeTrue();

        await using var db = fixture.NewContext();
        var stored = await db.Contracts.SingleAsync(
            c => c.Id == created.Value.Id, TestContext.Current.CancellationToken);
        stored.LicenseKeyEncrypted.ShouldNotBeNull();
        fixture.Protector.Unprotect(stored.LicenseKeyEncrypted).ShouldBe("ABCD-1234");
    }

    [Fact]
    public async Task Editing_without_a_licence_key_keeps_the_stored_one()
    {
        // The screen cannot show it, so it cannot send it back. Treating the
        // blank field as a deletion would wipe the key every time somebody
        // corrected a date.
        await fixture.ResetAsync();
        var id = (await CreateAsync("LIC-001", licenceKey: "ABCD-1234")).Value.Id;

        await UpdateAsync(id, name: "Renamed");

        (await GetAsync(id)).Value.HasLicenceKey.ShouldBeTrue();
    }

    [Fact]
    public async Task The_contract_number_is_not_editable()
    {
        // It is how the contract is quoted outside this system, where we cannot
        // see the references.
        await fixture.ResetAsync();
        var id = (await CreateAsync("AMC-001")).Value.Id;

        var updated = await UpdateAsync(id, name: "Renamed");

        updated.Value.ContractNumber.ShouldBe("AMC-001");
    }

    [Fact]
    public async Task Retiring_a_contract_hides_it_without_removing_it()
    {
        // A contract that covered an asset last year is what explains why a
        // repair was free.
        await fixture.ResetAsync();
        var id = (await CreateAsync("AMC-001")).Value.Id;

        await UpdateAsync(id, name: "Old", isDeleted: true);

        (await SearchAsync()).Value.Rows.ShouldBeEmpty();
        (await GetAsync(id)).Error!.Code.ShouldBe("Contract.NotFound");

        await using var db = fixture.NewContext();
        (await db.Contracts.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task An_unknown_contract_cannot_be_read_or_edited()
    {
        await fixture.ResetAsync();

        (await GetAsync(987654)).Error!.Code.ShouldBe("Contract.NotFound");
        (await UpdateAsync(987654, "Ghost")).Error!.Code.ShouldBe("Contract.NotFound");
        (await SetAssetsAsync(987654, [1])).Error!.Code.ShouldBe("Contract.NotFound");
        (await AddDocumentAsync(987654)).Error!.Code.ShouldBe("Contract.NotFound");
    }

    // ---------------------------------------------------------- searching

    [Fact]
    public async Task Contracts_are_listed_soonest_to_expire()
    {
        // The screen exists to answer one question: what do I have to do
        // something about.
        await fixture.ResetAsync();
        await CreateAsync("LATE", end: Today.AddDays(300));
        await CreateAsync("SOON", end: Today.AddDays(10));

        (await SearchAsync()).Value.Rows.Select(r => r.ContractNumber)
            .ShouldBe(["SOON", "LATE"]);
    }

    [Fact]
    public async Task Expired_contracts_are_hidden_unless_asked_for()
    {
        await fixture.ResetAsync();
        await CreateAsync("GONE", start: Today.AddDays(-400), end: Today.AddDays(-30));
        await CreateAsync("LIVE", end: Today.AddDays(30));

        (await SearchAsync()).Value.Rows.Single().ContractNumber.ShouldBe("LIVE");
        (await SearchAsync(includeExpired: true)).Value.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task The_expiring_count_is_over_everything_not_the_filter()
    {
        await fixture.ResetAsync();
        await CreateAsync("SOON-1", end: Today.AddDays(5));
        await CreateAsync("SOON-2", end: Today.AddDays(20));
        await CreateAsync("LATER", end: Today.AddDays(200));

        var page = (await SearchAsync(expiringWithinDays: 7)).Value;

        page.TotalCount.ShouldBe(1);
        page.ExpiringCount.ShouldBe(2);
    }

    [Fact]
    public async Task A_contract_can_be_found_by_number_name_type_and_vendor()
    {
        await fixture.ResetAsync();
        await CreateAsync("AMC-001", name: "Printer maintenance", type: ContractType.Amc, vendorId: 1);
        await CreateAsync("LIC-001", name: "Design software", type: ContractType.Licence);

        (await SearchAsync(search: "Printer")).Value.Rows.Single().ContractNumber.ShouldBe("AMC-001");
        (await SearchAsync(search: "LIC")).Value.Rows.Single().ContractNumber.ShouldBe("LIC-001");
        (await SearchAsync(type: ContractType.Licence)).Value.Rows.Single()
            .ContractNumber.ShouldBe("LIC-001");
        (await SearchAsync(vendorId: 1)).Value.Rows.Single().ContractNumber.ShouldBe("AMC-001");
    }

    [Fact]
    public async Task The_vendor_name_is_resolved_for_display()
    {
        await fixture.ResetAsync();
        await CreateAsync("AMC-001", vendorId: 1);

        (await SearchAsync()).Value.Rows.Single().VendorName.ShouldBe("Acme Systems");
    }

    [Fact]
    public async Task Days_to_expiry_goes_negative_once_it_has()
    {
        await fixture.ResetAsync();
        var id = (await CreateAsync(
            "GONE", start: Today.AddDays(-400), end: Today.AddDays(-30))).Value.Id;

        (await GetAsync(id)).Value.DaysToExpiry.ShouldBe(-30);
    }

    // ---------------------------------------------------------- renewing

    [Fact]
    public async Task Renewing_extends_the_same_row_and_counts_it()
    {
        // Not a new contract: the number is the same, the vendor is the same,
        // and the assets do not want re-linking every year.
        await fixture.ResetAsync();
        var id = (await CreateAsync("AMC-001", end: Today.AddDays(30), assetIds: [10])).Value.Id;

        var renewed = await RenewAsync(id, Today.AddDays(395));

        renewed.Value.RenewalCount.ShouldBe(1);
        renewed.Value.EndDate.ShouldBe(Today.AddDays(395));
        (await GetAsync(id)).Value.AssetIds.ShouldBe([10]);
    }

    [Fact]
    public async Task Renewing_to_a_date_that_is_not_later_is_refused()
    {
        // Most often it is somebody typing the current end date again.
        await fixture.ResetAsync();
        var id = (await CreateAsync("AMC-001", end: Today.AddDays(30))).Value.Id;

        (await RenewAsync(id, Today.AddDays(30))).Error!.Code
            .ShouldBe("Contract.RenewalNotLater");
    }

    [Fact]
    public async Task Renewal_remarks_are_appended_not_replaced()
    {
        // The reason for last year's renewal is still worth having.
        await fixture.ResetAsync();
        var id = (await CreateAsync("AMC-001", end: Today.AddDays(30))).Value.Id;
        await RenewAsync(id, Today.AddDays(395), remarks: "Renewed at the same rate.");

        await RenewAsync(id, Today.AddDays(760), remarks: "Rate increased by five per cent.");

        var remarks = (await GetAsync(id)).Value.Remarks;
        remarks.ShouldNotBeNull();
        remarks.ShouldContain("same rate");
        remarks.ShouldContain("five per cent");
    }

    // ----------------------------------------------------- assets and files

    [Fact]
    public async Task What_a_contract_covers_is_set_as_a_whole()
    {
        await fixture.ResetAsync();
        var id = (await CreateAsync("AMC-001", assetIds: [10, 11])).Value.Id;

        var set = await SetAssetsAsync(id, [11, 12]);

        set.Value.AssetCount.ShouldBe(2);
        (await GetAsync(id)).Value.AssetIds.ShouldBe([11, 12]);
    }

    [Fact]
    public async Task An_asset_that_stays_covered_keeps_the_date_it_came_under_cover()
    {
        await fixture.ResetAsync();
        var id = (await CreateAsync("AMC-001", assetIds: [10, 11])).Value.Id;
        var original = await LinkedOnAsync(id, assetId: 10);

        fixture.Clock.Advance(TimeSpan.FromDays(30));
        await SetAssetsAsync(id, [10, 12]);

        (await LinkedOnAsync(id, assetId: 10)).ShouldBe(original);
    }

    [Fact]
    public async Task The_same_asset_is_only_covered_once()
    {
        await fixture.ResetAsync();
        var id = (await CreateAsync("AMC-001")).Value.Id;

        var set = await SetAssetsAsync(id, [10, 10, 10]);

        set.Value.AssetCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_document_is_recorded_against_the_contract()
    {
        await fixture.ResetAsync();
        var id = (await CreateAsync("AMC-001")).Value.Id;

        var added = await AddDocumentAsync(id, @"\\files\contracts\amc-001.pdf");

        added.IsSuccess.ShouldBeTrue();
        (await GetAsync(id)).Value.Documents.Single().FileName.ShouldBe("amc-001.pdf");
    }

    // -------------------------------------------------- the reminder windows

    [Fact]
    public async Task A_contract_with_no_windows_of_its_own_inherits_the_default()
    {
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (60, null), (7, null));
        var id = (await CreateAsync("AMC-001")).Value.Id;

        var windows = (await GetAsync(id)).Value.Reminders;

        windows.Select(w => w.DaysBeforeExpiry).ShouldBe([60, 7]);
        windows.ShouldAllBe(w => !w.IsContractSpecific);
    }

    [Fact]
    public async Task A_contract_override_replaces_the_default_rather_than_adding_to_it()
    {
        // Merging would mean a contract that wants only a ninety-day warning
        // still gets the seven-day one, with no way to ask for less.
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (60, null), (7, null));
        var id = (await CreateAsync("AMC-001")).Value.Id;

        await SetWindowsAsync(id, (90, null));

        var windows = (await GetAsync(id)).Value.Reminders;
        windows.Select(w => w.DaysBeforeExpiry).ShouldBe([90]);
        windows.ShouldAllBe(w => w.IsContractSpecific);
    }

    [Fact]
    public async Task Setting_the_windows_again_replaces_them()
    {
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (60, null), (30, null), (7, null));

        await SetWindowsAsync(null, (14, null));

        var id = (await CreateAsync("AMC-001")).Value.Id;
        (await GetAsync(id)).Value.Reminders.Select(w => w.DaysBeforeExpiry).ShouldBe([14]);
    }

    [Fact]
    public async Task A_window_outside_the_allowed_range_is_refused()
    {
        await fixture.ResetAsync();

        (await SetWindowsAsync(null, (400, null))).Error!.Code.ShouldBe("ContractReminder.Days");
        (await SetWindowsAsync(null, (0, null))).Error!.Code.ShouldBe("ContractReminder.Days");
    }

    [Fact]
    public async Task The_same_window_cannot_appear_twice()
    {
        await fixture.ResetAsync();

        (await SetWindowsAsync(null, (30, null), (30, null))).Error!.Code
            .ShouldBe("ContractReminder.DuplicateWindow");
    }

    [Fact]
    public async Task Windows_for_a_contract_that_does_not_exist_are_refused()
    {
        await fixture.ResetAsync();

        (await SetWindowsAsync(987654, (30, null))).Error!.Code.ShouldBe("Contract.NotFound");
    }

    // -------------------------------------------------------- the reminders

    [Fact]
    public async Task Nothing_goes_out_before_a_window_opens()
    {
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null));
        await CreateAsync("AMC-001", end: Today.AddDays(60), vendorId: 1);

        (await RunWorkerAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task A_reminder_goes_to_the_vendor_when_nobody_else_is_named()
    {
        // The person who can do something about an expiring AMC usually works
        // for the vendor.
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null));
        await CreateAsync("AMC-001", end: Today.AddDays(20), vendorId: 1);

        (await RunWorkerAsync()).ShouldBe(1);

        var queued = fixture.Notifier.Queued.Single();
        queued.ToAddress.ShouldBe("support@acme.example");
        queued.SourceType.ShouldBe(EmailSource.Contract);
        queued.Subject.ShouldContain("expires in 20 days");
    }

    [Fact]
    public async Task A_named_recipient_wins_over_the_vendor()
    {
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, "facilities@fujitec.co.in"));
        await CreateAsync("AMC-001", end: Today.AddDays(20), vendorId: 1);

        await RunWorkerAsync();

        fixture.Notifier.Queued.Single().ToAddress.ShouldBe("facilities@fujitec.co.in");
    }

    [Fact]
    public async Task The_same_reminder_does_not_go_out_twice()
    {
        // The job is idempotent because of the index, not because it remembers
        // having run.
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null));
        await CreateAsync("AMC-001", end: Today.AddDays(20), vendorId: 1);
        await RunWorkerAsync();
        fixture.Notifier.Reset();

        fixture.Clock.Advance(TimeSpan.FromDays(1));

        (await RunWorkerAsync()).ShouldBe(0);
        fixture.Notifier.Queued.ShouldBeEmpty();
    }

    [Fact]
    public async Task Each_window_fires_in_turn()
    {
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null), (7, null));
        await CreateAsync("AMC-001", end: Today.AddDays(20), vendorId: 1);

        (await RunWorkerAsync()).ShouldBe(1);

        fixture.Clock.Advance(TimeSpan.FromDays(15));
        (await RunWorkerAsync()).ShouldBe(1);

        (await LogAsync()).Select(l => l.DaysBeforeExpiry).ShouldBe([30, 7]);
    }

    [Fact]
    public async Task A_missed_day_still_sends_rather_than_skipping_for_ever()
    {
        // The window opens at thirty days and stays open. A strict equality
        // check would lose the reminder entirely if the job did not run that
        // day.
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null));
        await CreateAsync("AMC-001", end: Today.AddDays(29), vendorId: 1);

        (await RunWorkerAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task A_renewed_contract_earns_its_reminders_again()
    {
        // R2-2: the log's unique key includes the expiry the reminder was
        // measured against, so a renewal is not permanently silent because it
        // was reminded about last year's date.
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null));
        var id = (await CreateAsync("AMC-001", end: Today.AddDays(20), vendorId: 1)).Value.Id;
        await RunWorkerAsync();

        await RenewAsync(id, Today.AddDays(385));
        fixture.Clock.Advance(TimeSpan.FromDays(360));
        fixture.Notifier.Reset();

        (await RunWorkerAsync()).ShouldBe(1);
        (await LogAsync()).Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_retired_contract_is_not_chased()
    {
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null));
        var id = (await CreateAsync("AMC-001", end: Today.AddDays(20), vendorId: 1)).Value.Id;
        await UpdateAsync(id, "Old", end: Today.AddDays(20), isDeleted: true);

        (await RunWorkerAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task An_expired_contract_is_not_chased()
    {
        // The windows are before expiry. Once it has gone, a reminder is a
        // report, not a warning.
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null));
        await CreateAsync("AMC-001", start: Today.AddDays(-400), end: Today.AddDays(-1), vendorId: 1);

        (await RunWorkerAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task A_contract_nobody_can_be_told_about_is_still_recorded()
    {
        // The window fired and reached nobody, which is a configuration problem
        // somebody has to see — and the row stops the worker rediscovering the
        // same silent window every day.
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null));
        await CreateAsync("AMC-001", end: Today.AddDays(20), vendorId: 2);

        (await RunWorkerAsync()).ShouldBe(1);

        var row = (await LogAsync()).Single();
        row.SentTo.ShouldBeNull();
        row.EmailOutboxId.ShouldBeNull();
        fixture.Notifier.Queued.ShouldBeEmpty();
        (await RunWorkerAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task With_no_windows_configured_nothing_is_chased()
    {
        await fixture.ResetAsync();
        await CreateAsync("AMC-001", end: Today.AddDays(1), vendorId: 1);

        (await RunWorkerAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task What_went_out_shows_on_the_contract()
    {
        await fixture.ResetAsync();
        await SetWindowsAsync(null, (30, null));
        var id = (await CreateAsync("AMC-001", end: Today.AddDays(20), vendorId: 1)).Value.Id;

        await RunWorkerAsync();

        var sent = (await GetAsync(id)).Value.SentReminders.Single();
        sent.DaysBeforeExpiry.ShouldBe(30);
        sent.Outcome.ShouldBe(ReminderOutcome.Queued);
        sent.SentTo.ShouldBe("support@acme.example");
    }

    // --------------------------------------------------------------- plumbing

    private DateOnly Today => DateOnly.FromDateTime(fixture.Clock.UtcNow);

    private Task<Result<CreateContractResponse>> CreateAsync(
        string number,
        string? name = null,
        string type = ContractType.Amc,
        int? vendorId = null,
        DateOnly? start = null,
        DateOnly? end = null,
        string? licenceKey = null,
        IReadOnlyList<int>? assetIds = null)
    {
        var handler = new CreateContractHandler(
            fixture.NewContext(), fixture.Vendors, fixture.Protector, fixture.Clock,
            fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new CreateContractCommand(
                number, name ?? number, type, vendorId,
                start ?? Today, end ?? Today.AddDays(365),
                null, null, licenceKey, false, null, assetIds ?? []),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateContractResponse>> UpdateAsync(
        int id,
        string name,
        DateOnly? end = null,
        bool isDeleted = false)
    {
        var handler = new UpdateContractHandler(
            fixture.NewContext(), fixture.Vendors, fixture.Protector, fixture.Clock,
            fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new UpdateContractCommand(
                id, name, null, Today.AddDays(-1), end ?? Today.AddDays(365),
                null, null, null, false, null, isDeleted),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<RenewContractResponse>> RenewAsync(
        int id, DateOnly newEnd, string? remarks = null)
    {
        var handler = new RenewContractHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new RenewContractCommand(id, newEnd, null, remarks),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<GetContractResponse>> GetAsync(int id)
    {
        var handler = new GetContractHandler(
            fixture.NewContext(), fixture.Vendors, fixture.Clock);

        return handler.HandleAsync(
            new GetContractQuery(id), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchContractsResponse>> SearchAsync(
        string? search = null,
        string? type = null,
        int? vendorId = null,
        int? expiringWithinDays = null,
        bool includeExpired = false)
    {
        var handler = new SearchContractsHandler(
            fixture.NewContext(), fixture.Vendors, fixture.Clock);

        return handler.HandleAsync(
            new SearchContractsQuery(
                search, type, vendorId, expiringWithinDays, includeExpired, 0, 50),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SetContractAssetsResponse>> SetAssetsAsync(
        int id, IReadOnlyList<int> assetIds)
    {
        var handler = new SetContractAssetsHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new SetContractAssetsCommand(id, assetIds), TestContext.Current.CancellationToken);
    }

    private Task<Result<AddContractDocumentResponse>> AddDocumentAsync(
        int id, string path = @"\\files\contracts\agreement.pdf")
    {
        var handler = new AddContractDocumentHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new AddContractDocumentCommand(
                id, path, Path.GetFileName(path), "application/pdf", 2048),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SetReminderWindowsResponse>> SetWindowsAsync(
        int? contractId, params (int Days, string? Recipients)[] windows)
    {
        var handler = new SetReminderWindowsHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new SetReminderWindowsCommand(
                contractId,
                [.. windows.Select(w => new SetReminderWindowsCommand.Window(
                    w.Days, w.Recipients, ReminderChannel.Email))]),
            TestContext.Current.CancellationToken);
    }

    private Task<int> RunWorkerAsync()
    {
        var worker = new ContractReminderWorker(
            fixture.NewContext(), fixture.Notifier, fixture.Vendors, fixture.Clock);

        return worker.RunAsync(TestContext.Current.CancellationToken);
    }

    private async Task<DateTime> LinkedOnAsync(int contractId, int assetId)
    {
        await using var db = fixture.NewContext();

        return await db.ContractAssets
            .Where(a => a.ContractId == contractId && a.AssetId == assetId)
            .Select(a => a.LinkedOnUtc)
            .SingleAsync(TestContext.Current.CancellationToken);
    }

    private async Task<List<ContractReminderLog>> LogAsync()
    {
        await using var db = fixture.NewContext();

        return await db.ContractReminderLogs
            .OrderBy(l => l.Id)
            .ToListAsync(TestContext.Current.CancellationToken);
    }
}
