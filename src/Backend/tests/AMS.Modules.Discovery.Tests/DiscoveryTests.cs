using AMS.Modules.Discovery.Agents;
using AMS.Modules.Discovery.Domain;
using AMS.Modules.Discovery.Features.IssueAgentKey;
using AMS.Modules.Discovery.Features.ReportInventory;
using AMS.Modules.Discovery.Features.ResolveDiscoveredDevice;
using AMS.Modules.Discovery.Features.RevokeAgentKey;
using AMS.Modules.Discovery.Features.SearchAgentKeys;
using AMS.Modules.Discovery.Features.SearchAssetHealth;
using AMS.Modules.Discovery.Features.SearchDiscoveredDevices;
using AMS.Modules.Discovery.Features.SearchInstalledSoftware;
using AMS.Modules.Discovery.Features.SetSoftwareCatalogEntry;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Discovery.Tests;

/// <summary>
/// Agent keys, the device queue, health, and software against the catalogue.
/// </summary>
[Collection(nameof(DiscoveryCollectionDefinition))]
public sealed class DiscoveryTests(DiscoveryFixture fixture)
{
    // ------------------------------------------------------------ the keys

    [Fact]
    public async Task A_key_is_shown_once_and_stored_as_a_hash()
    {
        // The point of a hash: an administrator who loses the key issues
        // another, and nobody with database access can read what the agents
        // are using.
        await fixture.ResetAsync();

        var issued = await IssueKeyAsync("Chennai rollout");

        issued.Value.Key.ShouldNotBeNullOrWhiteSpace();
        issued.Value.KeyPrefix.ShouldBe(issued.Value.Key[..AgentKeys.PrefixLength]);

        await using var db = fixture.NewContext();
        var stored = await db.AgentApiKeys.SingleAsync(TestContext.Current.CancellationToken);
        stored.KeyHash.ShouldNotBe(issued.Value.Key);
        AgentKeys.Matches(issued.Value.Key, stored.KeyHash).ShouldBeTrue();
    }

    [Fact]
    public async Task Two_keys_are_never_the_same()
    {
        await fixture.ResetAsync();

        var first = await IssueKeyAsync("One");
        var second = await IssueKeyAsync("Two");

        second.Value.Key.ShouldNotBe(first.Value.Key);
    }

    [Fact]
    public async Task The_list_never_shows_a_secret()
    {
        await fixture.ResetAsync();
        var issued = await IssueKeyAsync("Chennai rollout");

        var row = (await SearchKeysAsync()).Value.Rows.Single();

        row.KeyPrefix.ShouldBe(issued.Value.KeyPrefix);
        row.GetType().GetProperty("KeyHash").ShouldBeNull();
        row.GetType().GetProperty("Key").ShouldBeNull();
    }

    [Fact]
    public async Task A_revoked_key_keeps_its_row()
    {
        // LastUsedOnUtc on a revoked key is how somebody answers "was this ever
        // used, and until when" after a laptop went missing.
        await fixture.ResetAsync();
        var id = (await IssueKeyAsync("Old rollout")).Value.Id;

        await RevokeKeyAsync(id);

        var row = (await SearchKeysAsync()).Value.Rows.Single();
        row.IsActive.ShouldBeFalse();
        row.RevokedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task A_key_cannot_be_revoked_twice()
    {
        await fixture.ResetAsync();
        var id = (await IssueKeyAsync("Old rollout")).Value.Id;
        await RevokeKeyAsync(id);

        (await RevokeKeyAsync(id)).Error!.Code.ShouldBe("AgentApiKey.AlreadyRevoked");
    }

    [Fact]
    public async Task An_unknown_key_cannot_be_revoked()
    {
        await fixture.ResetAsync();

        (await RevokeKeyAsync(987654)).Error!.Code.ShouldBe("AgentApiKey.NotFound");
    }

    // ------------------------------------------------------ the agent post

    [Fact]
    public async Task A_report_with_no_key_is_rejected()
    {
        await fixture.ResetAsync();

        (await ReportAsync("LAPTOP-01", apiKey: null)).Error!.Code
            .ShouldBe("Agent.KeyRejected");
    }

    [Fact]
    public async Task A_report_with_an_unknown_key_is_rejected()
    {
        await fixture.ResetAsync();
        await IssueKeyAsync("Chennai rollout");

        (await ReportAsync("LAPTOP-01", apiKey: "not-a-real-key-at-all")).Error!.Code
            .ShouldBe("Agent.KeyRejected");
    }

    [Fact]
    public async Task A_report_with_a_revoked_key_is_rejected()
    {
        await fixture.ResetAsync();
        var issued = await IssueKeyAsync("Old rollout");
        await RevokeKeyAsync(issued.Value.Id);

        (await ReportAsync("LAPTOP-01", apiKey: issued.Value.Key)).Error!.Code
            .ShouldBe("Agent.KeyRejected");
    }

    [Fact]
    public async Task Every_rejection_says_the_same_thing()
    {
        // Telling an agent WHICH failure it was would tell anybody probing the
        // endpoint which of their guesses was closest.
        await fixture.ResetAsync();
        var issued = await IssueKeyAsync("Old rollout");
        await RevokeKeyAsync(issued.Value.Id);

        var noKey = await ReportAsync("LAPTOP-01", apiKey: null);
        var wrongKey = await ReportAsync("LAPTOP-01", apiKey: "wrong-key-entirely");
        var deadKey = await ReportAsync("LAPTOP-01", apiKey: issued.Value.Key);

        noKey.Error!.Message.ShouldBe(wrongKey.Error!.Message);
        wrongKey.Error.Message.ShouldBe(deadKey.Error!.Message);
    }

    [Fact]
    public async Task A_good_key_records_a_new_machine_in_the_queue()
    {
        // An agent that created assets would fill the register with contractor
        // laptops and test rigs.
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;

        var reported = await ReportAsync("LAPTOP-01", apiKey: key, serial: "SN-001");

        reported.IsSuccess.ShouldBeTrue();
        reported.Value.IsNewDevice.ShouldBeTrue();
        reported.Value.Status.ShouldBe(DiscoveredDeviceStatus.New);
        reported.Value.LinkedAssetId.ShouldBeNull();
    }

    [Fact]
    public async Task Using_a_key_stamps_when_it_was_last_used()
    {
        // A key nobody has used is a rollout that did not happen.
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;

        await ReportAsync("LAPTOP-01", apiKey: key);

        (await SearchKeysAsync()).Value.Rows.Single().LastUsedOnUtc.ShouldNotBeNull();
    }

    [Fact]
    public async Task The_same_machine_reporting_again_updates_rather_than_duplicates()
    {
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;
        await ReportAsync("LAPTOP-01", apiKey: key, serial: "SN-001");

        fixture.Clock.Advance(TimeSpan.FromHours(6));
        var second = await ReportAsync("LAPTOP-01", apiKey: key, serial: "SN-001", model: "X13");

        second.Value.IsNewDevice.ShouldBeFalse();
        var devices = (await SearchDevicesAsync()).Value;
        devices.TotalCount.ShouldBe(1);
        devices.Rows.Single().Model.ShouldBe("X13");
    }

    [Fact]
    public async Task A_renamed_machine_is_matched_on_its_serial()
    {
        // A machine keeps its serial when somebody renames it, and hostnames
        // get reused — the laptop handed to the next joiner is often called the
        // same thing.
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;
        await ReportAsync("LAPTOP-01", apiKey: key, serial: "SN-001");

        var renamed = await ReportAsync("R-NAIR-LAPTOP", apiKey: key, serial: "SN-001");

        renamed.Value.IsNewDevice.ShouldBeFalse();
        (await SearchDevicesAsync()).Value.Rows.Single().Hostname.ShouldBe("LAPTOP-01");
    }

    [Fact]
    public async Task Two_machines_with_no_serial_are_told_apart_by_hostname()
    {
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;

        await ReportAsync("DESKTOP-A", apiKey: key);
        await ReportAsync("DESKTOP-B", apiKey: key);

        (await SearchDevicesAsync()).Value.TotalCount.ShouldBe(2);
    }

    // ----------------------------------------------------------- the queue

    [Fact]
    public async Task A_machine_can_be_linked_to_an_asset()
    {
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;
        var deviceId = (await ReportAsync("LAPTOP-01", apiKey: key)).Value.DiscoveredDeviceId;

        var resolved = await ResolveAsync(deviceId, DiscoveredDeviceStatus.Linked, assetId: 42);

        resolved.Value.Status.ShouldBe(DiscoveredDeviceStatus.Linked);
        resolved.Value.LinkedAssetId.ShouldBe(42);
        (await SearchDevicesAsync()).Value.UnresolvedCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_machine_can_be_ignored()
    {
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;
        var deviceId = (await ReportAsync("CONTRACTOR-PC", apiKey: key)).Value.DiscoveredDeviceId;

        var resolved = await ResolveAsync(deviceId, DiscoveredDeviceStatus.Ignored);

        resolved.Value.Status.ShouldBe(DiscoveredDeviceStatus.Ignored);
        resolved.Value.LinkedAssetId.ShouldBeNull();
    }

    [Fact]
    public async Task Linking_without_an_asset_is_refused()
    {
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;
        var deviceId = (await ReportAsync("LAPTOP-01", apiKey: key)).Value.DiscoveredDeviceId;

        (await ResolveAsync(deviceId, DiscoveredDeviceStatus.Linked)).Error!.Code
            .ShouldBe("DiscoveredDevice.AssetRequired");
    }

    [Fact]
    public async Task Ignoring_with_an_asset_is_refused()
    {
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;
        var deviceId = (await ReportAsync("LAPTOP-01", apiKey: key)).Value.DiscoveredDeviceId;

        (await ResolveAsync(deviceId, DiscoveredDeviceStatus.Ignored, assetId: 42)).Error!.Code
            .ShouldBe("DiscoveredDevice.AssetNotAllowed");
    }

    [Fact]
    public async Task A_device_cannot_be_put_back_in_the_queue()
    {
        // New is what the agent sets, not a decision somebody makes.
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;
        var deviceId = (await ReportAsync("LAPTOP-01", apiKey: key)).Value.DiscoveredDeviceId;

        (await ResolveAsync(deviceId, DiscoveredDeviceStatus.New)).Error!.Code
            .ShouldBe("DiscoveredDevice.UnknownStatus");
    }

    [Fact]
    public async Task A_linked_machine_reports_against_its_asset_from_then_on()
    {
        await fixture.ResetAsync();
        var key = (await IssueKeyAsync("Chennai rollout")).Value.Key;
        var deviceId = (await ReportAsync("LAPTOP-01", apiKey: key)).Value.DiscoveredDeviceId;
        await ResolveAsync(deviceId, DiscoveredDeviceStatus.Linked, assetId: 42);

        var again = await ReportAsync("LAPTOP-01", apiKey: key, health: Health(cpu: 20, drive: 80));

        again.Value.LinkedAssetId.ShouldBe(42);
        (await SearchHealthAsync()).Value.Rows.Single().AssetId.ShouldBe(42);
    }

    // ---------------------------------------------------------- the health

    [Fact]
    public async Task Health_is_one_current_row_with_history_beside_it()
    {
        // A screen asking "how is this machine" wants one answer; a drive that
        // has been filling for a month is a different problem from one that
        // filled yesterday, and only the trend tells them apart.
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);

        await ReportAsync("LAPTOP-01", apiKey: key, health: Health(cpu: 10, drive: 60));
        fixture.Clock.Advance(TimeSpan.FromDays(1));
        await ReportAsync("LAPTOP-01", apiKey: key, health: Health(cpu: 90, drive: 95));

        var current = (await SearchHealthAsync()).Value.Rows.Single();
        current.SystemDrivePercent.ShouldBe(95m);

        await using var db = fixture.NewContext();
        (await db.AssetHealthHistories.CountAsync(TestContext.Current.CancellationToken))
            .ShouldBe(2);
    }

    [Fact]
    public async Task The_health_screen_puts_the_fullest_drive_first()
    {
        // The reading that turns into a ticket.
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key, health: Health(drive: 55));
        var second = await LinkedMachineAsync(assetId: 43, hostname: "LAPTOP-02", key: key);
        await ReportAsync("LAPTOP-02", apiKey: key, serial: "SN-002", health: Health(drive: 97));

        (await SearchHealthAsync()).Value.Rows[0].AssetId.ShouldBe(43);
    }

    [Fact]
    public async Task Machines_that_have_gone_quiet_can_be_found()
    {
        // Off, lost, or the agent was removed — all three worth knowing.
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key, health: Health());

        fixture.Clock.Advance(TimeSpan.FromDays(10));

        var quiet = (await SearchHealthAsync(notSeenForHours: 24)).Value;
        quiet.Rows.Single().HoursSinceSeen.ShouldBe(240);
        (await SearchHealthAsync(notSeenForHours: 480)).Value.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task A_report_with_no_health_records_none()
    {
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);

        await ReportAsync("LAPTOP-01", apiKey: key);

        (await SearchHealthAsync()).Value.Rows.ShouldBeEmpty();
    }

    // -------------------------------------------------------- the software

    [Fact]
    public async Task Installed_software_is_recorded_against_the_asset()
    {
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);

        var reported = await ReportAsync(
            "LAPTOP-01", apiKey: key,
            software: [("Design Studio", "3.1", "Acme"), ("Reader", "9", "Acme")]);

        reported.Value.SoftwareRecorded.ShouldBe(2);
        (await SearchSoftwareAsync()).Value.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Uninstalled_software_is_marked_removed_not_deleted()
    {
        // A licence audit asks what was installed during a period, not what is
        // installed today.
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key,
            software: [("Design Studio", "3.1", "Acme"), ("Reader", "9", "Acme")]);

        var second = await ReportAsync("LAPTOP-01", apiKey: key,
            software: [("Reader", "9", "Acme")]);

        second.Value.SoftwareRemoved.ShouldBe(1);
        (await SearchSoftwareAsync()).Value.Rows.Count.ShouldBe(1);
        (await SearchSoftwareAsync(includeRemoved: true)).Value.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Software_that_comes_back_is_un_removed_rather_than_duplicated()
    {
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key, software: [("Design Studio", "3.1", "Acme")]);
        await ReportAsync("LAPTOP-01", apiKey: key, software: [("Reader", "9", "Acme")]);

        await ReportAsync("LAPTOP-01", apiKey: key,
            software: [("Design Studio", "3.1", "Acme"), ("Reader", "9", "Acme")]);

        await using var db = fixture.NewContext();
        (await db.AssetInstalledSoftwares.CountAsync(TestContext.Current.CancellationToken))
            .ShouldBe(2);
        (await SearchSoftwareAsync()).Value.Rows.Count.ShouldBe(2);
    }

    [Fact]
    public async Task An_agent_that_reports_no_software_is_not_read_as_a_mass_uninstall()
    {
        // An agent that could not enumerate software sends nothing, and nothing
        // must not mean "everything was removed".
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key, software: [("Design Studio", "3.1", "Acme")]);

        var quiet = await ReportAsync("LAPTOP-01", apiKey: key);

        quiet.Value.SoftwareRemoved.ShouldBe(0);
        (await SearchSoftwareAsync()).Value.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task A_title_is_counted_by_machine_not_by_row()
    {
        // Two versions on one laptop is one seat. Counting rows would make
        // every upgrade look like a licence breach.
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);

        await ReportAsync("LAPTOP-01", apiKey: key,
            software: [("Design Studio", "3.1", "Acme"), ("Design Studio", "3.2", "Acme")]);

        (await SearchSoftwareAsync()).Value.Rows.Single().InstalledCount.ShouldBe(1);
    }

    [Fact]
    public async Task A_title_nobody_has_catalogued_is_undecided_not_unlicensed()
    {
        // Showing the two the same way would make every new title look like a
        // breach, which is how a compliance screen stops being read.
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key, software: [("Design Studio", "3.1", "Acme")]);

        var row = (await SearchSoftwareAsync()).Value.Rows.Single();

        row.IsInCatalogue.ShouldBeFalse();
        row.IsOverLicensed.ShouldBeFalse();
        row.LicensedSeats.ShouldBeNull();
    }

    [Fact]
    public async Task A_title_on_more_machines_than_seats_is_over_licensed()
    {
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key, software: [("Design Studio", "3.1", "Acme")]);
        await LinkedMachineAsync(assetId: 43, hostname: "LAPTOP-02", key: key);
        await ReportAsync("LAPTOP-02", apiKey: key, serial: "SN-002",
            software: [("Design Studio", "3.1", "Acme")]);

        await SetCatalogueAsync("Design Studio", seats: 1);

        var page = (await SearchSoftwareAsync()).Value;
        page.Rows.Single().IsOverLicensed.ShouldBeTrue();
        page.OverLicensedTitleCount.ShouldBe(1);
    }

    [Fact]
    public async Task Blacklisted_software_is_counted_by_installation()
    {
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key, software: [("Torrent Client", "1", null)]);
        await SetCatalogueAsync("Torrent Client", blacklisted: true);

        var page = (await SearchSoftwareAsync()).Value;

        page.BlacklistedInstallCount.ShouldBe(1);
        (await SearchSoftwareAsync(blacklistedOnly: true)).Value.Rows.Single()
            .SoftwareName.ShouldBe("Torrent Client");
    }

    [Fact]
    public async Task The_catalogue_reports_what_is_installed_when_it_is_set()
    {
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key, software: [("Design Studio", "3.1", "Acme")]);

        var entry = await SetCatalogueAsync("Design Studio", seats: 5);

        entry.Value.InstalledCount.ShouldBe(1);
        entry.Value.IsOverLicensed.ShouldBeFalse();
    }

    [Fact]
    public async Task Cataloguing_the_same_title_again_edits_it()
    {
        // The name is what the agent reports and there is nothing else to match
        // on, so this is an upsert.
        await fixture.ResetAsync();
        await SetCatalogueAsync("Design Studio", seats: 5);

        var second = await SetCatalogueAsync("Design Studio", seats: 10);

        second.Value.LicensedSeats.ShouldBe(10);

        await using var db = fixture.NewContext();
        (await db.SoftwareCatalogs.CountAsync(TestContext.Current.CancellationToken)).ShouldBe(1);
    }

    [Fact]
    public async Task Software_can_be_narrowed_to_one_machine()
    {
        await fixture.ResetAsync();
        var key = await LinkedMachineAsync(assetId: 42);
        await ReportAsync("LAPTOP-01", apiKey: key, software: [("Design Studio", "3.1", "Acme")]);
        await LinkedMachineAsync(assetId: 43, hostname: "LAPTOP-02", key: key);
        await ReportAsync("LAPTOP-02", apiKey: key, serial: "SN-002",
            software: [("Reader", "9", "Acme")]);

        (await SearchSoftwareAsync(assetId: 43)).Value.Rows.Single()
            .SoftwareName.ShouldBe("Reader");
    }

    // --------------------------------------------------------------- plumbing

    private static ReportInventoryCommand.HealthReading Health(
        decimal cpu = 15, decimal memory = 40, decimal drive = 50) =>
        new(cpu, memory, drive, 88, 12, "r.nair");

    private Task<Result<IssueAgentKeyResponse>> IssueKeyAsync(string name)
    {
        var handler = new IssueAgentKeyHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new IssueAgentKeyCommand(name), TestContext.Current.CancellationToken);
    }

    private Task<Result<RevokeAgentKeyResponse>> RevokeKeyAsync(int id)
    {
        var handler = new RevokeAgentKeyHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new RevokeAgentKeyCommand(id), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchAgentKeysResponse>> SearchKeysAsync()
    {
        var handler = new SearchAgentKeysHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchAgentKeysQuery(false), TestContext.Current.CancellationToken);
    }

    private Task<Result<ReportInventoryResponse>> ReportAsync(
        string hostname,
        string? apiKey,
        string? serial = null,
        string? model = null,
        ReportInventoryCommand.HealthReading? health = null,
        IReadOnlyList<(string Name, string? Version, string? Publisher)>? software = null)
    {
        var handler = new ReportInventoryHandler(fixture.NewContext(), fixture.Clock);

        return handler.HandleAsync(
            new ReportInventoryCommand(
                apiKey, hostname, serial, "Acme", model, "Windows 11", null, null, health,
                [.. (software ?? []).Select(s =>
                    new ReportInventoryCommand.SoftwareEntry(s.Name, s.Version, s.Publisher))],
                null),
            TestContext.Current.CancellationToken);
    }

    /// <summary>A machine already linked to an asset, and the key that reports it.</summary>
    private async Task<string> LinkedMachineAsync(
        int assetId, string hostname = "LAPTOP-01", string? key = null)
    {
        key ??= (await IssueKeyAsync("Chennai rollout")).Value.Key;

        var serial = hostname == "LAPTOP-01" ? "SN-001" : "SN-002";
        var reported = await ReportAsync(hostname, apiKey: key, serial: serial);

        await ResolveAsync(
            reported.Value.DiscoveredDeviceId, DiscoveredDeviceStatus.Linked, assetId);

        return key;
    }

    private Task<Result<ResolveDiscoveredDeviceResponse>> ResolveAsync(
        int id, string status, int? assetId = null)
    {
        var handler = new ResolveDiscoveredDeviceHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser);

        return handler.HandleAsync(
            new ResolveDiscoveredDeviceCommand(id, status, assetId),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchDiscoveredDevicesResponse>> SearchDevicesAsync()
    {
        var handler = new SearchDiscoveredDevicesHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchDiscoveredDevicesQuery(null, null, false, 0, 50),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchAssetHealthResponse>> SearchHealthAsync(int? notSeenForHours = null)
    {
        var handler = new SearchAssetHealthHandler(fixture.NewContext(), fixture.Clock);

        return handler.HandleAsync(
            new SearchAssetHealthQuery(null, null, notSeenForHours, 0, 50),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchInstalledSoftwareResponse>> SearchSoftwareAsync(
        int? assetId = null,
        bool blacklistedOnly = false,
        bool includeRemoved = false)
    {
        var handler = new SearchInstalledSoftwareHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchInstalledSoftwareQuery(
                null, assetId, blacklistedOnly, false, includeRemoved),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SetSoftwareCatalogEntryResponse>> SetCatalogueAsync(
        string name, int? seats = null, bool blacklisted = false)
    {
        var handler = new SetSoftwareCatalogEntryHandler(
            fixture.NewContext(), fixture.Clock, fixture.CurrentUser, fixture.SqlErrors);

        return handler.HandleAsync(
            new SetSoftwareCatalogEntryCommand(name, "Acme", seats, null, blacklisted, true),
            TestContext.Current.CancellationToken);
    }
}
