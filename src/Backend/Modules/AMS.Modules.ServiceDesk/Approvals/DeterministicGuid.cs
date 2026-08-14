using System.Security.Cryptography;
using System.Text;

namespace AMS.Modules.ServiceDesk.Approvals;

/// <summary>
/// The same inputs always give the same id.
/// </summary>
/// <remarks>
/// <para>
/// <c>UX_ApprovalNotificationLog_Idempotency</c> stops the same logical e-mail
/// being queued twice, but only if the key is the same on the second attempt.
/// A random <c>Guid</c> would make every retry a new message, which is exactly
/// what the index exists to prevent — a worker that restarts mid-pass would
/// send everybody their approval request again.
/// </para>
/// <para>
/// So the key is derived from what the message IS: its kind, the step, the
/// participant, and which occurrence of a repeating reminder it is. Two runs
/// that would send the same message produce the same key, and the second one
/// collides.
/// </para>
/// </remarks>
public static class DeterministicGuid
{
    /// <summary>A stable id for a string.</summary>
    /// <remarks>
    /// SHA-256 truncated to sixteen bytes. Not a cryptographic use — nothing
    /// here is secret and nothing is being authenticated — but a hash with no
    /// realistic collisions is worth having when a collision means a
    /// notification silently not sent.
    /// </remarks>
    public static Guid From(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));

        return new Guid(hash.AsSpan(0, 16));
    }

    /// <summary>A stable id for a set of parts, joined so they cannot run together.</summary>
    public static Guid From(params object?[] parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        return From(string.Join('|', parts.Select(p => p?.ToString() ?? string.Empty)));
    }
}
