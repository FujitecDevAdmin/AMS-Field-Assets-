using System.Security.Cryptography;
using System.Text;

namespace AMS.Modules.Identity.Authentication;

/// <summary>
/// Generates MFA recovery codes.
/// </summary>
/// <remarks>
/// Only hashes are stored, so a code is readable exactly once — at the moment
/// it is created. Nobody, including an administrator, can read one back later.
/// That is the property that makes a recovery code worth having.
/// </remarks>
public static class RecoveryCodes
{
    /// <summary>How many a user gets. Ten is enough to lose a few and still get in.</summary>
    public const int SetSize = 10;

    /// <summary>
    /// Crockford-style alphabet: no I, L, O, U, so a code read off a screen
    /// and typed on a phone cannot become a different valid code.
    /// </summary>
    private const string Alphabet = "ABCDEFGHJKMNPQRSTVWXYZ0123456789";

    private const int GroupLength = 5;
    private const int Groups = 2;

    /// <summary>A fresh set, in the form <c>ABCDE-FGHJK</c>.</summary>
    public static IReadOnlyList<string> CreateSet() =>
        [.. Enumerable.Range(0, SetSize).Select(_ => CreateOne())];

    private static string CreateOne()
    {
        var builder = new StringBuilder(GroupLength * Groups + Groups - 1);

        for (var group = 0; group < Groups; group++)
        {
            if (group > 0)
            {
                builder.Append('-');
            }

            for (var i = 0; i < GroupLength; i++)
            {
                builder.Append(Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)]);
            }
        }

        return builder.ToString();
    }
}
