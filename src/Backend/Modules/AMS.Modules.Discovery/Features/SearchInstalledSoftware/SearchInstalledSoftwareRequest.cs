namespace AMS.Modules.Discovery.Features.SearchInstalledSoftware;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchInstalledSoftwareRequest(
    string? Search,
    int? AssetId,
    bool? BlacklistedOnly,
    bool? OverLicensedOnly,
    bool? IncludeRemoved);
