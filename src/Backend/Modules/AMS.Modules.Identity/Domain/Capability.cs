using AMS.SharedKernel.Abstractions;

namespace AMS.Modules.Identity.Domain;

/// <summary>
/// One thing a person may do, named as the schema seeds it:
/// <c>handover.record</c>, <c>allocation.revert-return</c>.
/// </summary>
/// <remarks>
/// The name IS the primary key. Capabilities are referenced by name in
/// endpoint declarations and in seed data, and a surrogate id would mean the
/// same capability could exist twice under different ids.
/// </remarks>
public sealed class Capability : IAuditable
{
    public required string Name { get; set; }

    /// <summary>The module that owns the capability, not the name's prefix.</summary>
    public required string Module { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
