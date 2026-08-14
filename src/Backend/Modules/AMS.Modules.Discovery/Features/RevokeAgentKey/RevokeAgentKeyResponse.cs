namespace AMS.Modules.Discovery.Features.RevokeAgentKey;

/// <summary>
/// The key, dead.
/// </summary>
/// <param name="Id">The key row.</param>
/// <param name="KeyName">What it was called.</param>
/// <param name="RevokedOnUtc">When it stopped working.</param>
public sealed record RevokeAgentKeyResponse(
    int Id,
    string KeyName,
    DateTime RevokedOnUtc);
