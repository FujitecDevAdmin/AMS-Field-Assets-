namespace AMS.Modules.Verification.Domain;

/// <summary>A Branch Master entry included as an audit location.</summary>
public sealed class PhysicalVerificationCycleLocation
{
    public int PhysicalVerificationCycleId { get; set; }

    public int BranchId { get; set; }
}
