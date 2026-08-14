namespace AMS.Modules.Organization.Features.UpdateApplication;

/// <summary>
/// The updated application.
/// </summary>
/// <param name="Id">The application edited.</param>
/// <param name="ApplicationName">As stored, trimmed.</param>
/// <param name="IsActive">Retiring is deactivation: existing grants still point at it.</param>
public sealed record UpdateApplicationResponse(
    int Id,
    string ApplicationName,
    bool IsActive);
