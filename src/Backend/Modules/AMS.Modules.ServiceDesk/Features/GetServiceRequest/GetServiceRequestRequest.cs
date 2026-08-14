namespace AMS.Modules.ServiceDesk.Features.GetServiceRequest;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record GetServiceRequestRequest(
    bool? IncludeInternal);
