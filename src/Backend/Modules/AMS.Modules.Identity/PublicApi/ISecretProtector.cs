namespace AMS.Modules.Identity.PublicApi;

/// <summary>
/// Protects the encrypted-at-rest columns the schema marks
/// <c>*Encrypted</c> — here, <c>User.MfaSecretEncrypted</c>.
/// </summary>
/// <remarks>
/// docs/03 §8: these are <c>byte[]</c> protected with ASP.NET Data Protection,
/// excluded from audit, from logging, and from any projection that feeds a
/// grid. The purpose string is part of the contract — changing it makes every
/// stored secret unreadable, which for MFA means every enrolled user has to
/// enrol again.
/// </remarks>
public interface ISecretProtector
{
    byte[] Protect(string plaintext);

    string Unprotect(byte[] protectedBytes);
}
