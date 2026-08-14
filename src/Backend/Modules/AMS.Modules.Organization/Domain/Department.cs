namespace AMS.Modules.Organization.Domain;

/// <summary>
/// Mirrors <c>[Organization].[Department]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class Department
{
    public int Id { get; set; }

    public required string DepartmentName { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOnUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTime? ModifiedOnUtc { get; set; }

    public string? ModifiedBy { get; set; }
}
