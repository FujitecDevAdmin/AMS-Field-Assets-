namespace AMS.Modules.Organization;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R2-24, R2-25).
/// </summary>
/// <remarks>
/// A capability an endpoint declares but the seed does not contain can never
/// be granted to anybody, so the screen is simply unreachable. Add both
/// together — Identity learned that the hard way.
/// </remarks>
public static class Capabilities
{
    public static class Organization
    {
        /// <summary>Maintain branches, regions, departments, vendors and applications.</summary>
        public const string Manage = "organization.manage";

        /// <summary>Read the organisation master data.</summary>
        public const string View = "organization.view";

        /// <summary>Create and edit employees, and deactivate leavers.</summary>
        public const string EmployeeManage = "employee.manage";

        /// <summary>Read the employee directory.</summary>
        public const string EmployeeView = "employee.view";

        /// <summary>Grant and revoke an employee's access to an application.</summary>
        public const string ApplicationAccessManage = "application-access.manage";
    }
}
