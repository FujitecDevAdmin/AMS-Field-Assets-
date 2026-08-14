namespace AMS.Modules.Contracts.Features.AddContractDocument;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record AddContractDocumentRequest(
    string FilePath,
    string? FileName,
    string? ContentType,
    long? SizeBytes);
