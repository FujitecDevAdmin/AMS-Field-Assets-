namespace AMS.Modules.Allocations.Features.RequestReturn;

/// <summary>
/// The allocation, now flagged for return.
/// </summary>
/// <param name="Id">The allocation.</param>
/// <param name="ReturnRequestedOnUtc">When the employee asked. The branch queue sorts on it.</param>
public sealed record RequestReturnResponse(
    int Id,
    DateTime ReturnRequestedOnUtc);
