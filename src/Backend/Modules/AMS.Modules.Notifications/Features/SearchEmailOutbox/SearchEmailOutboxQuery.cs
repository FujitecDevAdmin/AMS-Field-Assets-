using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Notifications.Features.SearchEmailOutbox;

/// <summary>
/// What is queued, sent and stuck. Catalogue: the outbox queue.
/// </summary>
public sealed record SearchEmailOutboxQuery(
    string? Status,
    string? SourceType,
    long? SourceId,
    string? Search,
    int Skip,
    int Take) : IQuery<SearchEmailOutboxResponse>;
