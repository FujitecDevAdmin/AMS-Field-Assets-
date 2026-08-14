namespace AMS.Modules.Allocations.Features.GetMyAssets;

/// <summary>
/// Everything this employee is currently accountable for.
/// </summary>
/// <param name="Rows">Their open allocations.</param>
public sealed record GetMyAssetsResponse(
    IReadOnlyList<GetMyAssetsResponse.Row> Rows)
{
    /// <summary>One asset this employee holds.</summary>
    /// <param name="AllocationId">The allocation.</param>
    /// <param name="AssetId">The asset. Id only.</param>
    /// <param name="AllocatedOnUtc">Since when.</param>
    /// <param name="ExpectedReturnDate">When it is due back, if it is.</param>
    /// <param name="ReturnRequested">Whether they have already asked to give it back.</param>
    /// <param name="AcknowledgementStatus">
    /// Pending until they sign, Signed until the manager countersigns, then
    /// Approved. The screen prompts on the first of those.
    /// </param>
    public sealed record Row(
        int AllocationId,
        int AssetId,
        DateTime AllocatedOnUtc,
        DateOnly? ExpectedReturnDate,
        bool ReturnRequested,
        string? AcknowledgementStatus);
}
