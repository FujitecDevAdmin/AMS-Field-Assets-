using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.CreateCustomerSite;

/// <summary>
/// Add a customer site.
/// </summary>
public sealed record CreateCustomerSiteCommand(
    string? CustomerName,
    string SiteName,
    string? City,
    string? Address,
    decimal? Latitude,
    decimal? Longitude) : ICommand<CreateCustomerSiteResponse>;
