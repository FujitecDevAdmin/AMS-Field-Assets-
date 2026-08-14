using AMS.Modules.ServiceDesk.Domain;

namespace AMS.Modules.ServiceDesk.Features.RaiseServiceRequest;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class RaiseServiceRequestMapper
{
    public static RaiseServiceRequestCommand ToCommand(RaiseServiceRequestRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new RaiseServiceRequestCommand(
            request.RequestKind.Trim(),
            request.Subject.Trim(),
            string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            string.IsNullOrWhiteSpace(request.Priority) ? RequestPriority.Medium : request.Priority.Trim(),
            request.RequestCategoryId,
            request.RequestSubCategoryId,
            request.ServiceTemplateId,
            request.AssetId,
            string.IsNullOrWhiteSpace(request.ManualAssetText) ? null : request.ManualAssetText.Trim(),
            request.RequestedByEmployeeId,
            request.OnBehalfOfEmployeeId,
            request.LocationId,
            request.NewService is null ? null : new RaiseServiceRequestCommand.NewServiceDetail(
                request.NewService.NeedsEmail,
                request.NewService.NeedsErp,
                request.NewService.NeedsDms,
                request.NewService.NeedsVpn,
                request.NewService.RequiredByDate,
                string.IsNullOrWhiteSpace(request.NewService.Notes) ? null : request.NewService.Notes.Trim(),
                [.. request.NewService.Items.Select(i => new RaiseServiceRequestCommand.NewServiceItem(
                    i.AssetTypeId,
                    i.Quantity,
                    string.IsNullOrWhiteSpace(i.Specification) ? null : i.Specification.Trim()))]));
    }
}
