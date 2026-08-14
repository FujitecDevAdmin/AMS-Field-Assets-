using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Features.ChangeMyPassword;
using AMS.Modules.Identity.Features.ConfirmMfaEnrolment;
using AMS.Modules.Identity.Features.EnrolMfa;
using AMS.Modules.Identity.Features.GetMyProfile;
using AMS.Modules.Identity.Features.RegenerateRecoveryCodes;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Tests;

/// <summary>
/// Catalogue screen: My Profile. Features: Change my own password,
/// Multi-factor authentication (enrolment and recovery codes).
/// </summary>
[Collection(nameof(IdentityCollectionDefinition))]
public sealed class MyProfileTests(IdentityFixture fixture)
{
    private const string GoodPassword = "correct horse battery";
    private const string Secret = "JBSWY3DPEHPK3PXP";

    // ------------------------------------------------- GetMyProfile: positive

    [Fact]
    public async Task My_profile_reports_what_the_screen_shows()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("alice", mustChangePassword: true);

        var result = await GetProfileAsync(user.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Username.ShouldBe("alice");
        result.Value.MustChangePassword.ShouldBeTrue();
        result.Value.MfaEnabled.ShouldBeFalse();
        result.Value.RemainingRecoveryCodes.ShouldBe(0);
    }

    [Fact]
    public async Task My_profile_counts_only_unused_recovery_codes()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("bob", mfaEnabled: true, mfaSecret: Secret);
        await fixture.AddRecoveryCodeAsync(user.Id, "AAAAA-BBBBB");
        await fixture.AddRecoveryCodeAsync(user.Id, "CCCCC-DDDDD");
        await fixture.ExecuteAsync(
            $"UPDATE [Identity].[UserRecoveryCode] SET [UsedOnUtc] = SYSUTCDATETIME() WHERE [UserId] = {user.Id} AND [Id] = (SELECT MIN([Id]) FROM [Identity].[UserRecoveryCode] WHERE [UserId] = {user.Id});");

        (await GetProfileAsync(user.Id)).Value.RemainingRecoveryCodes.ShouldBe(1);
    }

    // ------------------------------------------------- GetMyProfile: negative

    [Fact]
    public async Task An_unknown_user_has_no_profile()
    {
        await fixture.ResetAsync();

        var result = await GetProfileAsync(9999);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("User.NotFound");
    }

    // -------------------------------------------- ChangeMyPassword: positive

    [Fact]
    public async Task Changing_my_password_clears_the_must_change_flag()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("carol", mustChangePassword: true);

        var result = await ChangePasswordAsync(user.Id, GoodPassword, "a brand new passphrase");

        result.IsSuccess.ShouldBeTrue();
        result.Value.MustChangePassword.ShouldBeFalse();
        (await fixture.ReloadAsync(user.Id)).MustChangePassword.ShouldBeFalse();
    }

    [Fact]
    public async Task The_new_password_is_the_one_that_works_afterwards()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("dave");

        await ChangePasswordAsync(user.Id, GoodPassword, "a brand new passphrase");
        var stored = (await fixture.ReloadAsync(user.Id)).PasswordHash;

        fixture.Hasher.Verify("a brand new passphrase", stored).ShouldBeTrue();
        fixture.Hasher.Verify(GoodPassword, stored).ShouldBeFalse("the old password must stop working");
    }

    // -------------------------------------------- ChangeMyPassword: negative

    [Fact]
    public async Task The_current_password_must_be_right()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("erin");

        var result = await ChangePasswordAsync(user.Id, "not my password", "a brand new passphrase");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Password.CurrentIncorrect");
    }

    [Fact]
    public async Task A_locked_account_cannot_change_its_password()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("frank", isLocked: true);

        var result = await ChangePasswordAsync(user.Id, GoodPassword, "a brand new passphrase");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Password.NotPermitted");
    }

    // ------------------------------------------------ ChangeMyPassword: edge

    [Fact]
    public async Task A_failed_change_leaves_the_old_password_working()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("grace");

        await ChangePasswordAsync(user.Id, "wrong", "a brand new passphrase");

        fixture.Hasher.Verify(GoodPassword, (await fixture.ReloadAsync(user.Id)).PasswordHash).ShouldBeTrue();
    }

    // ------------------------------------------------------- MFA: positive

    [Fact]
    public async Task Enrolment_issues_a_secret_but_does_not_switch_mfa_on()
    {
        // Turning it on here would lock out anybody whose camera failed
        // halfway through.
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("heidi");

        var result = await EnrolAsync(user.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Secret.ShouldNotBeNullOrWhiteSpace();
        result.Value.OtpAuthUri.ShouldStartWith("otpauth://totp/");
        (await fixture.ReloadAsync(user.Id)).MfaEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task Confirming_enrolment_switches_mfa_on_and_issues_recovery_codes()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("ivan");
        var enrol = await EnrolAsync(user.Id);

        var result = await ConfirmAsync(user.Id, CodeFor(enrol.Value.Secret));

        result.IsSuccess.ShouldBeTrue();
        result.Value.MfaEnabled.ShouldBeTrue();
        result.Value.RecoveryCodes.Count.ShouldBe(RecoveryCodes.SetSize);
        result.Value.RecoveryCodes.Distinct(StringComparer.Ordinal).Count()
            .ShouldBe(RecoveryCodes.SetSize, "codes must not repeat");
        (await fixture.ReloadAsync(user.Id)).MfaEnabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Only_hashes_of_the_recovery_codes_are_stored()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("judy");
        var enrol = await EnrolAsync(user.Id);
        var confirm = await ConfirmAsync(user.Id, CodeFor(enrol.Value.Secret));

        await using var context = fixture.NewContext();
        var stored = await context.UserRecoveryCodes.Where(c => c.UserId == user.Id).ToListAsync();

        foreach (var code in confirm.Value.RecoveryCodes)
        {
            stored.ShouldNotContain(s => s.CodeHash == code, "a plaintext code must never be stored");
        }

        stored.Count.ShouldBe(RecoveryCodes.SetSize);
    }

    // ------------------------------------------------------- MFA: negative

    [Fact]
    public async Task Confirming_without_enrolling_is_refused()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("ken");

        var result = await ConfirmAsync(user.Id, "123456");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Mfa.NotStarted");
    }

    [Fact]
    public async Task Confirming_with_a_wrong_code_leaves_mfa_off()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("laura");
        await EnrolAsync(user.Id);

        var result = await ConfirmAsync(user.Id, "000000");

        result.IsSuccess.ShouldBeFalse();
        (await fixture.ReloadAsync(user.Id)).MfaEnabled.ShouldBeFalse();
    }

    [Fact]
    public async Task Enrolling_again_while_already_enrolled_is_refused()
    {
        // It would silently invalidate a working authenticator and every
        // recovery code with it.
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("mallory", mfaEnabled: true, mfaSecret: Secret);

        var result = await EnrolAsync(user.Id);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Mfa.AlreadyEnrolled");
    }

    [Fact]
    public async Task Regenerating_without_a_valid_code_is_refused()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("niaj", mfaEnabled: true, mfaSecret: Secret);
        await fixture.AddRecoveryCodeAsync(user.Id, "KEEP-MEEEE");

        var result = await RegenerateAsync(user.Id, "000000");

        result.IsSuccess.ShouldBeFalse();

        await using var context = fixture.NewContext();
        (await context.UserRecoveryCodes.CountAsync(c => c.UserId == user.Id))
            .ShouldBe(1, "a refused regeneration must not destroy the existing codes");
    }

    // ----------------------------------------------------------- MFA: edge

    [Fact]
    public async Task Regenerating_replaces_every_previous_code()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("olivia");
        var enrol = await EnrolAsync(user.Id);
        var first = await ConfirmAsync(user.Id, CodeFor(enrol.Value.Secret));

        var second = await RegenerateAsync(user.Id, CodeFor(enrol.Value.Secret));

        second.IsSuccess.ShouldBeTrue();
        second.Value.RecoveryCodes.Count.ShouldBe(RecoveryCodes.SetSize);
        second.Value.RecoveryCodes.ShouldNotBe(first.Value.RecoveryCodes);

        await using var context = fixture.NewContext();
        (await context.UserRecoveryCodes.CountAsync(c => c.UserId == user.Id))
            .ShouldBe(RecoveryCodes.SetSize, "the old set must be gone, not appended to");
    }

    [Fact]
    public async Task Re_confirming_enrolment_does_not_leave_two_sets_of_codes()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("peggy");
        await fixture.AddRecoveryCodeAsync(user.Id, "STALE-CODES");

        var enrol = await EnrolAsync(user.Id);
        await ConfirmAsync(user.Id, CodeFor(enrol.Value.Secret));

        await using var context = fixture.NewContext();
        (await context.UserRecoveryCodes.CountAsync(c => c.UserId == user.Id))
            .ShouldBe(RecoveryCodes.SetSize, "codes from a previous enrolment must not survive");
    }

    [Fact]
    public async Task A_confirmed_user_can_then_sign_in_with_the_new_secret()
    {
        // The whole point of enrolment, end to end.
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("quentin");
        var enrol = await EnrolAsync(user.Id);
        await ConfirmAsync(user.Id, CodeFor(enrol.Value.Secret));

        await using var context = fixture.NewContext();
        var handler = new Features.VerifyMfaCode.VerifyMfaCodeHandler(
            context, fixture.Challenges, fixture.Totp, fixture.Secrets,
            IdentityFixture.NewEffectiveAccess(context), fixture.AccessTokens,
            fixture.Hasher, fixture.Clock);

        var result = await handler.HandleAsync(
            new Features.VerifyMfaCode.VerifyMfaCodeCommand(
                fixture.Challenges.Issue(user.Id), CodeFor(enrol.Value.Secret)),
            TestContext.Current.CancellationToken);

        result.IsSuccess.ShouldBeTrue();
    }

    private string CodeFor(string secret) =>
        TotpProbe.CodeForStep(secret, TotpProbe.StepAt(fixture.Clock.UtcNow));

    private async Task<SharedKernel.Results.Result<GetMyProfileResponse>> GetProfileAsync(int userId)
    {
        await using var context = fixture.NewContext();
        return await new GetMyProfileHandler(context)
            .HandleAsync(new GetMyProfileQuery(userId), TestContext.Current.CancellationToken);
    }

    private async Task<SharedKernel.Results.Result<ChangeMyPasswordResponse>> ChangePasswordAsync(
        int userId, string current, string next)
    {
        await using var context = fixture.NewContext();
        return await new ChangeMyPasswordHandler(context, fixture.Hasher, fixture.Clock)
            .HandleAsync(new ChangeMyPasswordCommand(userId, current, next), TestContext.Current.CancellationToken);
    }

    private async Task<SharedKernel.Results.Result<EnrolMfaResponse>> EnrolAsync(int userId)
    {
        await using var context = fixture.NewContext();
        return await new EnrolMfaHandler(context, fixture.Totp, fixture.Secrets, fixture.Clock)
            .HandleAsync(new EnrolMfaCommand(userId), TestContext.Current.CancellationToken);
    }

    private async Task<SharedKernel.Results.Result<ConfirmMfaEnrolmentResponse>> ConfirmAsync(
        int userId, string code)
    {
        await using var context = fixture.NewContext();
        return await new ConfirmMfaEnrolmentHandler(
                context, fixture.Totp, fixture.Secrets, fixture.Hasher, fixture.Clock)
            .HandleAsync(new ConfirmMfaEnrolmentCommand(userId, code), TestContext.Current.CancellationToken);
    }

    private async Task<SharedKernel.Results.Result<RegenerateRecoveryCodesResponse>> RegenerateAsync(
        int userId, string code)
    {
        await using var context = fixture.NewContext();
        return await new RegenerateRecoveryCodesHandler(
                context, fixture.Totp, fixture.Secrets, fixture.Hasher, fixture.Clock)
            .HandleAsync(new RegenerateRecoveryCodesCommand(userId, code), TestContext.Current.CancellationToken);
    }
}
