using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.CreateRegion;

/// <summary>
/// Add a region. Catalogue screen: Regions.
/// </summary>
public sealed record CreateRegionCommand(
    string RegionName,
    string? Description) : ICommand<CreateRegionResponse>;
