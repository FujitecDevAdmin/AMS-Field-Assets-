using AMS.SharedKernel.Results;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace AMS.SharedKernel.Web.Http;

/// <summary>
/// Validates every bound argument that has a validator, before the endpoint
/// runs.
/// </summary>
/// <remarks>
/// <para>
/// The validators in this solution target the <b>Request</b> — the HTTP wire
/// shape — not the Command. That is deliberate: shape is a property of what
/// was sent, and the same rule stated on the Command would run again for a
/// background job that cannot have sent a malformed one.
/// </para>
/// <para>
/// Which means the check has to happen where the Request exists, and that is
/// here rather than in the dispatcher. A filter and not a line in each
/// endpoint, because "zero logic in an endpoint" (02 §6) has to stay true and
/// a per-endpoint call is a per-endpoint omission.
/// </para>
/// <para>
/// First failure only. The endpoints declare <c>ProducesValidationProblem</c>
/// and the client shows one message beside one field.
/// </para>
/// </remarks>
public sealed class ValidationEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        foreach (var argument in context.Arguments)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument), context.HttpContext.RequestAborted);
            if (result.IsValid)
            {
                continue;
            }

            var failure = result.Errors[0];
            return Error
                .Validation($"{argument.GetType().Name}.{failure.PropertyName}", failure.ErrorMessage)
                .ToHttpResult();
        }

        return await next(context);
    }
}
