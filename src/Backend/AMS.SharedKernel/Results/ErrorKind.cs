namespace AMS.SharedKernel.Results;

/// <summary>
/// How an <see cref="Error"/> maps to HTTP. The mapping is mechanical and
/// lives in one place so no endpoint invents its own status code
/// (docs/02BACKENDCODINGSTANDARDS.md §3).
/// </summary>
public enum ErrorKind
{
    /// <summary>404.</summary>
    NotFound = 0,

    /// <summary>400.</summary>
    Validation = 1,

    /// <summary>409 — usually a filtered unique index doing its job.</summary>
    Conflict = 2,

    /// <summary>412 — stale RowVersion or SysStartTime.</summary>
    Concurrency = 3,

    /// <summary>403 — authenticated, but the capability is missing.</summary>
    Forbidden = 4,
}
