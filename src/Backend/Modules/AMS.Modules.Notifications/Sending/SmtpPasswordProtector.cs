using Microsoft.AspNetCore.DataProtection;

namespace AMS.Modules.Notifications.Sending;

/// <summary>
/// Protects <c>EmailSetting.SmtpPasswordEncrypted</c>.
/// </summary>
/// <remarks>
/// <para>
/// Its own purpose string, not Identity's. docs/03 §8 says the purpose is part
/// of the contract, and a purpose shared between two kinds of secret means
/// rotating the key for one breaks the other — an MFA re-enrolment for every
/// user because somebody changed an SMTP password would be a memorable way to
/// learn that.
/// </para>
/// <para>
/// So each module that stores a secret owns its protector, and
/// <c>ISecretProtector</c> stays inside Identity where its purpose belongs.
/// </para>
/// </remarks>
public sealed class SmtpPasswordProtector(IDataProtectionProvider provider)
{
    private const string Purpose = "AMS.Notifications.SmtpPassword";

    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    public byte[] Protect(string plaintext) => _protector.Protect(
        System.Text.Encoding.UTF8.GetBytes(plaintext));

    public string Unprotect(byte[] protectedBytes) =>
        System.Text.Encoding.UTF8.GetString(_protector.Unprotect(protectedBytes));
}
