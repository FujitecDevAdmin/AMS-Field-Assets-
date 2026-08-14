namespace AMS.Modules.Discovery.Features.IssueAgentKey;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record IssueAgentKeyRequest(
    string KeyName);
