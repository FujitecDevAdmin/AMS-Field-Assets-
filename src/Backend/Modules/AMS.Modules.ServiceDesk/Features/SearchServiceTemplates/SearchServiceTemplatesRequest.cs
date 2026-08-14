namespace AMS.Modules.ServiceDesk.Features.SearchServiceTemplates;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SearchServiceTemplatesRequest(
    bool? IsActive,
    string? RequestKind);
