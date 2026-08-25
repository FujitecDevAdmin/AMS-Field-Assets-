using AMS.Modules.Verification.Domain;
using AMS.Modules.Assets.PublicApi;
using AMS.Modules.Verification.Features.CloseVerificationCycle;
using AMS.Modules.Verification.Features.OpenVerificationCycle;
using AMS.Modules.Verification.Features.SearchVerificationCycles;
using AMS.Modules.Verification.Features.SearchVerifications;
using AMS.Modules.Verification.Features.SubmitVerification;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Verification.Tests;

/// <summary>
/// Verification cycles, and the endpoint a phone posts to hours later and
/// sometimes twice.
/// </summary>
[Collection(nameof(VerificationCollectionDefinition))]
public sealed class VerificationTests(VerificationFixture fixture)
{
    // -------------------------------------------------------- the cycle

    [Fact]
    public async Task A_cycle_can_be_opened_and_listed()
    {
        await fixture.ResetAsync();

        var opened = await OpenCycleAsync("Q2 2026");

        opened.IsSuccess.ShouldBeTrue();
        var row = (await SearchCyclesAsync()).Value.Rows.Single();
        row.CycleName.ShouldBe("Q2 2026");
        row.IsActive.ShouldBeTrue();
        row.VerifiedCount.ShouldBe(0);
    }

    [Fact]
    public async Task Multiple_cycles_can_be_open_for_the_same_branch()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await OpenCycleAsync("Q3 2026")).IsSuccess.ShouldBeTrue();
        (await SearchCyclesAsync()).Value.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task A_closed_cycle_does_not_prevent_a_later_cycle()
    {
        await fixture.ResetAsync();
        var id = (await OpenCycleAsync("Q2 2026")).Value.Id;

        await CloseCycleAsync(id);

        (await OpenCycleAsync("Q3 2026")).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task Two_cycles_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        var id = (await OpenCycleAsync("Q2 2026")).Value.Id;
        await CloseCycleAsync(id);

        (await OpenCycleAsync("Q2 2026")).Error!.Code.ShouldBe("VerificationCycle.NameTaken");
    }

    [Fact]
    public async Task A_cycle_cannot_end_before_it_starts()
    {
        await fixture.ResetAsync();

        var result = await OpenCycleAsync(
            "Backwards",
            start: new DateOnly(2026, 6, 30),
            end: new DateOnly(2026, 4, 1));

        result.Error!.Code.ShouldBe("VerificationCycle.Window");
    }

    [Fact]
    public async Task A_closed_cycle_cannot_be_closed_again()
    {
        await fixture.ResetAsync();
        var id = (await OpenCycleAsync("Q2 2026")).Value.Id;
        await CloseCycleAsync(id);

        (await CloseCycleAsync(id)).Error!.Code.ShouldBe("VerificationCycle.AlreadyClosed");
    }

    [Fact]
    public async Task Closing_reports_what_the_round_found()
    {
        await fixture.ResetAsync();
        var id = (await OpenCycleAsync("Q2 2026")).Value.Id;
        await SubmitAsync(10);
        await SubmitAsync(11, condition: WorkingCondition.Missing);

        var closed = await CloseCycleAsync(id);

        closed.Value.VerifiedCount.ShouldBe(2);
        closed.Value.ExceptionCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_cycle_that_does_not_exist_cannot_be_closed()
    {
        await fixture.ResetAsync();

        (await CloseCycleAsync(987654)).Error!.Code.ShouldBe("VerificationCycle.NotFound");
    }

    // ------------------------------------------------------ the sighting

    [Fact]
    public async Task A_sighting_is_recorded_against_the_open_cycle()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        var submitted = await SubmitAsync(10);

        submitted.IsSuccess.ShouldBeTrue();
        submitted.Value.AssetNumber.ShouldBe("AMS-000010");
        submitted.Value.WasAlreadyRecorded.ShouldBeFalse();
    }

    [Fact]
    public async Task Nothing_can_be_recorded_with_no_cycle_open()
    {
        await fixture.ResetAsync();

        (await SubmitAsync(10)).Error!.Code.ShouldBe("VerificationCycle.NotActive");
    }

    [Fact]
    public async Task An_unassigned_auditor_cannot_submit_to_an_audit()
    {
        await fixture.ResetAsync();
        var cycleId = (await OpenCycleAsync("Assigned audit")).Value.Id;
        fixture.CurrentUser.Id = 99;

        (await SubmitAsync(10, cycleId: cycleId)).Error!.Code
            .ShouldBe("VerificationCycle.NotAssigned");
    }

    [Fact]
    public async Task An_assigned_auditor_cannot_verify_an_asset_from_another_branch()
    {
        await fixture.ResetAsync();
        var cycleId = (await OpenCycleAsync("Branch audit")).Value.Id;
        fixture.Assets.Add(new AssetSnapshot(
            99, "AMS-000099", null, null, null, null, false, ImportedBranch: "Branch 2"));

        (await SubmitAsync(99, cycleId: cycleId)).Error!.Code
            .ShouldBe("Verification.AssetOutsideAuditBranch");
    }

    [Fact]
    public async Task An_asset_that_does_not_exist_cannot_be_verified()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await SubmitAsync(987654)).Error!.Code.ShouldBe("Asset.NotFound");
    }

    [Fact]
    public async Task A_condition_the_database_does_not_allow_is_refused()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await SubmitAsync(10, condition: "Fine")).Error!.Code
            .ShouldBe("Verification.UnknownCondition");
    }

    [Fact]
    public async Task The_phones_time_is_kept_not_the_servers()
    {
        // The capture happened when the technician was standing in front of the
        // asset, not when the signal came back.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");
        var captured = fixture.Clock.UtcNow.AddHours(-6);

        await SubmitAsync(10, verifiedOnUtc: captured);

        (await SearchAsync()).Value.Rows.Single().VerifiedOnUtc.ShouldBe(captured);
    }

    [Fact]
    public async Task What_the_phone_saw_beats_what_the_register_says()
    {
        // The technician is standing in front of the thing. If the register
        // thinks it is elsewhere, the register is what is wrong.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        await SubmitAsync(10, locationId: 7, holderEmployeeId: 501);

        var row = (await SearchAsync()).Value.Rows.Single();
        row.LocationId.ShouldBe(7);
        row.HolderEmployeeId.ShouldBe(501);
    }

    [Fact]
    public async Task With_nothing_from_the_phone_the_register_fills_in()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        await SubmitAsync(10);

        var row = (await SearchAsync()).Value.Rows.Single();
        row.LocationId.ShouldBe(1);
        row.HolderEmployeeId.ShouldBe(500);
    }

    [Fact]
    public async Task A_tag_on_the_wrong_asset_is_recorded_rather_than_refused()
    {
        // The technician is standing in front of the thing, and the tag being
        // wrong IS the finding.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        var submitted = await SubmitAsync(10, scannedQr: "AMS-000099");

        submitted.IsSuccess.ShouldBeTrue();
        submitted.Value.HasQrMismatch.ShouldBeTrue();
    }

    [Fact]
    public async Task A_tag_that_matches_apart_from_case_and_spacing_is_not_a_mismatch()
    {
        // A QR reader returns what is printed, and printers add neither.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await SubmitAsync(10, scannedQr: "  ams-000010 ")).Value.HasQrMismatch.ShouldBeFalse();
    }

    [Fact]
    public async Task No_scan_at_all_is_not_a_mismatch()
    {
        // A verification typed in from the web has no tag to compare.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await SubmitAsync(10)).Value.HasQrMismatch.ShouldBeFalse();
    }

    // ------------------------------------------- a retry, not a conflict

    [Fact]
    public async Task The_same_capture_sent_twice_gives_back_the_same_row()
    {
        // R2-21. Calling every retry a conflict is how technicians learn to
        // ignore conflicts.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");
        var captureId = Guid.NewGuid();

        var first = await SubmitAsync(10, captureId: captureId);
        var retry = await SubmitAsync(10, captureId: captureId);

        retry.IsSuccess.ShouldBeTrue();
        retry.Value.WasAlreadyRecorded.ShouldBeTrue();
        retry.Value.Id.ShouldBe(first.Value.Id);
        (await SearchAsync()).Value.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task Somebody_else_verifying_first_is_a_conflict()
    {
        // The other 2601, and it deserves different words.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");
        await SubmitAsync(10, captureId: Guid.NewGuid());

        await AssignAuditorAsync(2);
        fixture.CurrentUser.Id = 2;
        var second = await SubmitAsync(10, captureId: Guid.NewGuid());

        second.Error!.Code.ShouldBe("Verification.AlreadyVerified");
    }

    [Fact]
    public async Task A_capture_recorded_in_an_earlier_cycle_does_not_block_this_one()
    {
        // The uniqueness is per cycle. Sighting the same asset next quarter is
        // the whole point of doing it quarterly.
        await fixture.ResetAsync();
        var first = (await OpenCycleAsync("Q2 2026")).Value.Id;
        await SubmitAsync(10);
        await CloseCycleAsync(first);
        await OpenCycleAsync("Q3 2026");

        (await SubmitAsync(10)).IsSuccess.ShouldBeTrue();
    }

    // ------------------------------------------------------ bulk counting

    [Fact]
    public async Task A_bulk_line_is_counted_with_a_number()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        var counted = await SubmitAsync(
            20, isBulk: true, counted: 48, expected: 50, locationId: 1);

        counted.IsSuccess.ShouldBeTrue();
        counted.Value.Variance.ShouldBe(-2);
    }

    [Fact]
    public async Task A_bulk_count_without_a_number_is_not_a_count()
    {
        // CK_PhysicalVerification_BulkHasCount says the same thing, as a 500.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await SubmitAsync(20, isBulk: true, locationId: 1)).Error!.Code
            .ShouldBe("Verification.CountRequired");
    }

    [Fact]
    public async Task A_bulk_count_has_to_say_where_it_was_counted()
    {
        // The uniqueness rule for a count is per PLACE. With no location,
        // counting the same line at four branches would look like one place
        // counted four times.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await SubmitAsync(20, isBulk: true, counted: 10)).Error!.Code
            .ShouldBe("Verification.CountNeedsPlace");
    }

    [Fact]
    public async Task The_same_line_can_be_counted_at_several_branches()
    {
        // R3 split the index for exactly this. Counting the same bulk line at
        // four branches is the correct answer, not a duplicate.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await SubmitAsync(20, isBulk: true, counted: 10, locationId: 1)).IsSuccess.ShouldBeTrue();
        (await SubmitAsync(20, isBulk: true, counted: 25, locationId: 2)).IsSuccess.ShouldBeTrue();

        (await SearchAsync()).Value.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task The_same_line_cannot_be_counted_twice_at_one_branch()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");
        await SubmitAsync(20, isBulk: true, counted: 10, locationId: 1);

        await AssignAuditorAsync(2);
        fixture.CurrentUser.Id = 2;
        var again = await SubmitAsync(20, isBulk: true, counted: 12, locationId: 1);

        again.Error!.Code.ShouldBe("Verification.AlreadyCounted");
    }

    [Fact]
    public async Task A_single_asset_is_sighted_not_counted()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await SubmitAsync(10, isBulk: true, counted: 1, locationId: 1)).Error!.Code
            .ShouldBe("Verification.NotBulk");
    }

    [Fact]
    public async Task A_quantity_on_a_sighting_is_refused()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");

        (await SubmitAsync(10, counted: 1)).Error!.Code.ShouldBe("Verification.CountOnSighting");
    }

    // --------------------------------------------------- the exceptions

    [Fact]
    public async Task The_report_puts_the_worst_first()
    {
        // A report that buries the missing assets under three hundred healthy
        // ones is a report nobody finishes reading.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");
        await SubmitAsync(10, condition: WorkingCondition.Damaged);
        await SubmitAsync(11, condition: WorkingCondition.Missing);
        await SubmitAsync(20, isBulk: true, counted: 5, locationId: 1);

        var rows = (await SearchAsync()).Value.Rows;

        rows[0].WorkingCondition.ShouldBe(WorkingCondition.Missing);
        rows[1].WorkingCondition.ShouldBe(WorkingCondition.Damaged);
    }

    [Fact]
    public async Task The_report_can_show_only_the_exceptions()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");
        await SubmitAsync(10);
        await SubmitAsync(11, condition: WorkingCondition.NotWorking);

        var page = (await SearchAsync(exceptionsOnly: true)).Value;

        page.Rows.Single().AssetId.ShouldBe(11);
        page.TotalCount.ShouldBe(1);
    }

    [Fact]
    public async Task The_report_can_show_only_the_mismatched_tags()
    {
        // Its own kind of wrong, and it can happen to something in perfect
        // condition.
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");
        await SubmitAsync(10, scannedQr: "AMS-000099");
        await SubmitAsync(11);

        (await SearchAsync(mismatchesOnly: true)).Value.Rows.Single().AssetId.ShouldBe(10);
    }

    [Fact]
    public async Task The_report_can_be_narrowed_to_a_branch_and_a_cycle()
    {
        await fixture.ResetAsync();
        var cycle = (await OpenCycleAsync("Q2 2026")).Value.Id;
        await SubmitAsync(10, locationId: 1);
        await SubmitAsync(11, locationId: 2);

        (await SearchAsync(locationId: 2)).Value.Rows.Single().AssetId.ShouldBe(11);
        (await SearchAsync(cycleId: cycle)).Value.TotalCount.ShouldBe(2);
        (await SearchAsync(cycleId: 987654)).Value.TotalCount.ShouldBe(0);
    }

    [Fact]
    public async Task The_exception_count_is_over_the_filter()
    {
        await fixture.ResetAsync();
        await OpenCycleAsync("Q2 2026");
        await SubmitAsync(10, condition: WorkingCondition.Missing);
        await SubmitAsync(11);

        var page = (await SearchAsync()).Value;

        page.TotalCount.ShouldBe(2);
        page.ExceptionCount.ShouldBe(1);
    }

    // --------------------------------------------------------------- plumbing

    private Task<Result<OpenVerificationCycleResponse>> OpenCycleAsync(
        string name, DateOnly? start = null, DateOnly? end = null)
    {
        var handler = new OpenVerificationCycleHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.Assets,
            fixture.Branches, fixture.SqlErrors);

        return handler.HandleAsync(
            new OpenVerificationCycleCommand(name, 1, start ?? default, end, [1], [1]),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<CloseVerificationCycleResponse>> CloseCycleAsync(int id)
    {
        var handler = new CloseVerificationCycleHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new CloseVerificationCycleCommand(id), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchVerificationCyclesResponse>> SearchCyclesAsync()
    {
        var handler = new SearchVerificationCyclesHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchVerificationCyclesQuery(false), TestContext.Current.CancellationToken);
    }

    private async Task AssignAuditorAsync(int userId)
    {
        await using var context = fixture.NewContext();
        var cycleId = await context.PhysicalVerificationCycles
            .Where(cycle => cycle.IsActive)
            .OrderByDescending(cycle => cycle.Id)
            .Select(cycle => cycle.Id)
            .FirstAsync(TestContext.Current.CancellationToken);
        context.PhysicalVerificationAssignments.Add(new PhysicalVerificationAssignment
        {
            PhysicalVerificationCycleId = cycleId,
            AuditorUserId = userId,
            AssignedOnUtc = fixture.Clock.UtcNow,
            AssignedBy = fixture.CurrentUser.Username,
        });
        await context.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private async Task<Result<SubmitVerificationResponse>> SubmitAsync(
        int assetId,
        Guid? captureId = null,
        bool isBulk = false,
        decimal? counted = null,
        decimal? expected = null,
        string? scannedQr = null,
        string condition = WorkingCondition.Good,
        int? locationId = null,
        int? holderEmployeeId = null,
        DateTime? verifiedOnUtc = null,
        int? cycleId = null)
    {
        await using var lookup = fixture.NewContext();
        var assignedCycleId = await lookup.PhysicalVerificationAssignments
            .Where(assignment => assignment.AuditorUserId == fixture.CurrentUser.Id)
            .Join(lookup.PhysicalVerificationCycles.Where(cycle => cycle.IsActive),
                assignment => assignment.PhysicalVerificationCycleId,
                cycle => cycle.Id,
                (_, cycle) => cycle.Id)
            .OrderByDescending(id => id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken);
        var handler = new SubmitVerificationHandler(
            fixture.NewContext(), fixture.Assets, fixture.Clock, fixture.CurrentUser, fixture.Branches,
            fixture.SqlErrors);

        return await handler.HandleAsync(
            new SubmitVerificationCommand(
                cycleId ?? (assignedCycleId == 0 ? int.MaxValue : assignedCycleId), assetId, captureId, isBulk, counted, expected, scannedQr, condition,
                false, null, null, null, null, null, locationId, holderEmployeeId, verifiedOnUtc, null),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchVerificationsResponse>> SearchAsync(
        int? cycleId = null,
        int? locationId = null,
        bool exceptionsOnly = false,
        bool mismatchesOnly = false)
    {
        var handler = new SearchVerificationsHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchVerificationsQuery(
                cycleId, locationId, null, exceptionsOnly, mismatchesOnly, 0, 50),
            TestContext.Current.CancellationToken);
    }
}
