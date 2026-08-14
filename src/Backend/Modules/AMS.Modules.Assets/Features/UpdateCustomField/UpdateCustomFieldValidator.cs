using FluentValidation;

namespace AMS.Modules.Assets.Features.UpdateCustomField;

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
public sealed class UpdateCustomFieldValidator : AbstractValidator<UpdateCustomFieldRequest>
{
    public UpdateCustomFieldValidator()
    {
        RuleFor(x => x.DisplayLabel).NotEmpty().MaximumLength(150);
        RuleFor(x => x.ValidationRegex).MaximumLength(300);
        RuleFor(x => x.DefaultValue).MaximumLength(300);
        RuleFor(x => x.MaxValue)
            .GreaterThanOrEqualTo(x => x.MinValue!.Value)
            .When(x => x.MinValue.HasValue && x.MaxValue.HasValue);
    }
}
