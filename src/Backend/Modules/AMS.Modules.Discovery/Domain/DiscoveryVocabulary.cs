namespace AMS.Modules.Discovery.Domain;

/// <summary>
/// What has been decided about a machine the agent found.
/// </summary>
/// <remarks>
/// There is no CHECK constraint on the column, so this list is the only thing
/// keeping it a vocabulary. The queue exists because an agent reporting a
/// machine is not the same as somebody deciding it is an asset — the machine
/// may be a contractor's laptop, a test rig, or an asset already on the
/// register under a different name.
/// </remarks>
public static class DiscoveredDeviceStatus
{
    /// <summary>Seen, and nobody has looked at it yet. The queue.</summary>
    public const string New = "New";

    /// <summary>Matched to an asset already on the register.</summary>
    public const string Linked = "Linked";

    /// <summary>Turned into a new asset.</summary>
    public const string Registered = "Registered";

    /// <summary>Deliberately not ours. A contractor's machine, a test rig.</summary>
    public const string Ignored = "Ignored";

    public static readonly string[] Allowed = [New, Linked, Registered, Ignored];

    /// <summary>The statuses that mean somebody has dealt with it.</summary>
    public static readonly string[] Resolved = [Linked, Registered, Ignored];
}
