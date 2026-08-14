using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using AMS.SharedKernel.Abstractions;

namespace AMS.Modules.Identity.Authentication;

/// <summary>
/// Time-based one-time passwords, RFC 6238.
/// </summary>
/// <remarks>
/// Written out rather than taken from a package: it is forty lines of
/// well-specified arithmetic, and an authentication dependency is one more
/// thing to audit and keep patched.
/// </remarks>
public interface ITotpCodes
{
    /// <summary>A new random secret, base32 for an authenticator app to scan.</summary>
    string CreateSecret();

    /// <summary>
    /// True when <paramref name="code"/> is valid for <paramref name="secret"/>
    /// right now, allowing one step either side for clock drift.
    /// </summary>
    bool Verify(string secret, string code);
}

/// <inheritdoc />
public sealed class TotpCodes(IClock clock) : ITotpCodes
{
    private const int StepSeconds = 30;
    private const int Digits = 6;

    /// <summary>
    /// One step of tolerance each way. Two would be generous enough to widen
    /// the window a stolen code is usable in for no real gain.
    /// </summary>
    private const int DriftSteps = 1;

    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public string CreateSecret() => ToBase32(RandomNumberGenerator.GetBytes(20));

    public bool Verify(string secret, string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);

        if (string.IsNullOrWhiteSpace(code) || code.Length != Digits)
        {
            return false;
        }

        var key = FromBase32(secret);
        var step = ToUnixTime(clock.UtcNow) / StepSeconds;

        for (var offset = -DriftSteps; offset <= DriftSteps; offset++)
        {
            // Fixed-time comparison: an early return leaks which digits matched.
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(Compute(key, step + offset)),
                    Encoding.ASCII.GetBytes(code)))
            {
                return true;
            }
        }

        return false;
    }

    private static long ToUnixTime(DateTime utcNow) =>
        (long)(DateTime.SpecifyKind(utcNow, DateTimeKind.Utc) - DateTime.UnixEpoch).TotalSeconds;

    private static string Compute(byte[] key, long step)
    {
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);

        Span<byte> hash = stackalloc byte[20];
        HMACSHA1.HashData(key, counter, hash);

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   | (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString(CultureInfo.InvariantCulture).PadLeft(Digits, '0');
    }

    private static string ToBase32(byte[] data)
    {
        var builder = new StringBuilder();
        int buffer = 0, bitsLeft = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                builder.Append(Base32Alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            builder.Append(Base32Alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }

        return builder.ToString();
    }

    private static byte[] FromBase32(string secret)
    {
        var bytes = new List<byte>();
        int buffer = 0, bitsLeft = 0;

        foreach (var c in secret.TrimEnd('=').ToUpperInvariant())
        {
            var index = Base32Alphabet.IndexOf(c, StringComparison.Ordinal);
            if (index < 0)
            {
                continue;
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bytes.Add((byte)((buffer >> (bitsLeft - 8)) & 0xFF));
                bitsLeft -= 8;
            }
        }

        return [.. bytes];
    }
}
