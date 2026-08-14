namespace AMS.Modules.Notifications.Sending;

/// <summary>The thing that actually talks to a mail server.</summary>
/// <remarks>
/// <para>
/// A seam, and it earns its keep three times over: the dispatcher is testable
/// without a mail server, a site that moves to a hosted mail API replaces one
/// class, and a development environment can log messages instead of sending
/// them to real people.
/// </para>
/// <para>
/// It throws on failure rather than returning a result. The dispatcher has to
/// tell a refused address apart from a host that is down anyway, and the
/// exception carries what went wrong.
/// </para>
/// </remarks>
public interface IEmailTransport
{
    Task SendAsync(EmailProfile profile, OutgoingMessage message, CancellationToken ct);
}

/// <summary>The mail server to talk to.</summary>
/// <param name="Host">Its name.</param>
/// <param name="Port">Its port.</param>
/// <param name="UseSsl">Whether to encrypt the connection.</param>
/// <param name="FromAddress">Who the message is from.</param>
/// <param name="Username">The account, if it needs one.</param>
/// <param name="Password">
/// The password, decrypted. It exists in memory for the length of one send and
/// is never logged — docs/03 §8.
/// </param>
public sealed record EmailProfile(
    string Host,
    int Port,
    bool UseSsl,
    string FromAddress,
    string? Username,
    string? Password);

/// <summary>One message, ready to go.</summary>
public sealed record OutgoingMessage(
    string ToAddress,
    string? CcAddress,
    string Subject,
    string Body,
    bool IsHtml);
