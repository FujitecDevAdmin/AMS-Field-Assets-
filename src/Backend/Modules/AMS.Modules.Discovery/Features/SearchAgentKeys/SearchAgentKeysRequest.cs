namespace AMS.Modules.Discovery.Features.SearchAgentKeys;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchAgentKeysRequest(
    bool? ActiveOnly);
