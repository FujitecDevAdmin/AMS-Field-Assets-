namespace AMS.Modules.Identity.PublicApi;

/// <summary>
/// Hashes and verifies passwords. The algorithm is one decision in one place.
/// </summary>
/// <remarks>
/// A plain password never reaches the domain, never reaches a log, and never
/// reaches the database. The implementation lives in this module.
/// </remarks>
public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string hash);
}
