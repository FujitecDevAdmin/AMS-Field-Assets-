using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.Extensions.Logging;

namespace AMS.SharedKernel.Persistence.Transactions;

/// <summary>
/// Sends a command or query to its handler through the pipeline: validation,
/// then — for commands only — one transaction (01 §4).
/// </summary>
/// <remarks>
/// <para>
/// Reflection closes the open generic <c>IRequestHandler&lt;TRequest,
/// TResponse&gt;</c> once per call, because the endpoint knows the message only
/// as <c>ICommand&lt;TResponse&gt;</c>. The alternative is a second registration
/// per slice naming both types, which is one more place to forget.
/// </para>
/// <para>
/// Queries get no transaction. A query that needed one would be a query that
/// writes, and 01 §3 says it does not.
/// </para>
/// <para>
/// It does NOT validate. The validators in this solution target the Request,
/// not the Command, so the check belongs at the HTTP edge where the Request
/// exists — see ValidationEndpointFilter.
/// </para>
/// <para>
/// A command that fails rolls back. That is the point of putting the
/// transaction here rather than in each handler: a command is atomic whether
/// its author remembered to think about it or not.
/// </para>
/// </remarks>
public sealed class Dispatcher(
    IServiceProvider provider,
    IUnitOfWork unitOfWork,
    ILogger<Dispatcher> logger) : IDispatcher
{
    public Task<Result<TResponse>> SendAsync<TResponse>(
        ICommand<TResponse> command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(command);
        return SendAsync<TResponse>(command, transactional: true, ct);
    }

    public Task<Result<TResponse>> SendAsync<TResponse>(
        IQuery<TResponse> query,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(query);
        return SendAsync<TResponse>(query, transactional: false, ct);
    }

    private async Task<Result<TResponse>> SendAsync<TResponse>(
        object message,
        bool transactional,
        CancellationToken ct)
    {
        var messageType = message.GetType();

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(messageType, typeof(TResponse));
        var handler = provider.GetService(handlerType)
            ?? throw new InvalidOperationException(
                $"No handler is registered for {messageType.Name}. Add it in the module's "
                + "Add*Module — a slice that is not registered is a route that 500s.");

        var invoke = handlerType.GetMethod(nameof(IRequestHandler<object, object>.HandleAsync))!;

        async Task<Result<TResponse>> RunAsync() =>
            await (Task<Result<TResponse>>)invoke.Invoke(handler, [message, ct])!;

        if (!transactional)
        {
            return await RunAsync();
        }

        await using var scope = await unitOfWork.BeginAsync(ct);
        var result = await RunAsync();

        if (result.IsSuccess || message is IPersistsOnFailure)
        {
            // IPersistsOnFailure is SignIn and nothing else: its failed-attempt
            // counter is a record of the attempt, not part of the work, and
            // rolling it back would make account lockout unreachable.
            await scope.CommitAsync(ct);
        }
        else
        {
            // Left to the scope's dispose, which rolls back when nobody
            // committed. Logged because a rolled-back command is invisible
            // afterwards, and "it said no and nothing changed" is the single
            // most useful line in a support conversation.
            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "{Message} returned {Code}; rolled back.", messageType.Name, result.Error!.Code);
            }
        }

        return result;
    }

}
