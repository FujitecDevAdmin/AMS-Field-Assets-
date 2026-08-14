namespace AMS.Modules.SapSync.Domain;

/// <summary>
/// Mirrors <c>[SapSync].[SapSyncLog]</c> in AMS_Consolidated_Design_v2.sql.
/// </summary>
public sealed class SapSyncLog
{
    public long Id { get; set; }

    public required string Direction { get; set; }

    public required string SyncType { get; set; }

    public required string Outcome { get; set; }

    public required string Message { get; set; }

    public int RecordsProcessed { get; set; }

    public int RecordsFailed { get; set; }

    public string? SourceReference { get; set; }

    public DateTime StartedOnUtc { get; set; }

    public DateTime? CompletedOnUtc { get; set; }

    public int AttemptCount { get; set; }
}
