using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Discovery.Features.SearchInstalledSoftware;

/// <summary>
/// What is installed, and whether we are licensed for it. Catalogue: Installed Software.
/// </summary>
public sealed record SearchInstalledSoftwareQuery(
    string? Search,
    int? AssetId,
    bool BlacklistedOnly,
    bool OverLicensedOnly,
    bool IncludeRemoved) : IQuery<SearchInstalledSoftwareResponse>;
