namespace AMS.Modules.Contracts.Features.RenewContract;

/// <summary>
/// The contract, extended.
/// </summary>
/// <param name="Id">The contract.</param>
/// <param name="EndDate">The new expiry.</param>
/// <param name="RenewalCount">One higher than it was.</param>
public sealed record RenewContractResponse(
    int Id,
    DateOnly EndDate,
    int RenewalCount);
