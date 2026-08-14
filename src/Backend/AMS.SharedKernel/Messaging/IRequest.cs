using AMS.SharedKernel.Results;

namespace AMS.SharedKernel.Messaging;

/// <summary>
/// A command. Mutates, and is the transaction boundary
/// (docs/01ARCHITECTURE.md §3).
/// </summary>
/// <typeparam name="TResponse">What the slice returns on success.</typeparam>
public interface ICommand<TResponse>;

/// <summary>
/// A query. Never mutates, never calls <c>SaveChanges</c>.
/// </summary>
/// <typeparam name="TResponse">What the slice returns on success.</typeparam>
public interface IQuery<TResponse>;

/// <summary>
/// The one place a slice's work happens. One handler per slice, no exceptions
/// for expected outcomes (docs/02 §3, §4).
/// </summary>
public interface IRequestHandler<in TRequest, TResponse>
{
    Task<Result<TResponse>> HandleAsync(TRequest request, CancellationToken ct);
}

/// <summary>
/// Sends a command or query to its handler through the behavior pipeline:
/// logging, validation, capability check, unit of work (01 §4).
/// </summary>
public interface IDispatcher
{
    Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken ct);

    Task<Result<TResponse>> SendAsync<TResponse>(IQuery<TResponse> query, CancellationToken ct);
}
