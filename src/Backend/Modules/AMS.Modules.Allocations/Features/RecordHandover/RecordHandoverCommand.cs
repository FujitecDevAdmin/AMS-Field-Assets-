using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.RecordHandover;

/// <summary>
/// Take an asset into the branch store. Catalogue: Hand an asset into the branch store — condition and a mandatory remark, closes the allocation.
/// </summary>
public sealed record RecordHandoverCommand(
    int AllocationId,
    int BranchLocationId,
    string ReturnCondition,
    string Remarks,
    IReadOnlyList<string> ImagePaths) : ICommand<RecordHandoverResponse>;
