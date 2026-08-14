using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.UpdateRegion;

/// <summary>
/// Rename a region or retire it. Catalogue screen: Regions.
/// </summary>
public sealed record UpdateRegionCommand(
    int Id,
    string RegionName,
    string? Description,
    bool IsActive) : ICommand<UpdateRegionResponse>;
