using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Verification.Features.SearchAuditAssets;

public sealed record SearchAuditAssetsQuery(int AuditId) : IQuery<SearchAuditAssetsResponse>;
