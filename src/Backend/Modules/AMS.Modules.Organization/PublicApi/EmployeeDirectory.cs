using AMS.Modules.Organization.Persistence;
using AMS.Modules.Organization.PublicApi.Organization;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Organization.PublicApi;

/// <summary>Organization's answer to "who do they report to, and where do they work".</summary>
/// <remarks>
/// One reader of <c>Employee.ReportingManagerId</c>, not four. Approval
/// routing, SLA escalation and the joiner workflow all want a manager, and
/// each one reading the column itself would be three more places to change
/// when acting managers arrive.
/// </remarks>
public sealed class EmployeeDirectory(OrganizationDbContext db) : IEmployeeDirectory
{
    public async Task<int?> ManagerOfAsync(int employeeId, CancellationToken ct) =>
        await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => e.ReportingManagerId)
            .SingleOrDefaultAsync(ct);

    public async Task<int?> BranchOfAsync(int employeeId, CancellationToken ct) =>
        await db.Employees
            .AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => e.BranchId)
            .SingleOrDefaultAsync(ct);
}

/// <summary>Organization's answer to "where is this branch, and is it in use".</summary>
public sealed class BranchDirectory(OrganizationDbContext db) : IBranchDirectory
{
    public async Task<string?> TimeZoneOfAsync(int branchId, CancellationToken ct) =>
        await db.Branches
            .AsNoTracking()
            .Where(l => l.Id == branchId)
            .Select(l => l.TimeZoneId)
            .SingleOrDefaultAsync(ct);

    public async Task<bool> IsActiveAsync(int branchId, CancellationToken ct) =>
        await db.Branches.AsNoTracking().AnyAsync(l => l.Id == branchId && l.IsActive, ct);
}

/// <summary>Organization's answer to "who supplies this".</summary>
public sealed class VendorDirectory(OrganizationDbContext db) : IVendorDirectory
{
    public async Task<VendorContact?> FindAsync(int vendorId, CancellationToken ct) =>
        await db.Vendors
            .AsNoTracking()
            .Where(v => v.Id == vendorId)
            .Select(v => new VendorContact(v.VendorName, v.ContactPerson, v.Email))
            .SingleOrDefaultAsync(ct);

    public async Task<bool> IsActiveAsync(int vendorId, CancellationToken ct) =>
        await db.Vendors.AsNoTracking().AnyAsync(v => v.Id == vendorId && v.IsActive, ct);
}
