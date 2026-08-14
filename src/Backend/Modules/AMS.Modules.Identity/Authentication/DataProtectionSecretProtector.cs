using System.Text;
using AMS.Modules.Identity.PublicApi;
using Microsoft.AspNetCore.DataProtection;

namespace AMS.Modules.Identity.Authentication;

/// <inheritdoc />
public sealed class DataProtectionSecretProtector : ISecretProtector
{
    /// <summary>
    /// The purpose string the design script names beside the column. It is
    /// part of the data contract: change it and every stored MFA secret
    /// becomes unreadable.
    /// </summary>
    private const string Purpose = "AMS.Identity.MfaSecret";

    private readonly IDataProtector _protector;

    public DataProtectionSecretProtector(IDataProtectionProvider provider)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector(Purpose);
    }

    public byte[] Protect(string plaintext)
    {
        ArgumentException.ThrowIfNullOrEmpty(plaintext);
        return _protector.Protect(Encoding.UTF8.GetBytes(plaintext));
    }

    public string Unprotect(byte[] protectedBytes)
    {
        ArgumentNullException.ThrowIfNull(protectedBytes);
        return Encoding.UTF8.GetString(_protector.Unprotect(protectedBytes));
    }
}
