using AMS.Modules.Notifications.Persistence;
using AMS.Modules.Notifications.Sending;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Notifications.Features.UpdateEmailSetting;

/// <summary>Edit an SMTP profile or retire it. Catalogue: E-mail Settings.</summary>
/// <remarks>
/// <para>
/// An empty password means "leave it alone", not "clear it". The screen cannot
/// show the stored one, so it cannot send it back, and treating the blank field
/// it therefore posts as a deletion would wipe the password every time somebody
/// corrected the port.
/// </para>
/// <para>
/// Clearing a password is done by clearing the username: a profile with neither
/// sends unauthenticated, which is a real configuration and a deliberate one.
/// </para>
/// </remarks>
public sealed class UpdateEmailSettingHandler(
    NotificationsDbContext db,
    SmtpPasswordProtector protector,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<UpdateEmailSettingCommand, UpdateEmailSettingResponse>
{
    public async Task<Result<UpdateEmailSettingResponse>> HandleAsync(
        UpdateEmailSettingCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        var setting = await db.EmailSettings.SingleOrDefaultAsync(s => s.Id == request.Id, ct);
        if (setting is null)
        {
            return Error.NotFound("EmailSetting", request.Id);
        }

        var keepsPassword = setting.SmtpPasswordEncrypted is { Length: > 0 };

        if (!string.IsNullOrWhiteSpace(request.Username)
            && string.IsNullOrEmpty(request.Password)
            && !keepsPassword)
        {
            return Error.Validation(
                "EmailSetting.PasswordRequired",
                "A profile with a username needs a password.");
        }

        setting.ProfileName = request.ProfileName;
        setting.Host = request.Host;
        setting.Port = request.Port;
        setting.UseSsl = request.UseSsl;
        setting.FromAddress = request.FromAddress;
        setting.Username = request.Username;
        setting.IsDefault = request.IsDefault;
        setting.IsActive = request.IsActive;
        setting.ModifiedOnUtc = clock.UtcNow;
        setting.ModifiedBy = currentUser.Username;

        if (!string.IsNullOrEmpty(request.Password))
        {
            setting.SmtpPasswordEncrypted = protector.Protect(request.Password);
        }
        else if (string.IsNullOrWhiteSpace(request.Username))
        {
            setting.SmtpPasswordEncrypted = null;
        }

        // A retired profile is nobody's default: leaving the flag set would
        // hold the one live-default slot against a profile that cannot send.
        if (!request.IsActive)
        {
            setting.IsDefault = false;
        }

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql)
        {
            var error = sqlErrors.Translate(sql.Number, sql.Message);
            if (error is not null)
            {
                return error;
            }

            throw;
        }

        return new UpdateEmailSettingResponse(
            setting.Id, setting.ProfileName, setting.IsActive);
    }
}
