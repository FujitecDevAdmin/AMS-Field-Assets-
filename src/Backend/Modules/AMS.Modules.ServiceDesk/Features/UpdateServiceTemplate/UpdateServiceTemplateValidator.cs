using FluentValidation;

namespace AMS.Modules.ServiceDesk.Features.UpdateServiceTemplate;

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
public sealed class UpdateServiceTemplateValidator : AbstractValidator<UpdateServiceTemplateRequest>
{
    public UpdateServiceTemplateValidator()
    {
        RuleFor(x => x.TemplateName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.DefaultPriority).NotEmpty().MaximumLength(20);
        RuleFor(x => x.SubjectTemplate).NotEmpty().MaximumLength(300);
        RuleFor(x => x.DescriptionTemplate).MaximumLength(2000);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0).When(x => x.DisplayOrder.HasValue);
    }
}
