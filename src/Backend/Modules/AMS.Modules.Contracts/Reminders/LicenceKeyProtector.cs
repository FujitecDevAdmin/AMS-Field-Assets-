using Microsoft.AspNetCore.DataProtection;

namespace AMS.Modules.Contracts.Reminders;

/// <summary>Protects <c>Contract.LicenseKeyEncrypted</c>.</summary>
/// <remarks>
/// Its own purpose string, like the SMTP password's. docs/03 §8 says the
/// purpose is part of the contract, and sharing one between two kinds of secret
/// means rotating a key for one breaks the other.
///
/// A licence key is worth protecting for a reason the other secrets are not:
/// it is the thing a vendor audit asks for and the thing somebody could take
/// with them. It is never projected into a grid and never logged.
/// </remarks>
public sealed class LicenceKeyProtector(IDataProtectionProvider provider)
{
    private const string Purpose = "AMS.Contracts.LicenceKey";

    private readonly IDataProtector _protector = provider.CreateProtector(Purpose);

    public byte[] Protect(string plaintext) =>
        _protector.Protect(System.Text.Encoding.UTF8.GetBytes(plaintext));

    public string Unprotect(byte[] protectedBytes) =>
        System.Text.Encoding.UTF8.GetString(_protector.Unprotect(protectedBytes));
}
