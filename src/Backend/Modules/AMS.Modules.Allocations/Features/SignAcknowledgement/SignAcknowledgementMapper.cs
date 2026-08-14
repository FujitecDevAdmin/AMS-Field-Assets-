namespace AMS.Modules.Allocations.Features.SignAcknowledgement;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class SignAcknowledgementMapper
{
    public static SignAcknowledgementCommand ToCommand(SignAcknowledgementRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new SignAcknowledgementCommand(
            id,
            string.IsNullOrWhiteSpace(request.SignatureImagePath) ? null : request.SignatureImagePath.Trim(),
            string.IsNullOrWhiteSpace(request.DocumentPath) ? null : request.DocumentPath.Trim());
    }
}
