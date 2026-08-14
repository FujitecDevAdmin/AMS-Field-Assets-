namespace AMS.Modules.Verification.Features.SearchVerificationCycles;

/// <summary>
/// The cycles, newest first.
/// </summary>
/// <param name="Rows">Each with how much of it has been done.</param>
public sealed record SearchVerificationCyclesResponse(
    IReadOnlyList<SearchVerificationCyclesResponse.Row> Rows)
{
    /// <summary>One cycle.</summary>
    /// <param name="Id">The cycle.</param>
    /// <param name="CycleName">What it is called.</param>
    /// <param name="StartDate">When counting began.</param>
    /// <param name="EndDate">When it is meant to finish.</param>
    /// <param name="IsActive">Whether captures are being accepted against it. At most one is.</param>
    /// <param name="ClosedOnUtc">When it was closed.</param>
    /// <param name="VerifiedCount">How many rows were recorded.</param>
    /// <param name="ExceptionCount">How many of those were not Good.</param>
    public sealed record Row(
        int Id,
        string CycleName,
        DateOnly StartDate,
        DateOnly? EndDate,
        bool IsActive,
        DateTime? ClosedOnUtc,
        int VerifiedCount,
        int ExceptionCount);
}
