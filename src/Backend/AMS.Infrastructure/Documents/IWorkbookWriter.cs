namespace AMS.Infrastructure.Documents;

/// <summary>
/// Server-side Excel. Backed by the DevExpress Office File API, which the
/// Universal licence already covers.
/// </summary>
/// <remarks>
/// <para>
/// The vendor type never leaves this assembly. A module asks for bytes; it
/// does not learn what produced them. That is what makes replacing the
/// implementation a one-file change instead of a search across sixteen
/// modules.
/// </para>
/// <para>
/// Why server-side at all, when docs/04 §3 says every grid exports? Because
/// the DevExtreme client export has to hold every row in the browser first.
/// That is fine for a lookup table and wrong for the asset register. Grids
/// export what the user is looking at; reports and the whole-register export
/// come from here.
/// </para>
/// </remarks>
public interface IWorkbookWriter
{
    /// <summary>
    /// Writes one sheet of <paramref name="rows"/> and returns the .xlsx bytes.
    /// </summary>
    /// <param name="sheetName">Sheet name. Excel truncates past 31 characters.</param>
    /// <param name="columns">Header text, in order.</param>
    /// <param name="rows">Cell values, in the same order as <paramref name="columns"/>.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<byte[]> WriteAsync(
        string sheetName,
        IReadOnlyList<string> columns,
        IAsyncEnumerable<IReadOnlyList<object?>> rows,
        CancellationToken ct);
}
