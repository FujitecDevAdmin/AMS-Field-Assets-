using AMS.Modules.Notifications.Persistence;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Notifications.Features.SearchEmailSettings;

/// <summary>The SMTP profiles. Catalogue: E-mail Settings.</summary>
/// <remarks>
/// The password column is never projected. docs/03 §8: encrypted-at-rest
/// columns are excluded from audit, from logging, and from any projection that
/// feeds a grid — and a screen only needs to know whether one is set.
/// </remarks>
public sealed class SearchEmailSettingsHandler(NotificationsDbContext db)
    : IRequestHandler<SearchEmailSettingsQuery, SearchEmailSettingsResponse>
{
    public async Task<Result<SearchEmailSettingsResponse>> HandleAsync(
        SearchEmailSettingsQuery request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = db.EmailSettings.AsNoTracking();

        if (request.ActiveOnly)
        {
            query = query.Where(s => s.IsActive);
        }

        var rows = await query
            .OrderByDescending(s => s.IsDefault)
            .ThenBy(s => s.ProfileName)
            .Select(s => new SearchEmailSettingsResponse.Row(
                s.Id,
                s.ProfileName,
                s.Host,
                s.Port,
                s.UseSsl,
                s.FromAddress,
                s.Username,
                s.SmtpPasswordEncrypted != null && s.SmtpPasswordEncrypted.Length > 0,
                s.IsDefault,
                s.IsActive))
            .ToListAsync(ct);

        return new SearchEmailSettingsResponse(rows);
    }
}
