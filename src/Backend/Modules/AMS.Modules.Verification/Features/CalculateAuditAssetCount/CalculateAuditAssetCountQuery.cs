using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Verification.Features.CalculateAuditAssetCount;

public sealed record CalculateAuditAssetCountQuery(
    IReadOnlyList<int> LocationBranchIds) : IQuery<CalculateAuditAssetCountResponse>;
