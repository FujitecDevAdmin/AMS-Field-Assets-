namespace AMS.Modules.Allocations.Domain;

/// <summary>
/// Mirrors <c>[Allocations].[AllocationReturnReversal]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class AllocationReturnReversal
{
    public int Id { get; set; }

    public int AllocationId { get; set; }

    public int? HandoverId { get; set; }

    public required string Reason { get; set; }

    public DateTime PreviousReturnedOnUtc { get; set; }

    public int? PreviousAssetStatusId { get; set; }

    public int RestoredEmployeeId { get; set; }

    public int ReversedByUserId { get; set; }

    public DateTime ReversedOnUtc { get; set; }
}
