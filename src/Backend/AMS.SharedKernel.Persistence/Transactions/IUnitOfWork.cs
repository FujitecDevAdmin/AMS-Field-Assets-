using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace AMS.SharedKernel.Persistence.Transactions;

/// <summary>
/// The one database connection a request uses, and the transaction spanning it.
/// </summary>
/// <remarks>
/// <para>
/// This is architecture rule 4a made real: <b>a transaction spans modules; a
/// DbContext never does.</b> Every module context in a request is built on the
/// same <see cref="DbConnection"/>, so one <c>BEGIN TRANSACTION</c> covers all
/// of them — and no distributed transaction coordinator is involved, which is
/// the whole reason the design insists on one database.
/// </para>
/// <para>
/// Without it <c>IAssetTimeline</c> could not work: an allocation writes to
/// <c>[Allocations]</c> and its timeline line to <c>[Assets]</c>, and a
/// timeline that can commit without the change it describes is what the design
/// calls worse than no timeline. GateB proved the mechanism; this is the
/// mechanism wired into every request.
/// </para>
/// </remarks>
public interface IUnitOfWork
{
    /// <summary>The connection every module context in this request shares.</summary>
    DbConnection Connection { get; }

    /// <summary>The open transaction, or null outside one.</summary>
    DbTransaction? CurrentTransaction { get; }

    /// <summary>Opens the connection if it is not already open.</summary>
    Task<DbConnection> OpenAsync(CancellationToken ct);

    /// <summary>
    /// Starts the transaction a command runs inside.
    /// </summary>
    /// <remarks>
    /// A nested call returns a scope that does nothing on commit or dispose, so
    /// a handler that opens its own transaction — <c>DefineCustomField</c> does,
    /// to write a dropdown and its options together — takes part in the outer
    /// one instead of committing early and leaving the rest of the command
    /// uncovered.
    /// </remarks>
    Task<IUnitOfWorkScope> BeginAsync(CancellationToken ct);

    /// <summary>
    /// Puts a context on the ambient transaction, if there is one and it is not
    /// on it already.
    /// </summary>
    /// <remarks>
    /// Called by the save-changes interceptor rather than by handlers. A
    /// handler that has to remember this is a handler that will forget, and the
    /// symptom would be a silent partial commit rather than an error.
    /// </remarks>
    void Enlist(DbContext context);
}

/// <summary>One command's transaction, or a no-op when one is already open.</summary>
public interface IUnitOfWorkScope : IAsyncDisposable
{
    /// <summary>Commits, unless this scope is nested inside another.</summary>
    Task CommitAsync(CancellationToken ct);
}
