using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Discovery.Features.IssueAgentKey;

/// <summary>
/// Mint a key for an agent to use. Catalogue: Agent Keys.
/// </summary>
public sealed record IssueAgentKeyCommand(
    string KeyName) : ICommand<IssueAgentKeyResponse>;
