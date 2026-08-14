using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Contracts.Features.AddContractDocument;

/// <summary>
/// Attach the signed agreement. Catalogue: Contract Detail.
/// </summary>
public sealed record AddContractDocumentCommand(
    int Id,
    string FilePath,
    string? FileName,
    string? ContentType,
    long? SizeBytes) : ICommand<AddContractDocumentResponse>;
