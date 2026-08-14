using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace AMS.SharedKernel.Persistence.Transactions;

/// <summary>
/// Puts a context, and every command it issues, on the request's transaction.
/// </summary>
/// <remarks>
/// <para>
/// The alternative was for every handler to enlist its own context. That is a
/// line somebody eventually forgets, and the symptom is not an exception — it
/// is a write that commits on its own while the rest of the command rolls
/// back. Silent partial commits are the failure this whole design is arranged
/// to prevent, so enlistment happens where it cannot be skipped.
/// </para>
/// <para>
/// It hooks BOTH commands and saves, because they fail differently:
/// </para>
/// <list type="bullet">
/// <item>
/// <b>Commands.</b> From the moment the dispatcher begins a transaction the
/// connection has a pending local one, and SQL Server refuses any command that
/// does not carry it. A handler's first statement is usually a read, so
/// without this every request throws "BeginExecuteReader requires the command
/// to have a transaction" long before it reaches a save.
/// </item>
/// <item>
/// <b>Saves.</b> EF opens its own transaction for a save unless the context
/// has been told about the ambient one — and opening a second transaction on a
/// connection that already has one fails. A handler that writes before it
/// reads would hit this and nothing else would have caught it.
/// </item>
/// </list>
/// <para>
/// Enlisting at use rather than at construction is forced: DI builds the
/// handler and its context <b>before</b> the dispatcher begins the transaction,
/// so at construction there is nothing to join.
/// </para>
/// </remarks>
public sealed class EnlistInUnitOfWorkInterceptor(IUnitOfWork unitOfWork)
    : IDbCommandInterceptor, ISaveChangesInterceptor
{
    public DbCommand CommandInitialized(CommandEndEventData eventData, DbCommand result)
    {
        ArgumentNullException.ThrowIfNull(eventData);
        ArgumentNullException.ThrowIfNull(result);

        Enlist(eventData.Context);

        if (result.Transaction is null && unitOfWork.CurrentTransaction is { } transaction)
        {
            result.Transaction = transaction;
        }

        return result;
    }

    public InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Enlist(eventData.Context);
        return result;
    }

    public ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        Enlist(eventData.Context);
        return ValueTask.FromResult(result);
    }

    private void Enlist(DbContext? context)
    {
        if (context is not null)
        {
            unitOfWork.Enlist(context);
        }
    }
}
