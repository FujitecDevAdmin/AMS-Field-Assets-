using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Movements.Features.GetGrnQueue;

/// <summary>
/// Pending receipts at the destination. Catalogue screen: GRN Queue.
/// </summary>
public sealed record GetGrnQueueQuery(
    int? ToLocationId,
    int Skip,
    int Take) : IQuery<GetGrnQueueResponse>;
