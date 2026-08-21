namespace AMS.Modules.Verification.Features.AddAuditorsToCycle;

public sealed record AddAuditorsToCycleRequest(IReadOnlyList<int> AuditorUserIds);
