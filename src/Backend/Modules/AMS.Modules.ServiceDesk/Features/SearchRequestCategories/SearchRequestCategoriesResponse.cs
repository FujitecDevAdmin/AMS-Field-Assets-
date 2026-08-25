namespace AMS.Modules.ServiceDesk.Features.SearchRequestCategories;

/// <summary>
/// Every category with its sub-categories.
/// </summary>
/// <param name="Rows">The categories, alphabetically.</param>
public sealed record SearchRequestCategoriesResponse(
    IReadOnlyList<SearchRequestCategoriesResponse.Row> Rows)
{
    /// <summary>One category and everything under it.</summary>
    /// <param name="Id">The category.</param>
    /// <param name="CategoryName">Unique, enforced by UX_RequestCategory_Name.</param>
    /// <param name="CategoryType">Service or Incident.</param>
    /// <param name="IsActive">Retired categories stay: tickets still point at them.</param>
    /// <param name="TicketCount">Tickets classified under it.</param>
    /// <param name="SubCategories">Its sub-categories, in one round trip — the screen is a tree.</param>
    public sealed record Row(
        int Id,
        string CategoryName,
        string CategoryType,
        bool IsActive,
        int TicketCount,
        IReadOnlyList<SubCategoryRow> SubCategories);

    /// <summary>One sub-category.</summary>
    /// <param name="Id">The sub-category.</param>
    /// <param name="SubCategoryName">
    /// Unique WITHIN its category, not globally: "Hardware" can reasonably sit
    /// under both Desktop Support and Facilities.
    /// </param>
    /// <param name="IsActive">Retired ones stay for the same reason.</param>
    public sealed record SubCategoryRow(int Id, string SubCategoryName, bool IsActive);
}
