namespace AMS.Modules.Discovery;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R3-13).
/// </summary>
/// <remarks>
/// Note what is NOT here: the agent's own endpoint has no capability. An agent
/// is not a user — it has no session, no branches and nobody to grant anything
/// to — so it authenticates with an API key and is authorised by holding one.
/// Inventing a capability for it would mean creating a user account per
/// machine, which is how service accounts multiply.
/// </remarks>
public static class Capabilities
{
    public static class Discovery
    {
        /// <summary>Read discovered devices, health and installed software.</summary>
        public const string View = "discovery.view";

        /// <summary>Link a discovered device to an asset, or ignore it.</summary>
        public const string Manage = "discovery.manage";

        /// <summary>Issue and revoke agent API keys.</summary>
        public const string AgentKeyManage = "agent-key.manage";

        /// <summary>Maintain the software catalogue and licence counts.</summary>
        public const string SoftwareCatalogManage = "software-catalog.manage";
    }
}
