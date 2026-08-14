namespace AMS.Modules.Allocations.Features.DecideAllocationRequest;

/// <summary>
/// The decided request.
/// </summary>
/// <param name="Id">The request.</param>
/// <param name="Status">Approved or Rejected.</param>
public sealed record DecideAllocationRequestResponse(
    int Id,
    string Status);
