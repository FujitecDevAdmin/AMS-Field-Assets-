namespace AMS.Modules.ServiceLevel.Features.SearchSlaPolicies;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchSlaPoliciesRequest(
    string? Priority,
    bool? ActiveOnly);
