namespace AMS.Modules.ServiceDesk.Features.AddRequestNote;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record AddRequestNoteRequest(
    string Note,
    bool? IsInternal);
