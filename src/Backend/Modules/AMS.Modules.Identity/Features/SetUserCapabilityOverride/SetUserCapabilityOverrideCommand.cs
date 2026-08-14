using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.SetUserCapabilityOverride;

/// <summary>
/// Grant or deny one capability to one person. Catalogue: Grant or deny one capability - a deny wins.
/// </summary>
public sealed record SetUserCapabilityOverrideCommand(
    int UserId,
    string CapabilityName,
    bool IsGranted,
    string? Reason) : ICommand<SetUserCapabilityOverrideResponse>;
