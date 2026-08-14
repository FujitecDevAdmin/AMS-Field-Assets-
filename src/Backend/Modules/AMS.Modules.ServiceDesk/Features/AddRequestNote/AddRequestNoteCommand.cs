using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.AddRequestNote;

/// <summary>
/// Add a note to the conversation, public or internal. Catalogue: Conversations and History.
/// </summary>
public sealed record AddRequestNoteCommand(
    int Id,
    string Note,
    bool IsInternal) : ICommand<AddRequestNoteResponse>;
