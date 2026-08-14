namespace AMS.Modules.ServiceDesk.Features.SearchMyRequests;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchMyRequestsRequest(
    bool? OpenOnly,
    int? Skip,
    int? Take);
