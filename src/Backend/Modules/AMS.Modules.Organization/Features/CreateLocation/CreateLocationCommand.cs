using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.CreateLocation;

/// <summary>
/// Open a branch. Catalogue: Branches and locations, Put a branch in a region, Branch time zone.
/// </summary>
public sealed record CreateLocationCommand(
    string LocationCode,
    string LocationName,
    int? RegionId,
    string TimeZoneId,
    bool IsHeadOffice) : ICommand<CreateLocationResponse>;
