using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.AllocateAsset;

/// <summary>
/// Assign an available asset to an employee. Catalogue: Allocate an asset.
/// </summary>
public sealed record AllocateAssetCommand(
    int AssetId,
    int EmployeeId,
    int? LocationId,
    DateOnly? ExpectedReturnDate,
    int? ApprovalId,
    string? Remarks) : ICommand<AllocateAssetResponse>;
