namespace AMS.Modules.Verification.Domain;

/// <summary>An auditor assigned to a physical-verification cycle.</summary>
public sealed class PhysicalVerificationAssignment
{
    public int PhysicalVerificationCycleId { get; set; }

    public int AuditorUserId { get; set; }

    public DateTime AssignedOnUtc { get; set; }

    public string? AssignedBy { get; set; }
}
