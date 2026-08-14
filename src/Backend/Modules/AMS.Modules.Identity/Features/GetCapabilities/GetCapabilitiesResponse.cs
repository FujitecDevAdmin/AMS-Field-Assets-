namespace AMS.Modules.Identity.Features.GetCapabilities;

/// <summary>
/// The capability catalogue, for the matrix on the Roles &amp; Capabilities screen.
/// </summary>
/// <param name="Rows">The capabilities.</param>
public sealed record GetCapabilitiesResponse(IReadOnlyList<GetCapabilitiesResponse.Row> Rows)
{
    /// <summary>One capability.</summary>
    /// <param name="Name">The name endpoints declare, e.g. <c>handover.record</c>.</param>
    /// <param name="Module">
    /// The OWNING module, which is not always the name's prefix:
    /// <c>handover.dispatch</c> belongs to Movements (docs/02 §2).
    /// </param>
    /// <param name="Description">What holding it lets somebody do.</param>
    public sealed record Row(string Name, string Module, string? Description);
}
