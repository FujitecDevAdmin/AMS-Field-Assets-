namespace AMS.Modules.Movements.Features.GetGrnQueue;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record GetGrnQueueRequest(
    int? ToLocationId,
    int? Skip,
    int? Take);
