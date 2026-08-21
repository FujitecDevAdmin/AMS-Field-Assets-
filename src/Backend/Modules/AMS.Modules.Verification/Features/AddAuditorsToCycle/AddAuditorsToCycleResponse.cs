namespace AMS.Modules.Verification.Features.AddAuditorsToCycle;

public sealed record AddAuditorsToCycleResponse(
    int CycleId,
    IReadOnlyList<int> AddedAuditorUserIds,
    IReadOnlyList<int> AuditorUserIds);
