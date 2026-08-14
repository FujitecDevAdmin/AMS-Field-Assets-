using AMS.SharedKernel.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace AMS.SharedKernel.Web.Http;

/// <summary>
/// The mechanical mapping from <see cref="Result{T}"/> to HTTP
/// (docs/02 §3). One place, so no endpoint invents its own status code.
/// </summary>
public static class ResultExtensions
{
    // TypedResults, not Results: our own AMS.SharedKernel.Results namespace
    // shadows the ASP.NET Core `Results` class inside this file. TypedResults
    // is the better shape here anyway - it keeps the concrete result type.

    /// <summary>200 on success.</summary>
    public static IResult ToHttpResult<T>(this Result<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess ? TypedResults.Ok(result.Value) : Failure(result.Error!);
    }

    /// <summary>
    /// 201 with a Location header, for a command that created a row. The slice
    /// chooses this deliberately; nothing infers it.
    /// </summary>
    public static IResult ToCreatedResult<T>(this Result<T> result, Func<T, string> location)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(location);

        return result.IsSuccess
            ? TypedResults.Created(location(result.Value), result.Value)
            : Failure(result.Error!);
    }

    /// <summary>
    /// The same mapping, for a failure produced before any handler ran — the
    /// validation filter is the only caller.
    /// </summary>
    public static IResult ToHttpResult(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return Failure(error);
    }

    private static ProblemHttpResult Failure(Error error) => error.Kind switch
    {
        ErrorKind.NotFound => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status404NotFound, title: error.Code),
        ErrorKind.Validation => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status400BadRequest, title: error.Code),

        // The server writes readable 409s on purpose: the client shows this
        // message verbatim in a toast (docs/04 §3).
        ErrorKind.Conflict => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status409Conflict, title: error.Code),
        ErrorKind.Concurrency => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status412PreconditionFailed, title: error.Code),
        ErrorKind.Forbidden => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status403Forbidden, title: error.Code),
        _ => TypedResults.Problem(error.Message, statusCode: StatusCodes.Status500InternalServerError, title: error.Code),
    };
}
