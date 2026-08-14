namespace AMS.Modules.Organization.Features.GrantApplicationAccess;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record GrantApplicationAccessRequest(
    int ApplicationId,
    string? ApplicationLoginId);
