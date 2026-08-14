using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.SignAcknowledgement;

/// <summary>
/// A digital signature on the undertaking. Catalogue: Sign for an asset.
/// </summary>
public sealed record SignAcknowledgementCommand(
    int AllocationId,
    string? SignatureImagePath,
    string? DocumentPath) : ICommand<SignAcknowledgementResponse>;
