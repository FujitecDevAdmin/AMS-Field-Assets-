using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.CreateAuditorLocation;

public sealed record CreateAuditorLocationCommand(string LocationName)
    : ICommand<CreateAuditorLocationResponse>;
