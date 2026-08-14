using FluentValidation;

namespace AMS.Modules.Assets.Features.ImportAssetsExcel;

public sealed class ImportAssetsExcelValidator : AbstractValidator<ImportAssetsExcelRequest>
{
    private const long MaximumFileSize = 20 * 1024 * 1024;

    public ImportAssetsExcelValidator()
    {
        RuleFor(x => x.File).NotNull();
        RuleFor(x => x.File.Length).GreaterThan(0).LessThanOrEqualTo(MaximumFileSize);
        RuleFor(x => x.File.FileName).Must(name => string.Equals(
            Path.GetExtension(name), ".xlsx", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only .xlsx workbooks can be imported.");
    }
}
