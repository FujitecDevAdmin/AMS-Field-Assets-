using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Allocations.Features.RequestReturn;

/// <summary>
/// Tell the branch an asset is ready to give back. Catalogue: Request a return.
/// </summary>
public sealed record RequestReturnCommand(
    int Id) : ICommand<RequestReturnResponse>;
