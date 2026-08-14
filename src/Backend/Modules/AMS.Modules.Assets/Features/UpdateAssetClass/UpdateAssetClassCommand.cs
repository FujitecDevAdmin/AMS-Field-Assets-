using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.UpdateAssetClass;

/// <summary>
/// Edit an asset class or retire it.
/// </summary>
public sealed record UpdateAssetClassCommand(
    int Id,
    string ClassCode,
    string ClassName,
    string ReportingCategory,
    bool IsDepreciable,
    bool IsIntangible,
    bool IsActive) : ICommand<UpdateAssetClassResponse>;
