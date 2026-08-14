using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Discovery.Features.SearchAgentKeys;

/// <summary>
/// The agent keys and when each was last used. Catalogue: Agent Keys.
/// </summary>
public sealed record SearchAgentKeysQuery(
    bool ActiveOnly) : IQuery<SearchAgentKeysResponse>;
