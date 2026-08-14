namespace AMS.Modules.Organization.Features.SearchApplications;

/// <summary>Every application matching the filter.</summary>
/// <param name="Rows">The applications.</param>
public sealed record SearchApplicationsResponse(IReadOnlyList<SearchApplicationsResponse.Row> Rows)
{
    /// <summary>One business application.</summary>
    /// <param name="Id">The application.</param>
    /// <param name="ApplicationName">Unique, enforced by UX_Application_Name.</param>
    /// <param name="IsActive">Retired applications stay: existing grants point at them.</param>
    /// <param name="ActiveGrantCount">How many employees currently hold access.</param>
    public sealed record Row(int Id, string ApplicationName, bool IsActive, int ActiveGrantCount);
}
