namespace AMS.Modules.Identity.Features.GetCapabilities;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record GetCapabilitiesRequest(
    string? Module);
