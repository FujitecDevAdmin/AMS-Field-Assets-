using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Discovery.Features.RevokeAgentKey;

/// <summary>
/// Stop a key working. Catalogue: Agent Keys.
/// </summary>
public sealed record RevokeAgentKeyCommand(
    int Id) : ICommand<RevokeAgentKeyResponse>;
