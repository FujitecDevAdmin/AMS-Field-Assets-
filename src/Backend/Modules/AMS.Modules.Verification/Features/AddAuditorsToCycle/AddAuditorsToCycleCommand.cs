using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Verification.Features.AddAuditorsToCycle;

public sealed record AddAuditorsToCycleCommand(int CycleId, IReadOnlyList<int> AuditorUserIds)
    : ICommand<AddAuditorsToCycleResponse>;
