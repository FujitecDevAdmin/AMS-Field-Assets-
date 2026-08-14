namespace AMS.SharedKernel.Results;

/// <summary>
/// An expected failure. docs/02BACKENDCODINGSTANDARDS.md §3.
/// </summary>
/// <remarks>
/// Codes are stable and dot-separated because they end up in client code,
/// support tickets and log searches. Renaming one is a breaking change.
/// </remarks>
public sealed record Error(string Code, string Message, ErrorKind Kind)
{
    public static Error NotFound(string what, object id) =>
        new($"{what}.NotFound", $"{what} {id} was not found.", ErrorKind.NotFound);

    public static Error Conflict(string code, string message) =>
        new(code, message, ErrorKind.Conflict);

    public static Error Validation(string code, string message) =>
        new(code, message, ErrorKind.Validation);

    public static Error Concurrency(string code, string message) =>
        new(code, message, ErrorKind.Concurrency);

    public static Error Forbidden(string code, string message) =>
        new(code, message, ErrorKind.Forbidden);
}
