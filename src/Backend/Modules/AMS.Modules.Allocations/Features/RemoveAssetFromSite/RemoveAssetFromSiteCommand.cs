using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.RemoveAssetFromSite;

/// <summary>
/// Take an asset off a customer site.
/// </summary>
public sealed record RemoveAssetFromSiteCommand(
    int Id) : ICommand<RemoveAssetFromSiteResponse>;
