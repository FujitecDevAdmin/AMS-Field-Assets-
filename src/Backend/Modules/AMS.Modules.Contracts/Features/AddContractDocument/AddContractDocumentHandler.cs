using AMS.Modules.Contracts.Domain;
using AMS.Modules.Contracts.Persistence;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Contracts.Features.AddContractDocument;

/// <summary>Attach the signed agreement. Catalogue: Contract Detail.</summary>
/// <remarks>
/// The row holds where the file is, not the file. A scanned twelve-page AMC in
/// every backup of the contract table is not what a backup is for.
/// </remarks>
public sealed class AddContractDocumentHandler(
    ContractsDbContext db,
    IClock clock,
    ICurrentUser currentUser)
    : IRequestHandler<AddContractDocumentCommand, AddContractDocumentResponse>
{
    public async Task<Result<AddContractDocumentResponse>> HandleAsync(
        AddContractDocumentCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!await db.Contracts.AnyAsync(c => c.Id == request.Id && !c.IsDeleted, ct))
        {
            return Error.NotFound("Contract", request.Id);
        }

        var document = new ContractDocument
        {
            ContractId = request.Id,
            FilePath = request.FilePath,
            FileName = request.FileName,
            ContentType = request.ContentType,
            SizeBytes = request.SizeBytes,
            UploadedByUserId = currentUser.Id,
            UploadedOnUtc = clock.UtcNow,
        };

        db.ContractDocuments.Add(document);

        await db.SaveChangesAsync(ct);

        return new AddContractDocumentResponse(
            document.Id, request.Id, document.FileName);
    }
}
