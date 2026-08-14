namespace AMS.Modules.Contracts.Domain;

/// <summary>What kind of agreement this is.</summary>
/// <remarks>
/// R3 widened this beyond IT: a lease on a building and an insurance policy on
/// a lift are contracts with expiry dates that somebody has to be reminded
/// about, exactly like an AMC on a laptop. There is no CHECK constraint on the
/// column, so this list is the only thing keeping it a vocabulary rather than
/// free text.
/// </remarks>
public static class ContractType
{
    /// <summary>Annual maintenance contract.</summary>
    public const string Amc = "Amc";

    /// <summary>Manufacturer's cover, usually bought with the asset.</summary>
    public const string Warranty = "Warranty";

    /// <summary>A building, a vehicle, a floor of a building.</summary>
    public const string Lease = "Lease";

    /// <summary>Software, with seats and a key.</summary>
    public const string Licence = "Licence";

    /// <summary>Somebody comes and does something, periodically.</summary>
    public const string Service = "Service";

    /// <summary>Cover against a thing going wrong.</summary>
    public const string Insurance = "Insurance";

    public static readonly string[] Allowed =
        [Amc, Warranty, Lease, Licence, Service, Insurance];
}

/// <summary>How a reminder reaches somebody. CK_ContractReminderSetting_Channel.</summary>
public static class ReminderChannel
{
    public const string Email = "Email";
    public const string InApp = "InApp";
    public const string Both = "Both";

    public static readonly string[] Allowed = [Email, InApp, Both];
}

/// <summary>What became of a reminder. CK_ContractReminderLog_Outcome.</summary>
/// <remarks>
/// Failed is the interesting one: UX_ContractReminderLog_OncePerThreshold
/// excludes it (R2-3), so a send that failed to queue can be retried tomorrow
/// instead of being blocked for ever.
/// </remarks>
public static class ReminderOutcome
{
    public const string Queued = "Queued";
    public const string Sent = "Sent";
    public const string Failed = "Failed";

    public static readonly string[] Allowed = [Queued, Sent, Failed];
}
