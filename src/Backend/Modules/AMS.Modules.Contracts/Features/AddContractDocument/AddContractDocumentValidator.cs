using FluentValidation;

namespace AMS.Modules.Contracts.Features.AddContractDocument;

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
public sealed class AddContractDocumentValidator : AbstractValidator<AddContractDocumentRequest>
{
    public AddContractDocumentValidator()
    {
        RuleFor(x => x.FilePath).NotEmpty().MaximumLength(400);
        RuleFor(x => x.FileName).MaximumLength(260);
        RuleFor(x => x.ContentType).MaximumLength(120);
        RuleFor(x => x.SizeBytes).GreaterThan(0).When(x => x.SizeBytes.HasValue);
    }
}
