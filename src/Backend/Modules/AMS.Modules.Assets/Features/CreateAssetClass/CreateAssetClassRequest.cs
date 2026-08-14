namespace AMS.Modules.Assets.Features.CreateAssetClass;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record CreateAssetClassRequest(
    string ClassCode,
    string ClassName,
    string ReportingCategory,
    bool? IsDepreciable,
    bool? IsIntangible);
