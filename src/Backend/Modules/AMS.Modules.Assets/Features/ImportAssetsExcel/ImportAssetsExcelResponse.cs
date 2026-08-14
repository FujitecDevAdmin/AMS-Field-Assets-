namespace AMS.Modules.Assets.Features.ImportAssetsExcel;

public sealed record ImportAssetsExcelResponse(
    int TotalRows,
    int ImportedRows,
    int ReactivatedRows,
    int SkippedRows,
    int CreatedAssetTypes,
    IReadOnlyList<ImportAssetsExcelResponse.SkippedRow> SkippedRowDetails,
    IReadOnlyList<ImportAssetsExcelResponse.RowError> Errors)
{
    public sealed record RowError(int RowNumber, string Message);

    public sealed record SkippedRow(
        int RowNumber,
        IReadOnlyDictionary<string, string?> Fields,
        string SystemRemarks);
}
