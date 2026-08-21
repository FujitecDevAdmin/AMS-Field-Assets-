namespace AMS.Modules.Verification.Features.SearchAuditAssets;

public sealed record SearchAuditAssetsResponse(
    int AuditId,
    string AuditName,
    string BranchName,
    string AuditStatus,
    IReadOnlyList<SearchAuditAssetsResponse.AssetRow> Rows)
{
    public sealed record AssetRow(
        int Id,
        string AssetNumber,
        string AssetName,
        string? SerialNumber,
        string? Location,
        decimal Quantity,
        bool IsVerified,
        int? VerifiedByUserId,
        string? VerifiedBy,
        DateTime? VerifiedOnUtc);
}
