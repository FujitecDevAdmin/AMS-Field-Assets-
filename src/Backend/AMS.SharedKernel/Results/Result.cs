namespace AMS.SharedKernel.Results;

/// <summary>
/// The outcome of a handler. docs/02BACKENDCODINGSTANDARDS.md §3:
/// expected failures are values, exceptions are bugs.
/// </summary>
public sealed class Result<T>
{
    private readonly T? _value;

    private Result(T value)
    {
        _value = value;
        Error = null;
    }

    private Result(Error error)
    {
        _value = default;
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public Error? Error { get; }

    /// <summary>The value. Reading it on a failed result is a bug, and throws like one.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException(
            $"Result is a failure ({Error!.Code}); check IsSuccess before reading Value.");

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(Error error) => new(error);

    /// <summary>Lets a handler <c>return someValue;</c> without ceremony.</summary>
    public static implicit operator Result<T>(T value) => new(value);

    /// <summary>Lets a handler <c>return Error.NotFound(...);</c> without ceremony.</summary>
    public static implicit operator Result<T>(Error error) => new(error);

    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<Error, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(Error!);

    public Result<TOut> Map<TOut>(Func<T, TOut> map) =>
        IsSuccess ? Result<TOut>.Success(map(_value!)) : Result<TOut>.Failure(Error!);
}
