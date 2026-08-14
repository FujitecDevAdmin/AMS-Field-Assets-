namespace AMS.Modules.Organization.Domain;

/// <summary>
/// Mirrors <c>[Organization].[EmployeeApplication]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class EmployeeApplication
{
    public int Id { get; set; }

    public int EmployeeId { get; set; }

    public int ApplicationId { get; set; }

    public string? ApplicationLoginId { get; set; }

    public DateTime GrantedOnUtc { get; set; }

    public DateTime? RevokedOnUtc { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
