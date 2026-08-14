namespace AMS.Modules.ServiceDesk.Features.SendRequestEmail;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SendRequestEmailMapper
{
    public static SendRequestEmailCommand ToCommand(SendRequestEmailRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SendRequestEmailCommand(
            id,
            request.ToAddresses.Trim(),
            string.IsNullOrWhiteSpace(request.CcAddresses) ? null : request.CcAddresses.Trim(),
            request.Subject.Trim(),
            request.Body,
            request.IsHtml ?? true);
    }
}
