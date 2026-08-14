using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.UpdateCustomerSite;

/// <summary>
/// Edit a customer site or retire it.
/// </summary>
public sealed record UpdateCustomerSiteCommand(
    int Id,
    string? CustomerName,
    string SiteName,
    string? City,
    string? Address,
    decimal? Latitude,
    decimal? Longitude,
    bool IsActive) : ICommand<UpdateCustomerSiteResponse>;
