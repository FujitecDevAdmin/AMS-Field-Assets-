using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Notifications.Features.RequeueEmail;

/// <summary>
/// Try a failed message again. Catalogue: the outbox queue.
/// </summary>
public sealed record RequeueEmailCommand(
    long Id) : ICommand<RequeueEmailResponse>;
