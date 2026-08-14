namespace AMS.Modules.Identity;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them.
/// </summary>
/// <remarks>
/// Grouped by the OWNING module, while the string keeps the prefix the schema
/// gave it — the two do not always agree, and the seed wins (docs/02 §2).
/// Never retype a capability name at a call site.
/// </remarks>
public static class Capabilities
{
    public static class Identity
    {
        /// <summary>Create, edit, lock, unlock and reset users.</summary>
        public const string UserManage = "user.manage";

        /// <summary>Read a user's effective capability set.</summary>
        public const string UserView = "user.view";

        /// <summary>Create, rename, retire roles and set what they grant.</summary>
        public const string RoleManage = "role.manage";
    }
}
