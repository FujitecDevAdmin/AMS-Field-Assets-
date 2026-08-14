using AMS.Modules.Notifications.Domain;
using AMS.Modules.Notifications.Persistence;
using AMS.Modules.Notifications.Sending;
using AMS.SharedKernel.Abstractions;
using AMS.SharedKernel.Messaging;
using AMS.SharedKernel.Persistence;
using AMS.SharedKernel.Results;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Notifications.Features.CreateEmailSetting;

/// <summary>Add an SMTP profile. Catalogue: E-mail Settings.</summary>
/// <remarks>
/// The password is protected before it is written and never read back. It is
/// the only secret in this module, and the reason the module has its own
/// protector rather than borrowing Identity's: a purpose string shared between
/// two kinds of secret means rotating a key for one breaks the other.
/// </remarks>
public sealed class CreateEmailSettingHandler(
    NotificationsDbContext db,
    SmtpPasswordProtector protector,
    IClock clock,
    ICurrentUser currentUser,
    SqlErrorTranslator sqlErrors)
    : IRequestHandler<CreateEmailSettingCommand, CreateEmailSettingResponse>
{
    public async Task<Result<CreateEmailSettingResponse>> HandleAsync(
        CreateEmailSettingCommand request,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);

        // A username with no password is a profile that cannot authenticate,
        // and the failure comes back from the mail server hours later as
        // "535 authentication failed" against a message somebody was waiting
        // for.
        if (!string.IsNullOrWhiteSpace(request.Username)
            && string.IsNullOrEmpty(request.Password))
        {
            return Error.Validation(
                "EmailSetting.PasswordRequired",
                "A profile with a username needs a password.");
        }

        var setting = new EmailSetting
        {
            ProfileName = request.ProfileName,
            Host = request.Host,
            Port = request.Port,
            UseSsl = request.UseSsl,
            FromAddress = request.FromAddress,
            Username = request.Username,
            SmtpPasswordEncrypted = string.IsNullOrEmpty(request.Password)
                ? null
                : protector.Protect(request.Password),
            IsDefault = request.IsDefault,
            IsActive = true,
            CreatedOnUtc = clock.UtcNow,
            CreatedBy = currentUser.Username,
        };

        db.EmailSettings.Add(setting);

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

        return new CreateEmailSettingResponse(
            setting.Id, setting.ProfileName, setting.IsDefault);
    }
}
