using Microsoft.AspNetCore.Http;

namespace AMS.Modules.Assets.Features.ImportAssetsExcel;

public sealed record ImportAssetsExcelRequest(IFormFile File);
