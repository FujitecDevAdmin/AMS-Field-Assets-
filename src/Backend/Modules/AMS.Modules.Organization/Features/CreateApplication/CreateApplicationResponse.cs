namespace AMS.Modules.Organization.Features.CreateApplication;

/// <summary>
/// The new application.
/// </summary>
/// <param name="Id">The new application.</param>
/// <param name="ApplicationName">As stored, trimmed.</param>
public sealed record CreateApplicationResponse(
    int Id,
    string ApplicationName);
