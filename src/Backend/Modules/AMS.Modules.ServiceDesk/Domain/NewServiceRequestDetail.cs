namespace AMS.Modules.ServiceDesk.Domain;

/// <summary>
/// Mirrors <c>[ServiceDesk].[NewServiceRequestDetail]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class NewServiceRequestDetail
{
    public int ServiceRequestId { get; set; }

    public int RequestCategoryId { get; set; }

    public int RequestSubCategoryId { get; set; }

    public DateOnly? RequiredByDate { get; set; }

    public string? Notes { get; set; }
}
