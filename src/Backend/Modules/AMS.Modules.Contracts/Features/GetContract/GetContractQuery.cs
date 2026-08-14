using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Contracts.Features.GetContract;

/// <summary>
/// One contract with what it covers. Catalogue: Contract Detail.
/// </summary>
public sealed record GetContractQuery(
    int Id) : IQuery<GetContractResponse>;
