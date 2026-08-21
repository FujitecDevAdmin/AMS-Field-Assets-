namespace AMS.Modules.Verification.Features.SearchMyAudits;

public sealed record SearchMyAuditsResponse(IReadOnlyList<SearchMyAuditsResponse.AuditRow> Rows)
{
    public sealed record AuditRow(
        int Id,
        string AuditName,
        int BranchId,
        string BranchName,
        DateOnly StartDate,
        DateOnly? EndDate,
        bool IsActive,
        IReadOnlyList<AssetRow> Assets);

    public sealed record AssetRow(
        int Id,
        string AssetNumber,
        string AssetName,
        string? SerialNumber,
        string? QrCodeValue,
        string? BarcodeValue,
        string? Location,
        decimal Quantity,
        bool IsBulk,
        bool IsVerified);
}
