namespace AMS.Modules.Contracts.Features.AddContractDocument;

/// <summary>
/// The document, as listed on the contract.
/// </summary>
/// <param name="Id">The document row.</param>
/// <param name="ContractId">The contract.</param>
/// <param name="FileName">What to show; FilePath is where it actually lives.</param>
public sealed record AddContractDocumentResponse(
    int Id,
    int ContractId,
    string? FileName);
