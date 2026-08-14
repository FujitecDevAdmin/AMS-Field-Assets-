namespace AMS.Modules.ServiceDesk.Features.SendRequestEmail;

/// <summary>
/// The message, queued.
/// </summary>
/// <param name="Id">The e-mail row.</param>
/// <param name="ServiceRequestId">The ticket it belongs to.</param>
/// <param name="Status">Always Queued. Delivery is the Notifications module's job, and SMTP acceptance is not inbox placement.</param>
public sealed record SendRequestEmailResponse(
    int Id,
    int ServiceRequestId,
    string Status);
