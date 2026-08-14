namespace AMS.Modules.Contracts;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R3-11).
/// </summary>
/// <remarks>
/// Seventh module in a row to have none before its screens were written. The
/// pattern is settled: the seed is written when the SCREENS are, not when the
/// tables are, because until a screen exists nobody knows what it needs
/// permission to do.
///
/// View is separate from manage because an AMC's expiry is something a branch
/// administrator needs to SEE — it decides whether a repair is chargeable —
/// while editing the contract belongs with whoever negotiates it.
/// </remarks>
public static class Capabilities
{
    public static class Contracts
    {
        /// <summary>Read contracts, their covered assets and their documents.</summary>
        public const string View = "contract.view";

        /// <summary>Create, edit, renew and retire contracts.</summary>
        public const string Manage = "contract.manage";

        /// <summary>Configure expiry reminder windows and recipients.</summary>
        public const string ReminderManage = "contract-reminder.manage";
    }
}
