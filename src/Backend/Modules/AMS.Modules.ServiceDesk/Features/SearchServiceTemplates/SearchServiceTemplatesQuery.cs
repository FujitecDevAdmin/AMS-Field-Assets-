using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.SearchServiceTemplates;

/// <summary>
/// Pre-written requests with defaults. Catalogue screen: Service Templates.
/// </summary>
public sealed record SearchServiceTemplatesQuery(
    bool? IsActive,
    string? RequestKind) : IQuery<SearchServiceTemplatesResponse>;
