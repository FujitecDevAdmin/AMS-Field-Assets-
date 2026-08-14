namespace AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record RaiseServiceRequestRequest(
    string RequestKind,
    string Subject,
    string? Description,
    string? Priority,
    int? RequestCategoryId,
    int? RequestSubCategoryId,
    int? ServiceTemplateId,
    int? AssetId,
    string? ManualAssetText,
    int RequestedByEmployeeId,
    int? OnBehalfOfEmployeeId,
    int? LocationId,
    RaiseServiceRequestRequest.NewServiceDetail? NewService)
{
    /// <summary>
    /// The extra questions a New Service request asks: which systems the joiner
    /// needs, by when, and what kit.
    /// </summary>
    /// <remarks>
    /// Null for every other kind. A SupportTicket has no joining date, and
    /// carrying the columns anyway is how a table ends up half empty and
    /// nobody trusting either half.
    /// </remarks>
    public sealed record NewServiceDetail(
        bool NeedsEmail,
        bool NeedsErp,
        bool NeedsDms,
        bool NeedsVpn,
        DateOnly? RequiredByDate,
        string? Notes,
        IReadOnlyList<NewServiceItem> Items)
    {
        /// <summary>The kit asked for. Empty is allowed: access without hardware is a request too.</summary>
        public IReadOnlyList<NewServiceItem> Items { get; init; } = Items ?? [];
    }

    /// <summary>One line of kit.</summary>
    public sealed record NewServiceItem(int AssetTypeId, int Quantity, string? Specification);
}
