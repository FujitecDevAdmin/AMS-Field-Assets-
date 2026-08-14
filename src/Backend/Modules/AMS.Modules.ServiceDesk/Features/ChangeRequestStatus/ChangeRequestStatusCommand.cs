using AMS.SharedKernel.Messaging;

namespace AMS.Modules.ServiceDesk.Features.ChangeRequestStatus;

/// <summary>
/// Move a ticket: start it, hold it, resolve it, close it, reopen it. Catalogue: the status bar on Request Detail.
/// </summary>
public sealed record ChangeRequestStatusCommand(
    int Id,
    int RequestStatusId,
    string? Resolution,
    string? Remarks) : ICommand<ChangeRequestStatusResponse>;
