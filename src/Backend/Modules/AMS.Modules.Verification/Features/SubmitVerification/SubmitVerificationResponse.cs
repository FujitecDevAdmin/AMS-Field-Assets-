namespace AMS.Modules.Verification.Features.SubmitVerification;

/// <summary>
/// The verification, as recorded.
/// </summary>
/// <param name="Id">The row.</param>
/// <param name="AssetId">What was verified.</param>
/// <param name="AssetNumber">For a message a person has to read.</param>
/// <param name="WorkingCondition">What it was found in.</param>
/// <param name="HasQrMismatch">Whether the scanned tag belonged to a different asset.</param>
/// <param name="Variance">Counted minus expected, on a bulk count. Null on a sighting.</param>
/// <param name="WasAlreadyRecorded">True when this device had already sent this capture. The answer is the row it sent, not a second one.</param>
public sealed record SubmitVerificationResponse(
    int Id,
    int AssetId,
    string AssetNumber,
    string WorkingCondition,
    bool HasQrMismatch,
    decimal? Variance,
    bool WasAlreadyRecorded);
