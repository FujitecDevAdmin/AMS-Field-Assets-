using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.ReverseReturn;

/// <summary>
/// Restore an allocation closed in error. Catalogue: Reverse a return made in error — records who reversed it and why.
/// </summary>
public sealed record ReverseReturnCommand(
    int Id,
    string Reason) : ICommand<ReverseReturnResponse>;
