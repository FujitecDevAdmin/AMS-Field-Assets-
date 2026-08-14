namespace AMS.Modules.Allocations.Features.RecordHandover;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RecordHandoverMapper
{
    public static RecordHandoverCommand ToCommand(RecordHandoverRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RecordHandoverCommand(
            id,
            request.BranchLocationId,
            request.ReturnCondition.Trim(),
            request.Remarks.Trim(),
            request.ImagePaths ?? []);
    }
}
