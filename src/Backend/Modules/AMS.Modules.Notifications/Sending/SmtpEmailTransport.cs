using System.Net;
using System.Net.Mail;

namespace AMS.Modules.Notifications.Sending;

/// <summary>Sends over SMTP.</summary>
/// <remarks>
/// <para>
/// A client per send rather than one held open. The dispatcher runs every few
/// seconds and a pooled connection to a mail server that may have been
/// restarted, moved or reconfigured in between is a connection that fails on
/// the message that mattered.
/// </para>
/// <para>
/// Addresses are split on both commas and semicolons, because the column says
/// "comma or semicolon separated" and people type whichever their last mail
/// client wanted.
/// </para>
/// </remarks>
public sealed class SmtpEmailTransport : IEmailTransport
{
    public async Task SendAsync(
        EmailProfile profile,
        OutgoingMessage message,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(profile);
        ArgumentNullException.ThrowIfNull(message);

        using var client = new SmtpClient(profile.Host, profile.Port)
        {
            EnableSsl = profile.UseSsl,
        };

        if (!string.IsNullOrWhiteSpace(profile.Username))
        {
            client.Credentials = new NetworkCredential(profile.Username, profile.Password);
        }

        using var mail = new MailMessage
        {
            From = new MailAddress(profile.FromAddress),
            Subject = message.Subject,
            Body = message.Body,
            IsBodyHtml = message.IsHtml,
        };

        foreach (var address in Split(message.ToAddress))
        {
            mail.To.Add(address);
        }

        foreach (var address in Split(message.CcAddress))
        {
            mail.CC.Add(address);
        }

        await client.SendMailAsync(mail, ct);
    }

    private static string[] Split(string? addresses) =>
        string.IsNullOrWhiteSpace(addresses)
            ? []
            : addresses
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries
                    | StringSplitOptions.TrimEntries);
}
