namespace AMS.Modules.Allocations.Features.SignAcknowledgement;

/// <summary>
/// The HTTP wire shape. Never a domain entity in either direction (docs/01 §3).
/// </summary>
public sealed record SignAcknowledgementRequest(
    string? SignatureImagePath,
    string? DocumentPath);
