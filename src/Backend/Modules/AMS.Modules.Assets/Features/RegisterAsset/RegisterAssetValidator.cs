using FluentValidation;

namespace AMS.Modules.Assets.Features.RegisterAsset;

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
public sealed class RegisterAssetValidator : AbstractValidator<RegisterAssetRequest>
{
    public RegisterAssetValidator()
    {
        RuleFor(x => x.AssetNumber).NotEmpty().MaximumLength(40);
        RuleFor(x => x.AssetName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.SerialNumber).MaximumLength(100);
        RuleFor(x => x.Make).MaximumLength(100);
        RuleFor(x => x.Model).MaximumLength(100);
        RuleFor(x => x.CostCenter).MaximumLength(40);
        RuleFor(x => x.UnitOfMeasure).MaximumLength(20);
        RuleFor(x => x.Remarks).MaximumLength(1000);
        RuleFor(x => x.AssetTypeId).GreaterThan(0);
        RuleFor(x => x.AssetStatusId).GreaterThan(0);
        RuleFor(x => x.AssetClassId).GreaterThan(0).When(x => x.AssetClassId.HasValue);
        // CK_Asset_QuantityPositive says the same thing in the database. Saying it
        // here too turns a 500 into a message beside the field.
        RuleFor(x => x.Quantity).GreaterThan(0).When(x => x.Quantity.HasValue);
    }
}
