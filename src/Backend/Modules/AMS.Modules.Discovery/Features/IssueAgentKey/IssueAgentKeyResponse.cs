namespace AMS.Modules.Discovery.Features.IssueAgentKey;

/// <summary>
/// The key, shown once.
/// </summary>
/// <param name="Id">The key row.</param>
/// <param name="KeyName">What it is called — usually a site or a rollout.</param>
/// <param name="Key">The secret. This is the ONLY time it is readable; the database keeps a hash.</param>
/// <param name="KeyPrefix">The first twelve characters, which is what the screen shows afterwards.</param>
public sealed record IssueAgentKeyResponse(
    int Id,
    string KeyName,
    string Key,
    string KeyPrefix);
