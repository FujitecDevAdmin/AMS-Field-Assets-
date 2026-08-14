using FluentValidation;

namespace AMS.Modules.Allocations.Features.SignAcknowledgement;

/// <summary>
/// Shape only. Lengths mirror the schema exactly.
/// </summary>
/// <remarks>
/// Business invariants are NOT here. "Already taken", "already allocated" and
/// "one active per X" are filtered unique indexes, and a read-then-write check
/// is a race with a nicer error message (docs/02 §5, 03 §1 rule 6).
///
/// Every Request has a validator, even a trivial one, so nobody forgets when a
/// field is added later.
/// </remarks>
public sealed class SignAcknowledgementValidator : AbstractValidator<SignAcknowledgementRequest>
{
    public SignAcknowledgementValidator()
    {
        RuleFor(x => x.SignatureImagePath).MaximumLength(400);
        RuleFor(x => x.DocumentPath).MaximumLength(400);
    }
}
