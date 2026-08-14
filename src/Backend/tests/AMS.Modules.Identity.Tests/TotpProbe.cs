using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;

namespace AMS.Modules.Identity.Tests;

/// <summary>
/// An independent RFC 6238 implementation, used only by the tests.
/// </summary>
/// <remarks>
/// <para>
/// Deliberately NOT reusing <c>TotpCodes</c>. A test that generates its codes
/// with the implementation under test agrees with that implementation by
/// construction, including when both are wrong. This one is written from the
/// RFC, so the two have to agree on the standard rather than on each other.
/// </para>
/// <para>
/// It also computes a code for a NAMED step. The first version of these tests
/// brute-forced "whatever verifies right now", which with one step of drift
/// tolerance can be the code for the previous or next step — and the drift
/// test then failed for a reason that had nothing to do with drift.
/// </para>
/// </remarks>
internal static class TotpProbe
{
    private const int StepSeconds = 30;
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    /// <summary>The 30-second counter <paramref name="moment"/> falls in.</summary>
    public static long StepAt(DateTime moment) =>
        (long)(DateTime.SpecifyKind(moment, DateTimeKind.Utc) - DateTime.UnixEpoch).TotalSeconds / StepSeconds;

    /// <summary>The six-digit code for one specific step.</summary>
    public static string CodeForStep(string secret, long step)
    {
        var key = FromBase32(secret);

        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, step);

        Span<byte> hash = stackalloc byte[20];
#pragma warning disable CA5350 // RFC 6238 specifies HMAC-SHA1; see .editorconfig note on TotpCodes.cs
        HMACSHA1.HashData(key, counter, hash);
#pragma warning restore CA5350

        var offset = hash[^1] & 0x0F;
        var binary = ((hash[offset] & 0x7F) << 24)
                   | ((hash[offset + 1] & 0xFF) << 16)
                   | ((hash[offset + 2] & 0xFF) << 8)
                   | (hash[offset + 3] & 0xFF);

        return (binary % 1_000_000).ToString(CultureInfo.InvariantCulture).PadLeft(6, '0');
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
