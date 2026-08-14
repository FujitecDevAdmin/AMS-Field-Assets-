namespace AMS.Modules.Contracts.Features.GetContract;

/// <summary>
/// Request to query. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class GetContractMapper
{
    public static GetContractQuery ToQuery(GetContractRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new GetContractQuery(
            id);
    }
}
