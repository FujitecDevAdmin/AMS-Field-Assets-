using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AMS.Modules.Notifications.Sending;

/// <summary>
/// Runs <see cref="EmailDispatcher"/> on a loop for as long as the API is up.
/// </summary>
/// <remarks>
/// <para>
/// A thin shell on purpose: everything worth testing is in the dispatcher,
/// which this only calls. A background worker that is the only way to run the
/// thing is a worker nobody can test.
/// </para>
/// <para>
/// It creates a scope per pass, because the DbContext is scoped and a singleton
/// holding one for the life of the process would accumulate every entity it had
/// ever tracked.
/// </para>
/// <para>
/// It never throws. A background service that faults takes the host with it in
/// the default configuration, and losing the API because a mail server is
/// unreachable would be a spectacularly bad trade.
/// </para>
/// </remarks>
public sealed class EmailDispatcherService(
    IServiceScopeFactory scopes,
    ILogger<EmailDispatcherService> logger,
    DispatcherOptions options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollSeconds = options.PollSeconds;
        var batchSize = options.BatchSize;

        DispatcherLog.Started(logger, pollSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var sent = 0;

            try
            {
                using var scope = scopes.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<EmailDispatcher>();

                sent = await dispatcher.SendBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                DispatcherLog.PassFailed(logger, ex);
            }

            try
            {
                // A full batch means there is probably more waiting, so go
                // straight round again. Sleeping the full interval after every
                // batch would drain a backlog of a thousand messages at twenty
                // every fifteen seconds - a quarter of an hour to say something
                // that was urgent when it was queued.
                if (sent < batchSize)
                {
                    await Task.Delay(TimeSpan.FromSeconds(pollSeconds), stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        DispatcherLog.Stopped(logger);
    }
}
