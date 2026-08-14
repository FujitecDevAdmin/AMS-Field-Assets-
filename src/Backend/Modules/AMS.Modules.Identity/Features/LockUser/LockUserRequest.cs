namespace AMS.Modules.Identity.Features.LockUser;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record LockUserRequest(
    string? Reason);
