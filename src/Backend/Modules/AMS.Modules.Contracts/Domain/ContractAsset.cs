namespace AMS.Modules.Contracts.Domain;

/// <summary>
/// Mirrors <c>[Contracts].[ContractAsset]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class ContractAsset
{
    public int Id { get; set; }

    public int ContractId { get; set; }

    public int AssetId { get; set; }

    public DateTime LinkedOnUtc { get; set; }

    public int? LinkedByUserId { get; set; }
}
