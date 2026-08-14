namespace AMS.Modules.Allocations.Features.ReverseReturn;

/// <summary>
/// The reversal record.
/// </summary>
/// <param name="Id">The reversal, kept as evidence.</param>
/// <param name="AllocationId">The allocation put back.</param>
public sealed record ReverseReturnResponse(
    int Id,
    int AllocationId);
