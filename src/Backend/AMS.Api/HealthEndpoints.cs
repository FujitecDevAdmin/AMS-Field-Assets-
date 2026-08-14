using AMS.SharedKernel.Persistence.Transactions;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Api;

/// <summary>
/// Liveness and readiness. The only routes the host owns.
/// </summary>
/// <remarks>
/// Every other route belongs to a module and is contributed by its
/// <c>Map*Module</c> (01 §5). These two do not: "can this process reach its
/// database" is a question about the deployment, not about any one module.
/// </remarks>
public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Anonymous on purpose: a load balancer has no bearer token, and a
        // liveness probe that needs one reports the process dead whenever
        // authentication is misconfigured.
        endpoints.MapGet("/health/live", () => TypedResults.Ok(new { status = "live" }))
            .AllowAnonymous()
            .WithName("HealthLive")
            .ExcludeFromDescription();

        endpoints.MapGet("/health/ready", async (
                [FromServices] IUnitOfWork unitOfWork,
                CancellationToken ct) =>
            {
                try
                {
                    await unitOfWork.OpenAsync(ct);
                    return Results.Ok(new { status = "ready" });
                }
                catch (Microsoft.Data.SqlClient.SqlException ex)
                {
                    // 503 and not 500: the process is fine, the database is not,
                    // and an orchestrator should stop sending traffic rather
                    // than restart a healthy container.
                    return Results.Problem(
                        ex.Message,
                        statusCode: StatusCodes.Status503ServiceUnavailable,
                        title: "Database unreachable");
                }
            })
            .AllowAnonymous()
            .WithName("HealthReady")
            .ExcludeFromDescription();

        return endpoints;
    }
}
