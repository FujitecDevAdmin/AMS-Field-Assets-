namespace AMS.Modules.Identity.Features.RegenerateRecoveryCodes;

/// <summary>
/// A fresh set. Every previous code stops working immediately.
/// </summary>
/// <param name="RecoveryCodes">Shown ONCE, like the originals.</param>
public sealed record RegenerateRecoveryCodesResponse(
    IReadOnlyList<string> RecoveryCodes);
