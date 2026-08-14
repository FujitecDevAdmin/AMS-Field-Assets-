namespace AMS.Modules.Notifications.Features.RequeueEmail;

/// <summary>
/// The message, waiting again.
/// </summary>
/// <param name="Id">The message.</param>
/// <param name="Status">Always Pending.</param>
/// <param name="AttemptCount">Reset to zero, so it gets a full set of tries at the corrected address.</param>
public sealed record RequeueEmailResponse(
    long Id,
    string Status,
    int AttemptCount);
