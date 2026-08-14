using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Transfers.Features.RaiseTransfer;

/// <summary>
/// Raise a transfer. Catalogue: by employee, department, branch or cost centre.
/// </summary>
public sealed record RaiseTransferCommand(
    int AssetId,
    string TransferType,
    int? ToEmployeeId,
    int? ToDepartmentId,
    int? ToLocationId,
    string? ToCostCenter,
    string? Remarks) : ICommand<RaiseTransferResponse>;
