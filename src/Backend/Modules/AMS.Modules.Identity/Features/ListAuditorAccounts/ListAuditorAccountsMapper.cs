namespace AMS.Modules.Identity.Features.ListAuditorAccounts;

public static class ListAuditorAccountsMapper
{
    public static ListAuditorAccountsQuery ToQuery(ListAuditorAccountsRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ListAuditorAccountsQuery();
    }
}
