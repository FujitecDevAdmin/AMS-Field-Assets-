using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;

/// <summary>
/// Raise a ticket, report a fault on an asset, or ask for a new service. Catalogue: Raise a Request.
/// </summary>
public sealed record RaiseServiceRequestCommand(
    string RequestKind,
    string Subject,
    string? Description,
    string Priority,
    int? RequestCategoryId,
    int? RequestSubCategoryId,
    int? ServiceTemplateId,
    int? AssetId,
    string? ManualAssetText,
    int RequestedByEmployeeId,
    int? OnBehalfOfEmployeeId,
    int? LocationId,
    RaiseServiceRequestCommand.NewServiceDetail? NewService) : ICommand<RaiseServiceRequestResponse>
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
        IReadOnlyList<NewServiceItem> Items);

    /// <summary>One line of kit: three laptops, a monitor, a phone.</summary>
    /// <param name="AssetTypeId">Assets.AssetType, id only (rule 2).</param>
    /// <param name="Quantity">How many. CK_NewServiceRequestItem_PositiveQuantity requires at least one.</param>
    /// <param name="Specification">Anything the standard type does not say.</param>
    public sealed record NewServiceItem(int AssetTypeId, int Quantity, string? Specification);
}
