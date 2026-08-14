namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[NewServiceRequestItem]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class NewServiceRequestItem
{
    public int Id { get; set; }

    public int ServiceRequestId { get; set; }

    public int AssetTypeId { get; set; }

    public int Quantity { get; set; }

    public string? Specification { get; set; }
}
