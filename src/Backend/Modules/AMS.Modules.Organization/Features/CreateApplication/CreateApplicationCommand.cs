using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Organization.Features.CreateApplication;

/// <summary>
/// Add a business application. Catalogue: Application master.
/// </summary>
public sealed record CreateApplicationCommand(
    string ApplicationName) : ICommand<CreateApplicationResponse>;
