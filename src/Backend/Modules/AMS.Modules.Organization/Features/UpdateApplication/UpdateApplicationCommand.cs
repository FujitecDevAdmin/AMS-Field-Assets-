using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.UpdateApplication;

/// <summary>
/// Rename an application or retire it. Catalogue: Application master.
/// </summary>
public sealed record UpdateApplicationCommand(
    int Id,
    string ApplicationName,
    bool IsActive) : ICommand<UpdateApplicationResponse>;
