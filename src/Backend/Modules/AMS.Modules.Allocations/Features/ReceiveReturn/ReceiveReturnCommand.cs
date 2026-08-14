using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.ReceiveReturn;

/// <summary>
/// Close the allocation and free the asset. Catalogue: Receive a return.
/// </summary>
public sealed record ReceiveReturnCommand(
    int Id,
    int? AssetStatusId,
    string? Remarks) : ICommand<ReceiveReturnResponse>;
