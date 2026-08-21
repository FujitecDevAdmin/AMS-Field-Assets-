namespace AMS.Modules.Verification.Features.CalculateAuditAssetCount;

public sealed record CalculateAuditAssetCountRequest(
    IReadOnlyList<int> LocationBranchIds);
