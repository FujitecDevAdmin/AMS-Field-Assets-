using AMS.SharedKernel.Results;

namespace AMS.SharedKernel.Persistence;

/// <summary>
/// Turns a unique-index violation into the readable 409 the client shows.
/// </summary>
/// <remarks>
/// <para>
/// The schema is the concurrency law and the API translates it
/// (docs/01 §4, 03 §7). A filtered unique index is how "one holder at a time"
/// survives two people clicking at once; this is how the loser finds out.
/// </para>
/// <para>
/// Registration is by INDEX NAME, so a new filtered unique index must add a
/// line here. That is deliberate: an unregistered index surfaces as a bare
/// 409 with a SQL Server message, which is not something to put in front of a
/// branch administrator.
/// </para>
/// </remarks>
public sealed class SqlErrorTranslator
{
    private readonly Dictionary<string, Error> _byIndexName = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>SQL Server: duplicate key in a unique index.</summary>
    public const int DuplicateKeyInIndex = 2601;

    /// <summary>SQL Server: unique constraint violation.</summary>
    public const int UniqueConstraintViolation = 2627;

    public SqlErrorTranslator Register(string indexName, string errorCode, string message)
    {
        _byIndexName[indexName] = Error.Conflict(errorCode, message);
        return this;
    }

    /// <summary>
    /// Finds the registered error for whichever index the message names.
    /// Returns null when nothing matches, and the caller should let the
    /// exception through rather than invent a friendly message for a
    /// constraint nobody has thought about.
    /// </summary>
    public Error? Translate(int sqlErrorNumber, string sqlMessage)
    {
        if (sqlErrorNumber is not (DuplicateKeyInIndex or UniqueConstraintViolation))
        {
            return null;
        }

        ArgumentNullException.ThrowIfNull(sqlMessage);

        foreach (var (indexName, error) in _byIndexName)
        {
            if (sqlMessage.Contains(indexName, StringComparison.OrdinalIgnoreCase))
            {
                return error;
            }
        }

        return null;
    }

    /// <summary>Index names registered so far — used by the architecture test.</summary>
    public IReadOnlyCollection<string> RegisteredIndexes => _byIndexName.Keys;
}
