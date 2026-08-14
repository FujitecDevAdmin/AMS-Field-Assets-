using System.Security.Cryptography;
using AMS.Modules.Identity.PublicApi;

namespace AMS.Modules.Identity.Persistence;

/// <summary>
/// PBKDF2-SHA256. One algorithm, one place, one decision.
/// </summary>
/// <remarks>
/// The stored form is <c>iterations.salt.hash</c>, all base64, so the
/// iteration count can be raised later without invalidating existing hashes —
/// each hash carries the count it was made with.
/// </remarks>
public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltBytes = 16;
    private const int HashBytes = 32;
    private const int Iterations = 210_000;

    public string Hash(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);

        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, HashBytes);

        return $"{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        ArgumentException.ThrowIfNullOrEmpty(hash);

        var parts = hash.Split('.');
        if (parts.Length != 3 || !int.TryParse(parts[0], out var iterations))
        {
            return false;
        }

        var salt = Convert.FromBase64String(parts[1]);
        var expected = Convert.FromBase64String(parts[2]);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

        // Constant time: a comparison that returns early leaks how much of the
        // hash matched.
        return CryptographicOperations.FixedTimeEquals(actual, expected);
    }
}
