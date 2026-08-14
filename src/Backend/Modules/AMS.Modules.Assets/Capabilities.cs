namespace AMS.Modules.Assets;

/// <summary>
/// The capability names this module's endpoints declare, spelled exactly as
/// Section 17.6 of the design script seeds them (R3-4).
/// </summary>
/// <remarks>
/// A capability an endpoint declares but the seed does not contain can never
/// be granted to anybody, so the screen is simply unreachable. Add both
/// together — Identity learned that the hard way, and Revision 2 shipped an
/// <c>[Assets]</c> schema whose only seeded capabilities were
/// <c>field-asset.*</c>.
/// </remarks>
public static class Capabilities
{
    public static class Assets
    {
        /// <summary>Read the asset register.</summary>
        public const string View = "asset.view";

        /// <summary>Register and edit assets, and record disposals.</summary>
        public const string Manage = "asset.manage";

        /// <summary>
        /// Maintain asset types, classes, statuses, custom fields and
        /// chart-of-account codes.
        /// </summary>
        /// <remarks>
        /// Separate from <see cref="Manage"/> because the audience differs: a
        /// branch administrator runs the register, but inventing an asset
        /// class would corrupt the finance roll-up for everybody.
        /// </remarks>
        public const string TaxonomyManage = "asset-taxonomy.manage";

        /// <summary>
        /// Read the book values and depreciation mirrored from SAP.
        /// </summary>
        /// <remarks>
        /// There is deliberately no matching <c>.manage</c>. SAP owns the
        /// arithmetic and AMS never writes it, so a capability to edit book
        /// values would be a capability to make the two systems disagree.
        /// </remarks>
        public const string FinanceView = "asset-finance.view";

        /// <summary>View the register filtered to field assets.</summary>
        public const string FieldAssetView = "field-asset.view";

        /// <summary>Create, edit and import field assets in the register.</summary>
        public const string FieldAssetManage = "field-asset.manage";
    }
}
