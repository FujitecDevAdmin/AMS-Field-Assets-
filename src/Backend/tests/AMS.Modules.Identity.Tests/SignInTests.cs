using AMS.Modules.Identity.Authentication;
using AMS.Modules.Identity.Features.SignIn;

namespace AMS.Modules.Identity.Tests;

/// <summary>
/// Catalogue features: Sign in, Forced password change, Account lockout.
/// </summary>
[Collection(nameof(IdentityCollectionDefinition))]
public sealed class SignInTests(IdentityFixture fixture)
{
    private const string GoodPassword = "correct horse battery";

    // ------------------------------------------------------------- positive

    [Fact]
    public async Task Correct_credentials_sign_in()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("alice");

        var result = await HandleAsync("alice", GoodPassword);

        result.IsSuccess.ShouldBeTrue();
        result.Value.Username.ShouldBe("alice");
        result.Value.MfaRequired.ShouldBeFalse();
        result.Value.MfaChallengeToken.ShouldBeNull();
    }

    [Fact]
    public async Task A_new_account_is_told_it_must_change_its_password()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("bob", mustChangePassword: true);

        var result = await HandleAsync("bob", GoodPassword);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MustChangePassword.ShouldBeTrue();
    }

    [Fact]
    public async Task An_enrolled_user_is_challenged_and_not_yet_signed_in()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("carol", mfaEnabled: true, mfaSecret: "JBSWY3DPEHPK3PXP");

        var result = await HandleAsync("carol", GoodPassword);

        result.IsSuccess.ShouldBeTrue();
        result.Value.MfaRequired.ShouldBeTrue();
        result.Value.MfaChallengeToken.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Signing_in_records_the_time_and_clears_the_failure_count()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("dave");

        await HandleAsync("dave", "wrong");
        await HandleAsync("dave", GoodPassword);

        var reloaded = await fixture.ReloadAsync(user.Id);
        reloaded.FailedLoginAttempts.ShouldBe(0);
        reloaded.LastLoginOnUtc.ShouldNotBeNull();
    }

    // ------------------------------------------------------------- negative

    [Fact]
    public async Task An_unknown_username_is_refused()
    {
        await fixture.ResetAsync();

        var result = await HandleAsync("nobody", GoodPassword);

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("SignIn.Invalid");
    }

    [Fact]
    public async Task A_wrong_password_is_refused_and_counted()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("erin");

        var result = await HandleAsync("erin", "not the password");

        result.IsSuccess.ShouldBeFalse();
        (await fixture.ReloadAsync(user.Id)).FailedLoginAttempts.ShouldBe(1);
    }

    [Fact]
    public async Task A_deactivated_user_is_refused()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("frank", isActive: false);

        (await HandleAsync("frank", GoodPassword)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task A_locked_user_is_refused_even_with_the_right_password()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("grace", isLocked: true);

        (await HandleAsync("grace", GoodPassword)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Every_failure_returns_the_same_error()
    {
        // Telling "no such user" apart from "wrong password" is how an
        // attacker enumerates usernames, and "your account is locked"
        // confirms one exists.
        await fixture.ResetAsync();
        await fixture.AddUserAsync("heidi");
        await fixture.AddUserAsync("ivan", isLocked: true);

        var unknown = await HandleAsync("nobody-at-all", GoodPassword);
        var wrongPassword = await HandleAsync("heidi", "wrong");
        var locked = await HandleAsync("ivan", GoodPassword);

        unknown.Error!.Code.ShouldBe(wrongPassword.Error!.Code);
        wrongPassword.Error.Code.ShouldBe(locked.Error!.Code);
        unknown.Error.Message.ShouldBe(locked.Error.Message);
    }

    // ----------------------------------------------------------------- edge

    [Fact]
    public async Task The_account_locks_on_the_configured_attempt_and_not_before()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("judy");

        for (var attempt = 1; attempt < LockoutPolicy.MaxFailedAttempts; attempt++)
        {
            await HandleAsync("judy", "wrong");
            (await fixture.ReloadAsync(user.Id)).IsLocked
                .ShouldBeFalse($"must not lock on attempt {attempt}");
        }

        await HandleAsync("judy", "wrong");

        var locked = await fixture.ReloadAsync(user.Id);
        locked.IsLocked.ShouldBeTrue();
        locked.FailedLoginAttempts.ShouldBe(LockoutPolicy.MaxFailedAttempts);
    }

    [Fact]
    public async Task A_success_before_the_threshold_resets_the_count()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("ken");

        for (var attempt = 0; attempt < LockoutPolicy.MaxFailedAttempts - 1; attempt++)
        {
            await HandleAsync("ken", "wrong");
        }

        await HandleAsync("ken", GoodPassword);

        // Without the reset, one more failure days later would lock the
        // account for reasons nobody could reconstruct.
        (await fixture.ReloadAsync(user.Id)).FailedLoginAttempts.ShouldBe(0);

        await HandleAsync("ken", "wrong");
        (await fixture.ReloadAsync(user.Id)).IsLocked.ShouldBeFalse();
    }

    [Fact]
    public async Task Once_locked_the_correct_password_still_does_not_work()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("laura");

        for (var attempt = 0; attempt < LockoutPolicy.MaxFailedAttempts; attempt++)
        {
            await HandleAsync("laura", "wrong");
        }

        (await HandleAsync("laura", GoodPassword)).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Usernames_are_matched_exactly_as_stored()
    {
        // SQL Server's default collation is case-insensitive, so "Mallory"
        // finds "mallory". This test records that behaviour deliberately: if
        // the collation ever changes, sign-in changes with it.
        await fixture.ResetAsync();
        await fixture.AddUserAsync("mallory");

        (await HandleAsync("MALLORY", GoodPassword)).IsSuccess.ShouldBeTrue();
    }

    private async Task<SharedKernel.Results.Result<SignInResponse>> HandleAsync(string username, string password)
    {
        await using var context = fixture.NewContext();
        var handler = new SignInHandler(
            context, fixture.Hasher, fixture.Challenges,
            IdentityFixture.NewEffectiveAccess(context), fixture.AccessTokens, fixture.Clock);
        return await handler.HandleAsync(new SignInCommand(username, password), TestContext.Current.CancellationToken);
    }
}
