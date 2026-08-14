namespace AMS.Modules.Contracts.Features.AddContractDocument;

/// <summary>
/// Request to command. Explicit,
/// greppable, compile-checked - no AutoMapper (docs/02 §4).
/// </summary>
public static class AddContractDocumentMapper
{
    public static AddContractDocumentCommand ToCommand(AddContractDocumentRequest request, int id)
    {
        ArgumentNullException.ThrowIfNull(request);

        return new AddContractDocumentCommand(
            id,
            request.FilePath.Trim(),
            string.IsNullOrWhiteSpace(request.FileName) ? null : request.FileName.Trim(),
            string.IsNullOrWhiteSpace(request.ContentType) ? null : request.ContentType.Trim(),
            request.SizeBytes);
    }
}
