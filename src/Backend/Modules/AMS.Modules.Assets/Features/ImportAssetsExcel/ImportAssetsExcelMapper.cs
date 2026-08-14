namespace AMS.Modules.Assets.Features.ImportAssetsExcel;

public static class ImportAssetsExcelMapper
{
    public static async Task<ImportAssetsExcelCommand> ToCommandAsync(
        ImportAssetsExcelRequest request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var stream = new MemoryStream();
        await request.File.CopyToAsync(stream, ct);
        return new ImportAssetsExcelCommand(request.File.FileName, stream.ToArray());
    }
}
