using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.UpdateLocation;

/// <summary>
/// Edit a branch, move it between regions, or retire it.
/// </summary>
public sealed record UpdateLocationCommand(
    int Id,
    string LocationCode,
    string LocationName,
    int? RegionId,
    string TimeZoneId,
    bool IsHeadOffice,
    bool IsActive) : ICommand<UpdateLocationResponse>;
