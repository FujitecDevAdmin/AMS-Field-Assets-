namespace AMS.Modules.Notifications;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R3-10).
/// </summary>
/// <remarks>
/// <para>
/// The module had none at all before this. Every e-mail in the system goes
/// through its outbox, and nothing could be granted to look at it — so a
/// message that failed to send was invisible to everybody, which defeats the
/// point of having an outbox rather than sending inline.
/// </para>
/// <para>
/// Sixth module in a row to ship its first slices with that gap open. The
/// pattern is settled: the seed is written when the SCREENS are, not when the
/// tables are.
/// </para>
/// <para>
/// There is deliberately no capability for reading your own notifications.
/// Every signed-in user reads their own, and a capability would be a lie —
/// withdrawing it would stop somebody being told things about their own work.
/// </para>
/// </remarks>
public static class Capabilities
{
    public static class Notifications
    {
        /// <summary>Configure SMTP profiles and the sending address.</summary>
        public const string EmailSettingManage = "email-setting.manage";

        /// <summary>Read the e-mail queue and requeue a failed message.</summary>
        public const string OutboxManage = "outbox.manage";
    }
}
