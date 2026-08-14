using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.SearchAssetClasses;

/// <summary>
/// The finance taxonomy. Catalogue screen: Asset Classes and Chart of Accounts.
/// </summary>
public sealed record SearchAssetClassesQuery(
    bool? IsActive) : IQuery<SearchAssetClassesResponse>;
