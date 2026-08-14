using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.CreateAssetClass;

/// <summary>
/// Add an asset class. Catalogue: Classify an asset for the accounts.
/// </summary>
public sealed record CreateAssetClassCommand(
    string ClassCode,
    string ClassName,
    string ReportingCategory,
    bool IsDepreciable,
    bool IsIntangible) : ICommand<CreateAssetClassResponse>;
