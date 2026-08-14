using System.Security.Cryptography;
using System.Text;

namespace AMS.Modules.Discovery.Agents;

/// <summary>A newly issued key: the secret, and what is stored about it.</summary>
/// <param name="Key">
/// The whole key. Shown to an administrator once and never recoverable — the
/// database holds a hash, and the point of a hash is that this cannot be got
/// back out of it.
/// </param>
/// <param name="Prefix">
/// The first few characters, stored in the clear. It is how a presented key is
/// found without hashing every row, and it is what an administrator sees on the
/// screen afterwards to tell one key from another.
/// </param>
/// <param name="Hash">What is stored.</param>
public sealed record IssuedAgentKey(string Key, string Prefix, string Hash);

/// <summary>
/// Makes and checks agent API keys.
/// </summary>
/// <remarks>
/// <para>
/// SHA-256, not a password KDF. An API key is 256 bits of randomness this
/// system generated; there is no dictionary to attack and no user who reused it
/// on another site, so the slow hashing that protects a chosen password buys
/// nothing here — and it would be paid on every inventory post from every
/// machine in the company.
/// </para>
/// <para>
/// Comparison is fixed-time. The comparison is against a hash rather than the
/// key, so a timing leak reveals little, but "little" is not a good reason to
/// leak it.
/// </para>
/// </remarks>
public static class AgentKeys
{
    /// <summary>How many characters of the key are kept in the clear.</summary>
    /// <remarks>
    /// Twelve, matching the column. Long enough that a lookup by prefix returns
    /// one row in practice, short enough to be useless on its own.
    /// </remarks>
    public const int PrefixLength = 12;

    /// <summary>Mints a key nobody has seen before.</summary>
    public static IssuedAgentKey Issue()
    {
        // Base64url of 32 random bytes: no padding, no characters that need
        // escaping in a header or a config file, and nothing a person has to
        // transcribe carefully.
        var key = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return new IssuedAgentKey(key, key[..PrefixLength], Hash(key));
    }

    /// <summary>The stored form of a key.</summary>
    public static string Hash(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
    }

    /// <summary>The lookup handle for a presented key.</summary>
    public static string? PrefixOf(string? key) =>
        key is { Length: >= PrefixLength } ? key[..PrefixLength] : null;

    /// <summary>Whether a presented key matches a stored hash.</summary>
    public static bool Matches(string presented, string storedHash)
    {
        ArgumentNullException.ThrowIfNull(storedHash);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(Hash(presented)),
            Encoding.UTF8.GetBytes(storedHash));
    }
}
