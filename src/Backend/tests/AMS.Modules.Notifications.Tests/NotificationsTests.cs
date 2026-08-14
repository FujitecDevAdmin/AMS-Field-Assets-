using AMS.Modules.Notifications.Domain;
using AMS.Modules.Notifications.Features.CreateEmailSetting;
using AMS.Modules.Notifications.Features.MarkNotificationsRead;
using AMS.Modules.Notifications.Features.RequeueEmail;
using AMS.Modules.Notifications.Features.SearchEmailOutbox;
using AMS.Modules.Notifications.Features.SearchEmailSettings;
using AMS.Modules.Notifications.Features.SearchMyNotifications;
using AMS.Modules.Notifications.Features.UpdateEmailSetting;
using AMS.Modules.Notifications.PublicApi.Notifications;
using AMS.Modules.Notifications.Sending;
using AMS.SharedKernel.Results;
using Microsoft.Extensions.Logging.Abstractions;

namespace AMS.Modules.Notifications.Tests;

/// <summary>
/// The outbox, the bell, the SMTP profiles, and the dispatcher that drains the
/// queue.
/// </summary>
[Collection(nameof(NotificationsCollectionDefinition))]
public sealed class NotificationsTests(NotificationsFixture fixture)
{
    // -------------------------------------------------------- queuing

    [Fact]
    public async Task Queuing_writes_a_pending_message_and_returns_its_id()
    {
        await fixture.ResetAsync();

        var id = await QueueAsync("user@fujitec.co.in", "Your ticket");

        id.ShouldBeGreaterThan(0);
        var row = (await SearchOutboxAsync()).Value.Rows.Single();
        row.Id.ShouldBe(id);
        row.Status.ShouldBe(OutboxStatus.Pending);
        row.AttemptCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_queued_message_remembers_what_asked_for_it()
    {
        // Otherwise a bounced address is a row in a queue with nothing attached
        // to it.
        await fixture.ResetAsync();

        await QueueAsync("user@fujitec.co.in", "Your ticket",
            source: EmailSource.ServiceRequest, sourceId: 42);

        var row = (await SearchOutboxAsync(sourceType: EmailSource.ServiceRequest)).Value.Rows.Single();
        row.SourceId.ShouldBe(42);
    }

    [Fact]
    public async Task The_queue_can_be_searched_by_address_and_subject()
    {
        await fixture.ResetAsync();
        await QueueAsync("kumar@fujitec.co.in", "Printer replaced");
        await QueueAsync("iyer@fujitec.co.in", "VPN access");

        (await SearchOutboxAsync(search: "kumar")).Value.Rows.Single()
            .Subject.ShouldBe("Printer replaced");
        (await SearchOutboxAsync(search: "VPN")).Value.Rows.Single()
            .ToAddress.ShouldBe("iyer@fujitec.co.in");
    }

    [Fact]
    public async Task The_queue_counts_are_over_the_whole_queue_not_the_filter()
    {
        // A reader who has filtered to one ticket still wants to know the queue
        // is on fire.
        await fixture.ResetAsync();
        await QueueAsync("a@fujitec.co.in", "One", source: EmailSource.ServiceRequest, sourceId: 1);
        await QueueAsync("b@fujitec.co.in", "Two", source: EmailSource.Contract, sourceId: 2);

        var page = (await SearchOutboxAsync(sourceType: EmailSource.Contract)).Value;

        page.TotalCount.ShouldBe(1);
        page.PendingCount.ShouldBe(2);
    }

    // ----------------------------------------------------- the dispatcher

    [Fact]
    public async Task The_dispatcher_sends_what_is_waiting()
    {
        await fixture.ResetAsync();
        await CreateProfileAsync("Works");
        await QueueAsync("user@fujitec.co.in", "Your ticket");

        var attempted = await SendBatchAsync();

        attempted.ShouldBe(1);
        fixture.Transport.Sent.Single().Subject.ShouldBe("Your ticket");
        (await SearchOutboxAsync()).Value.Rows.Single().Status.ShouldBe(OutboxStatus.Sent);
    }

    [Fact]
    public async Task A_sent_message_records_when_the_server_accepted_it()
    {
        await fixture.ResetAsync();
        await CreateProfileAsync("Works");
        await QueueAsync("user@fujitec.co.in", "Your ticket");

        await SendBatchAsync();

        (await SearchOutboxAsync()).Value.Rows.Single().SentOnUtc.ShouldBe(fixture.Clock.UtcNow);
    }

    [Fact]
    public async Task The_dispatcher_sends_oldest_first()
    {
        await fixture.ResetAsync();
        await CreateProfileAsync("Works");
        await QueueAsync("first@fujitec.co.in", "First");
        fixture.Clock.Advance(TimeSpan.FromMinutes(5));
        await QueueAsync("second@fujitec.co.in", "Second");

        await SendBatchAsync();

        fixture.Transport.Sent.Select(m => m.Subject).ShouldBe(["First", "Second"]);
    }

    [Fact]
    public async Task A_failed_send_stays_queued_and_counts_the_attempt()
    {
        // The whole reason for an outbox: a dead SMTP host must not lose what
        // somebody wrote.
        await fixture.ResetAsync();
        await CreateProfileAsync("Broken");
        await QueueAsync("user@fujitec.co.in", "Your ticket");
        fixture.Transport.Fails = new InvalidOperationException("Connection refused.");

        await SendBatchAsync();

        var row = (await SearchOutboxAsync()).Value.Rows.Single();
        row.Status.ShouldBe(OutboxStatus.Pending);
        row.AttemptCount.ShouldBe(1);
        row.LastError.ShouldNotBeNull();
        row.LastError.ShouldContain("Connection refused");
    }

    [Fact]
    public async Task A_message_that_starts_working_is_sent_and_the_error_cleared()
    {
        await fixture.ResetAsync();
        await CreateProfileAsync("Flaky");
        await QueueAsync("user@fujitec.co.in", "Your ticket");
        fixture.Transport.FailuresRemaining = 2;

        await SendBatchAsync();
        await SendBatchAsync();
        await SendBatchAsync();

        var row = (await SearchOutboxAsync()).Value.Rows.Single();
        row.Status.ShouldBe(OutboxStatus.Sent);
        row.AttemptCount.ShouldBe(3);
        row.LastError.ShouldBeNull();
    }

    [Fact]
    public async Task A_message_is_given_up_on_after_enough_attempts()
    {
        // A message retried for ever is a message nobody ever looks at, and a
        // Failed row on a screen is the only thing that gets a wrong address
        // corrected.
        await fixture.ResetAsync();
        await CreateProfileAsync("Broken");
        await QueueAsync("wrong@nowhere.invalid", "Your ticket");
        fixture.Transport.Fails = new InvalidOperationException("No such mailbox.");

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await SendBatchAsync(maxAttempts: 3);
        }

        var row = (await SearchOutboxAsync()).Value.Rows.Single();
        row.Status.ShouldBe(OutboxStatus.Failed);
        row.AttemptCount.ShouldBe(3);
    }

    [Fact]
    public async Task A_failed_message_is_not_picked_up_again()
    {
        await fixture.ResetAsync();
        await CreateProfileAsync("Broken");
        await QueueAsync("wrong@nowhere.invalid", "Your ticket");
        fixture.Transport.Fails = new InvalidOperationException("No such mailbox.");
        await SendBatchAsync(maxAttempts: 1);

        var attempted = await SendBatchAsync(maxAttempts: 1);

        attempted.ShouldBe(0);
    }

    [Fact]
    public async Task With_no_profile_configured_nothing_is_sent_and_nothing_is_failed()
    {
        // A site that has not configured SMTP yet has a queue that will send the
        // moment it does. Burning the attempt counter meanwhile would exhaust
        // it before the first real try.
        await fixture.ResetAsync();
        await QueueAsync("user@fujitec.co.in", "Your ticket");

        var attempted = await SendBatchAsync();

        attempted.ShouldBe(0);
        var row = (await SearchOutboxAsync()).Value.Rows.Single();
        row.Status.ShouldBe(OutboxStatus.Pending);
        row.AttemptCount.ShouldBe(0);
    }

    [Fact]
    public async Task A_retired_profile_is_not_used()
    {
        await fixture.ResetAsync();
        var id = (await CreateProfileAsync("Old")).Value.Id;
        await UpdateProfileAsync(id, "Old", isActive: false);
        await QueueAsync("user@fujitec.co.in", "Your ticket");

        (await SendBatchAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task The_dispatcher_sends_through_the_default_profile()
    {
        await fixture.ResetAsync();
        await CreateProfileAsync("Secondary", host: "smtp2.fujitec.co.in");
        await CreateProfileAsync("Primary", host: "smtp1.fujitec.co.in", isDefault: true);
        await QueueAsync("user@fujitec.co.in", "Your ticket");

        await SendBatchAsync();

        fixture.Transport.LastProfile!.Host.ShouldBe("smtp1.fujitec.co.in");
    }

    [Fact]
    public async Task The_dispatcher_reads_back_the_password_the_settings_screen_wrote()
    {
        // Written protected by one slice and unprotected by another. A
        // pass-through fake would prove nothing about that.
        await fixture.ResetAsync();
        await CreateProfileAsync("Authenticated", username: "ams", password: "s3cret");
        await QueueAsync("user@fujitec.co.in", "Your ticket");

        await SendBatchAsync();

        fixture.Transport.LastProfile!.Password.ShouldBe("s3cret");
    }

    [Fact]
    public async Task A_batch_takes_no_more_than_its_size()
    {
        await fixture.ResetAsync();
        await CreateProfileAsync("Works");

        for (var i = 0; i < 5; i++)
        {
            await QueueAsync($"user{i}@fujitec.co.in", $"Message {i}");
        }

        (await SendBatchAsync(batchSize: 2)).ShouldBe(2);
        (await SearchOutboxAsync(status: OutboxStatus.Pending)).Value.Rows.Count.ShouldBe(3);
    }

    // -------------------------------------------------------- requeuing

    [Fact]
    public async Task A_failed_message_can_be_tried_again_with_a_full_set_of_attempts()
    {
        // Somebody requeues because they have fixed what stopped it. Giving it
        // one last try before it fails again would make the button useless in
        // exactly the case it exists for.
        await fixture.ResetAsync();
        await CreateProfileAsync("Broken");
        var id = await QueueAsync("user@fujitec.co.in", "Your ticket");
        fixture.Transport.Fails = new InvalidOperationException("Refused.");
        await SendBatchAsync(maxAttempts: 1);

        var requeued = await RequeueAsync(id);

        requeued.Value.Status.ShouldBe(OutboxStatus.Pending);
        requeued.Value.AttemptCount.ShouldBe(0);

        fixture.Transport.Fails = null;
        await SendBatchAsync();
        (await SearchOutboxAsync()).Value.Rows.Single().Status.ShouldBe(OutboxStatus.Sent);
    }

    [Fact]
    public async Task A_sent_message_cannot_be_requeued()
    {
        // It would send twice, and the person pressing the button would have no
        // way of knowing they had.
        await fixture.ResetAsync();
        await CreateProfileAsync("Works");
        var id = await QueueAsync("user@fujitec.co.in", "Your ticket");
        await SendBatchAsync();

        (await RequeueAsync(id)).Error!.Code.ShouldBe("EmailOutbox.AlreadySent");
    }

    [Fact]
    public async Task A_waiting_message_cannot_be_requeued()
    {
        await fixture.ResetAsync();
        var id = await QueueAsync("user@fujitec.co.in", "Your ticket");

        (await RequeueAsync(id)).Error!.Code.ShouldBe("EmailOutbox.AlreadyQueued");
    }

    [Fact]
    public async Task A_message_that_does_not_exist_cannot_be_requeued()
    {
        await fixture.ResetAsync();

        (await RequeueAsync(987654)).Error!.Code.ShouldBe("EmailOutbox.NotFound");
    }

    // ------------------------------------------------------ SMTP profiles

    [Fact]
    public async Task A_profile_can_be_created_and_listed_without_its_password()
    {
        // docs/03 §8: encrypted columns are excluded from any projection that
        // feeds a grid. The screen needs to know one is set, not what it is.
        await fixture.ResetAsync();

        await CreateProfileAsync("Primary", username: "ams", password: "s3cret");

        var row = (await SearchProfilesAsync()).Value.Rows.Single();
        row.HasPassword.ShouldBeTrue();
        row.GetType().GetProperty("Password").ShouldBeNull();
    }

    [Fact]
    public async Task Only_one_profile_can_be_the_default()
    {
        await fixture.ResetAsync();
        await CreateProfileAsync("First", isDefault: true);

        (await CreateProfileAsync("Second", isDefault: true)).Error!.Code
            .ShouldBe("EmailSetting.DefaultExists");
    }

    [Fact]
    public async Task Two_profiles_cannot_share_a_name()
    {
        await fixture.ResetAsync();
        await CreateProfileAsync("Primary");

        (await CreateProfileAsync("Primary")).Error!.Code.ShouldBe("EmailSetting.NameTaken");
    }

    [Fact]
    public async Task A_profile_with_a_username_needs_a_password()
    {
        // Otherwise the failure comes back from the mail server hours later as
        // an authentication error against a message somebody was waiting for.
        await fixture.ResetAsync();

        (await CreateProfileAsync("Half", username: "ams")).Error!.Code
            .ShouldBe("EmailSetting.PasswordRequired");
    }

    [Fact]
    public async Task Editing_a_profile_without_a_password_keeps_the_stored_one()
    {
        // The screen cannot show the stored password, so it cannot send it
        // back. Treating the blank field as a deletion would wipe it every time
        // somebody corrected the port.
        await fixture.ResetAsync();
        var id = (await CreateProfileAsync(
            "Primary", username: "ams", password: "s3cret")).Value.Id;

        await UpdateProfileAsync(id, "Primary", username: "ams", port: 587);

        (await SearchProfilesAsync()).Value.Rows.Single().HasPassword.ShouldBeTrue();
        await QueueAsync("user@fujitec.co.in", "Your ticket");
        await SendBatchAsync();
        fixture.Transport.LastProfile!.Password.ShouldBe("s3cret");
    }

    [Fact]
    public async Task Clearing_the_username_clears_the_password()
    {
        // A profile with neither sends unauthenticated, which is a real
        // configuration and a deliberate one.
        await fixture.ResetAsync();
        var id = (await CreateProfileAsync(
            "Primary", username: "ams", password: "s3cret")).Value.Id;

        await UpdateProfileAsync(id, "Primary", username: null);

        (await SearchProfilesAsync()).Value.Rows.Single().HasPassword.ShouldBeFalse();
    }

    [Fact]
    public async Task Retiring_a_profile_gives_up_the_default_slot()
    {
        await fixture.ResetAsync();
        var id = (await CreateProfileAsync("Primary", isDefault: true)).Value.Id;

        await UpdateProfileAsync(id, "Primary", isDefault: true, isActive: false);

        var row = (await SearchProfilesAsync()).Value.Rows.Single();
        row.IsDefault.ShouldBeFalse();
        (await CreateProfileAsync("Replacement", isDefault: true)).IsSuccess.ShouldBeTrue();
    }

    [Fact]
    public async Task An_unknown_profile_cannot_be_edited()
    {
        await fixture.ResetAsync();

        (await UpdateProfileAsync(987654, "Ghost")).Error!.Code.ShouldBe("EmailSetting.NotFound");
    }

    // ------------------------------------------------------------- the bell

    [Fact]
    public async Task A_notification_lands_in_one_persons_list()
    {
        await fixture.ResetAsync();

        await NotifyAsync(7, "Your request was approved.");

        (await MyNotificationsAsync(7)).Value.Rows.Single().Text
            .ShouldBe("Your request was approved.");
        (await MyNotificationsAsync(8)).Value.Rows.ShouldBeEmpty();
    }

    [Fact]
    public async Task The_bell_counts_everything_unread_not_the_page()
    {
        // A bell showing "3" while there are forty is a bell nobody believes
        // twice.
        await fixture.ResetAsync();

        for (var i = 0; i < 5; i++)
        {
            await NotifyAsync(7, $"Something happened {i}.");
        }

        var page = (await MyNotificationsAsync(7, take: 2)).Value;

        page.Rows.Count.ShouldBe(2);
        page.UnreadCount.ShouldBe(5);
    }

    [Fact]
    public async Task Telling_several_people_tells_each_of_them_once()
    {
        // The same person can be reached twice by one event — a team lead who
        // is also the assigned technician — and two identical lines read as two
        // things happening.
        await fixture.ResetAsync();

        await NotifyManyAsync([7, 8, 7], "The ticket is overdue.");

        (await MyNotificationsAsync(7)).Value.Rows.Count.ShouldBe(1);
        (await MyNotificationsAsync(8)).Value.Rows.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Notifications_can_be_cleared_one_at_a_time_or_all_at_once()
    {
        await fixture.ResetAsync();
        await NotifyAsync(7, "First.");
        await NotifyAsync(7, "Second.");
        var first = (await MyNotificationsAsync(7)).Value.Rows[^1].Id;

        var one = await MarkReadAsync(7, [first]);
        one.Value.MarkedCount.ShouldBe(1);
        one.Value.UnreadCount.ShouldBe(1);

        var rest = await MarkReadAsync(7, all: true);
        rest.Value.MarkedCount.ShouldBe(1);
        rest.Value.UnreadCount.ShouldBe(0);
    }

    [Fact]
    public async Task Clearing_somebody_elses_notification_does_nothing()
    {
        // Scoped in the WHERE clause rather than checked afterwards: an id
        // belonging to somebody else matches nothing, which is also the answer
        // that cannot be turned into a way of finding out whether it exists.
        await fixture.ResetAsync();
        await NotifyAsync(8, "Not yours.");
        var theirs = (await MyNotificationsAsync(8)).Value.Rows.Single().Id;

        var result = await MarkReadAsync(7, [theirs]);

        result.Value.MarkedCount.ShouldBe(0);
        (await MyNotificationsAsync(8)).Value.UnreadCount.ShouldBe(1);
    }

    [Fact]
    public async Task Clearing_nothing_in_particular_is_refused()
    {
        await fixture.ResetAsync();

        (await MarkReadAsync(7, [])).Error!.Code.ShouldBe("Notification.NothingToMark");
    }

    [Fact]
    public async Task A_long_notification_is_cut_to_fit_its_column()
    {
        await fixture.ResetAsync();

        await NotifyAsync(7, new string('x', 900));

        (await MyNotificationsAsync(7)).Value.Rows.Single().Text.Length.ShouldBe(500);
    }

    // --------------------------------------------------------------- plumbing

    private Notifier NewNotifier() => new Notifier(fixture.NewContext(), fixture.Clock);

    private Task<long> QueueAsync(
        string to,
        string subject,
        string? source = null,
        long? sourceId = null) =>
        NewNotifier().QueueEmailAsync(
            new OutboundEmail(to, null, subject, "The body.", true, source, sourceId),
            TestContext.Current.CancellationToken);

    private Task NotifyAsync(int userId, string text) =>
        NewNotifier().NotifyAsync(userId, text, null, TestContext.Current.CancellationToken);

    private Task NotifyManyAsync(IEnumerable<int> userIds, string text) =>
        NewNotifier().NotifyManyAsync(userIds, text, null, TestContext.Current.CancellationToken);

    private Task<int> SendBatchAsync(int batchSize = 20, int maxAttempts = 5)
    {
        var dispatcher = new EmailDispatcher(
            fixture.NewContext(),
            fixture.Transport,
            fixture.Protector,
            fixture.Clock,
            NullLogger<EmailDispatcher>.Instance,
            new DispatcherOptions(BatchSize: batchSize, MaxAttempts: maxAttempts));

        return dispatcher.SendBatchAsync(TestContext.Current.CancellationToken);
    }

    private Task<Result<CreateEmailSettingResponse>> CreateProfileAsync(
        string name,
        string host = "smtp.fujitec.co.in",
        string? username = null,
        string? password = null,
        bool isDefault = false)
    {
        var handler = new CreateEmailSettingHandler(
            fixture.NewContext(), fixture.Protector, fixture.Clock, fixture.CurrentUser,
            fixture.SqlErrors);

        return handler.HandleAsync(
            new CreateEmailSettingCommand(
                name, host, 25, true, "ams@fujitec.co.in", username, password, isDefault),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<UpdateEmailSettingResponse>> UpdateProfileAsync(
        int id,
        string name,
        string? username = null,
        string? password = null,
        int port = 25,
        bool isDefault = false,
        bool isActive = true)
    {
        var handler = new UpdateEmailSettingHandler(
            fixture.NewContext(), fixture.Protector, fixture.Clock, fixture.CurrentUser,
            fixture.SqlErrors);

        return handler.HandleAsync(
            new UpdateEmailSettingCommand(
                id, name, "smtp.fujitec.co.in", port, true, "ams@fujitec.co.in",
                username, password, isDefault, isActive),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchEmailSettingsResponse>> SearchProfilesAsync()
    {
        var handler = new SearchEmailSettingsHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchEmailSettingsQuery(false), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchEmailOutboxResponse>> SearchOutboxAsync(
        string? status = null,
        string? sourceType = null,
        string? search = null)
    {
        var handler = new SearchEmailOutboxHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchEmailOutboxQuery(status, sourceType, null, search, 0, 50),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<RequeueEmailResponse>> RequeueAsync(long id)
    {
        var handler = new RequeueEmailHandler(fixture.NewContext(), fixture.Clock);

        return handler.HandleAsync(
            new RequeueEmailCommand(id), TestContext.Current.CancellationToken);
    }

    private Task<Result<SearchMyNotificationsResponse>> MyNotificationsAsync(
        int userId, int take = 50)
    {
        var handler = new SearchMyNotificationsHandler(fixture.NewContext());

        return handler.HandleAsync(
            new SearchMyNotificationsQuery(userId, false, take),
            TestContext.Current.CancellationToken);
    }

    private Task<Result<MarkNotificationsReadResponse>> MarkReadAsync(
        int userId, IReadOnlyList<long>? ids = null, bool all = false)
    {
        var handler = new MarkNotificationsReadHandler(fixture.NewContext(), fixture.Clock);

        return handler.HandleAsync(
            new MarkNotificationsReadCommand(userId, ids ?? [], all),
            TestContext.Current.CancellationToken);
    }
}
