using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceLevel.Features.SearchSlaPolicies;

/// <summary>
/// The SLA policies and their escalation ladders. Catalogue: SLA Policy Setup.
/// </summary>
public sealed record SearchSlaPoliciesQuery(
    string? Priority,
    bool ActiveOnly) : IQuery<SearchSlaPoliciesResponse>;
