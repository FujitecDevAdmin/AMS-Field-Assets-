namespace AMS.Modules.Transfers.Features.SearchTransferRequests;

/// <summary>
/// One page of transfer requests, newest first.
/// </summary>
/// <param name="Rows">The page.</param>
/// <param name="TotalCount">Requests matching the filter.</param>
public sealed record SearchTransferRequestsResponse(
    IReadOnlyList<SearchTransferRequestsResponse.Row> Rows,
    int TotalCount)
{
    /// <summary>One transfer request.</summary>
    /// <param name="Id">The request.</param>
    /// <param name="AssetId">What is moving. Id only — Assets is another module.</param>
    /// <param name="TransferType">Employee, Department, Branch or CostCenter.</param>
    /// <param name="Status">Pending, Approved, Rejected, Completed or Cancelled.</param>
    /// <param name="FromEmployeeId">Who held it when the request was raised.</param>
    /// <param name="ToEmployeeId">Who should hold it.</param>
    /// <param name="FromDepartmentId">The department it was in.</param>
    /// <param name="ToDepartmentId">The department it should be in.</param>
    /// <param name="FromLocationId">The branch it was at.</param>
    /// <param name="ToLocationId">The branch it should be at.</param>
    /// <param name="FromCostCenter">The cost centre carrying it.</param>
    /// <param name="ToCostCenter">The cost centre that should.</param>
    /// <param name="RequestedByUserId">Who raised it.</param>
    /// <param name="RequestedOnUtc">When.</param>
    /// <param name="ApprovedByUserId">Who decided it.</param>
    /// <param name="ApprovedOnUtc">When they did.</param>
    /// <param name="CompletedOnUtc">When the change was actually applied.</param>
    /// <param name="Remarks">The reason, or the decision remark.</param>
    /// <param name="MovementId">The shipment it caused, if the asset had to travel.</param>
    /// <param name="SapSyncStatus">NotRequired, Pending, Sent or Failed.</param>
    public sealed record Row(
        int Id,
        int AssetId,
        string TransferType,
        string Status,
        int? FromEmployeeId,
        int? ToEmployeeId,
        int? FromDepartmentId,
        int? ToDepartmentId,
        int? FromLocationId,
        int? ToLocationId,
        string? FromCostCenter,
        string? ToCostCenter,
        int RequestedByUserId,
        DateTime RequestedOnUtc,
        int? ApprovedByUserId,
        DateTime? ApprovedOnUtc,
        DateTime? CompletedOnUtc,
        string? Remarks,
        int? MovementId,
        string SapSyncStatus);
}
