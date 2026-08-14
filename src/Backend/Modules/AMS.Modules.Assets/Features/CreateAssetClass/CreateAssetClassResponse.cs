namespace AMS.Modules.Assets.Features.CreateAssetClass;

/// <summary>
/// The new class.
/// </summary>
/// <param name="Id">The new class.</param>
/// <param name="ClassCode">Unique. The importer matches on it, so it is the register's own spelling.</param>
/// <param name="ClassName">Unique.</param>
public sealed record CreateAssetClassResponse(
    int Id,
    string ClassCode,
    string ClassName);
