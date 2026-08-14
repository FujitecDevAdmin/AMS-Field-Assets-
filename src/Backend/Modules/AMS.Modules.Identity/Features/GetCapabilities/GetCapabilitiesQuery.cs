using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.GetCapabilities;

/// <summary>
/// Every capability the application knows about, for the matrix.
/// </summary>
public sealed record GetCapabilitiesQuery(
    string? Module) : IQuery<GetCapabilitiesResponse>;
