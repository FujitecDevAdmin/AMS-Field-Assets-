namespace AMS.Modules.ServiceDesk.Features.AddRequestNote;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class AddRequestNoteMapper
{
    public static AddRequestNoteCommand ToCommand(AddRequestNoteRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AddRequestNoteCommand(
            id,
            request.Note.Trim(),
            request.IsInternal ?? false);
    }
}
