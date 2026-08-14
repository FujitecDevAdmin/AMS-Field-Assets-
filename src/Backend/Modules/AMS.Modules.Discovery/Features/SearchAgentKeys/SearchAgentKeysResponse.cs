namespace AMS.Modules.Discovery.Features.SearchAgentKeys;

/// <summary>
/// The keys. Never the secrets.
/// </summary>
/// <param name="Rows">One row per key, most recently used first.</param>
public sealed record SearchAgentKeysResponse(
    IReadOnlyList<SearchAgentKeysResponse.Row> Rows)
{
    /// <summary>One agent key.</summary>
    /// <param name="Id">The key row.</param>
    /// <param name="KeyName">What it is called.</param>
    /// <param name="KeyPrefix">
    /// The first twelve characters. All anybody sees after the day it was
    /// issued, and enough to tell one key from another.
    /// </param>
    /// <param name="IsActive">Whether it still works.</param>
    /// <param name="LastUsedOnUtc">
    /// When an agent last presented it. A key nobody has used is a rollout that
    /// did not happen.
    /// </param>
    /// <param name="RevokedOnUtc">When it was stopped.</param>
    /// <param name="CreatedOnUtc">When it was issued.</param>
    public sealed record Row(
        int Id,
        string KeyName,
        string KeyPrefix,
        bool IsActive,
        DateTime? LastUsedOnUtc,
        DateTime? RevokedOnUtc,
        DateTime CreatedOnUtc);
}
