namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[NewServiceRequestDetail]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class NewServiceRequestDetail
{
    public int ServiceRequestId { get; set; }

    public bool NeedsEmail { get; set; }

    public bool NeedsErp { get; set; }

    public bool NeedsDms { get; set; }

    public bool NeedsVpn { get; set; }

    public DateOnly? RequiredByDate { get; set; }

    public string? Notes { get; set; }
}
