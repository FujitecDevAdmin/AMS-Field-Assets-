namespace AMS.Modules.Movements.Domain;

/// <summary>Where a shipment has got to.</summary>
/// <remarks>Spelled exactly as CK_AssetMovement_Status allows (R2-7).</remarks>
public static class MovementStatus
{
    /// <summary>
    /// Left, not arrived. The asset belongs to NEITHER branch while it is here.
    /// </summary>
    public const string InTransit = "InTransit";

    /// <summary>Arrived and confirmed. Only now does the asset's branch change.</summary>
    public const string Received = "Received";

    /// <summary>Called back before it arrived.</summary>
    public const string Cancelled = "Cancelled";
}

/// <summary>Why an asset is travelling.</summary>
/// <remarks>Spelled exactly as CK_AssetMovement_Type and CK_MovementBatch_Type allow.</remarks>
public static class MovementType
{
    /// <summary>Branch to branch.</summary>
    public const string Transfer = "Transfer";

    /// <summary>Branch standby stock going back to the head-office store.</summary>
    public const string HandoverToHo = "HandoverToHO";

    /// <summary>The two the database allows.</summary>
    public static readonly string[] All = [Transfer, HandoverToHo];
}
