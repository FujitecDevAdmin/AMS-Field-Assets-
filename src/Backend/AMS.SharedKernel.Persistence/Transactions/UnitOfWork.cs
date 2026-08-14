using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace AMS.SharedKernel.Persistence.Transactions;

/// <summary>
/// One <see cref="SqlConnection"/> per request, shared by every module context.
/// </summary>
/// <remarks>
/// Scoped. In a web request that is the request; in a background job it is
/// whatever scope the job creates. Either way the lifetime is what makes
/// "one connection, one transaction, several schemas" true.
/// </remarks>
public sealed class UnitOfWork(string connectionString) : IUnitOfWork, IDisposable, IAsyncDisposable
{
    private readonly SqlConnection _connection = new(connectionString);

    /// <summary>
    /// The contexts already put on the current transaction.
    /// </summary>
    /// <remarks>
    /// The default comparer is reference equality here — DbContext does not
    /// override Equals — which is exactly what is wanted: two contexts of the
    /// same type in one scope are two things to enlist. Tracked at all because
    /// SavingChanges runs on every save and UseTransaction is not free.
    /// </remarks>
    private readonly HashSet<DbContext> _enlisted = [];

    private SqlTransaction? _transaction;
    private int _depth;

    public DbConnection Connection => _connection;

    public DbTransaction? CurrentTransaction => _transaction;

    public async Task<DbConnection> OpenAsync(CancellationToken ct)
    {
        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(ct);
        }

        return _connection;
    }

    public async Task<IUnitOfWorkScope> BeginAsync(CancellationToken ct)
    {
        if (_depth++ > 0)
        {
            // Already inside one. The inner scope must not commit: the outer
            // command is not finished, and committing here would leave the rest
            // of it running outside any transaction at all.
            return NestedScope.Instance;
        }

        await OpenAsync(ct);
        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(ct);
        return new OuterScope(this);
    }

    public void Enlist(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (_transaction is null || !_enlisted.Add(context))
        {
            return;
        }

        context.Database.UseTransaction(_transaction);
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
        }

        await _connection.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// The synchronous half, and it is not optional.
    /// </summary>
    /// <remarks>
    /// A scoped service that implements only IAsyncDisposable makes the
    /// container throw the moment anything disposes its scope synchronously —
    /// <c>using var scope = provider.CreateScope()</c>, which is what every
    /// background job and every EF tooling entry point does. The integration
    /// tests found it on the first request they made.
    /// </remarks>
    public void Dispose()
    {
        _transaction?.Dispose();
        _connection.Dispose();
        GC.SuppressFinalize(this);
    }

    private async Task EndAsync(bool commit, CancellationToken ct)
    {
        _depth = 0;

        if (_transaction is null)
        {
            return;
        }

        try
        {
            if (commit)
            {
                await _transaction.CommitAsync(ct);
            }
            else
            {
                await _transaction.RollbackAsync(ct);
            }
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
            _enlisted.Clear();
        }
    }

    /// <summary>The real transaction. Rolls back unless told to commit.</summary>
    private sealed class OuterScope(UnitOfWork owner) : IUnitOfWorkScope
    {
        private bool _committed;

        public async Task CommitAsync(CancellationToken ct)
        {
            await owner.EndAsync(commit: true, ct);
            _committed = true;
        }

        /// <summary>
        /// Rolls back when nobody committed. That is the default on purpose: a
        /// handler that returned a failure Result, or threw, must not leave
        /// half its writes behind.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            if (!_committed)
            {
                await owner.EndAsync(commit: false, CancellationToken.None);
            }
        }
    }

    /// <summary>A scope inside another. Does nothing, deliberately.</summary>
    private sealed class NestedScope : IUnitOfWorkScope
    {
        public static readonly NestedScope Instance = new();

        public Task CommitAsync(CancellationToken ct) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
