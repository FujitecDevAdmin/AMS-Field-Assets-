using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.ImportAssetsExcel;

public sealed record ImportAssetsExcelCommand(string FileName, byte[] Content)
    : ICommand<ImportAssetsExcelResponse>;
