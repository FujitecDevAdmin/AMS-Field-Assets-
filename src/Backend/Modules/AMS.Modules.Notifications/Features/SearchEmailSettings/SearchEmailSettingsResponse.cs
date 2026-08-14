namespace AMS.Modules.Notifications.Features.SearchEmailSettings;

/// <summary>
/// The profiles. Never the passwords.
/// </summary>
/// <param name="Rows">One row per profile, default first.</param>
public sealed record SearchEmailSettingsResponse(
    IReadOnlyList<SearchEmailSettingsResponse.Row> Rows)
{
    /// <summary>One SMTP profile.</summary>
    /// <param name="Id">The profile.</param>
    /// <param name="ProfileName">What it is called.</param>
    /// <param name="Host">The mail server.</param>
    /// <param name="Port">Its port.</param>
    /// <param name="UseSsl">Whether the connection is encrypted.</param>
    /// <param name="FromAddress">Who messages come from.</param>
    /// <param name="Username">The account, if it needs one.</param>
    /// <param name="HasPassword">
    /// Whether a password is stored. The password itself never leaves the
    /// database — docs/03 §8 — so the screen shows whether one is set and
    /// offers to replace it, which is all anybody can act on.
    /// </param>
    /// <param name="IsDefault">Whether the dispatcher sends through it.</param>
    /// <param name="IsActive">Whether it may be used at all.</param>
    public sealed record Row(
        int Id,
        string ProfileName,
        string Host,
        int Port,
        bool UseSsl,
        string FromAddress,
        string? Username,
        bool HasPassword,
        bool IsDefault,
        bool IsActive);
}
