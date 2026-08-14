using AMS.Modules.Identity.Features.VerifyMfaCode;
using Microsoft.EntityFrameworkCore;

namespace AMS.Modules.Identity.Tests;

/// <summary>
/// Catalogue feature: Multi-factor authentication — "verify a code at sign-in,
/// keep single-use recovery codes".
/// </summary>
[Collection(nameof(IdentityCollectionDefinition))]
public sealed class VerifyMfaCodeTests(IdentityFixture fixture)
{
    private const string Secret = "JBSWY3DPEHPK3PXP";
    private const int StepSeconds = 30;

    /// <summary>The fixture clock's starting point, restored by tests that move it.</summary>
    private static readonly DateTime Baseline = new(2026, 8, 12, 9, 0, 0, DateTimeKind.Utc);

    // ------------------------------------------------------------- positive

    [Fact]
    public async Task A_valid_authenticator_code_completes_the_sign_in()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("alice", mfaEnabled: true, mfaSecret: Secret);

        var result = await HandleAsync(fixture.Challenges.Issue(user.Id), CurrentCode());

        result.IsSuccess.ShouldBeTrue();
        result.Value.UserId.ShouldBe(user.Id);
        result.Value.UsedRecoveryCode.ShouldBeFalse();
    }

    [Fact]
    public async Task A_recovery_code_completes_the_sign_in_and_says_so()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("bob", mfaEnabled: true, mfaSecret: Secret);
        await fixture.AddRecoveryCodeAsync(user.Id, "RESCUE-1234");
        await fixture.AddRecoveryCodeAsync(user.Id, "RESCUE-5678");

        var result = await HandleAsync(fixture.Challenges.Issue(user.Id), "RESCUE-1234");

        result.IsSuccess.ShouldBeTrue();
        result.Value.UsedRecoveryCode.ShouldBeTrue();
        result.Value.RemainingRecoveryCodes.ShouldBe(1);
    }

    // ------------------------------------------------------------- negative

    [Fact]
    public async Task A_wrong_code_is_refused()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("carol", mfaEnabled: true, mfaSecret: Secret);

        var result = await HandleAsync(fixture.Challenges.Issue(user.Id), "000000");

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Mfa.Invalid");
    }

    [Fact]
    public async Task A_tampered_challenge_token_is_refused()
    {
        await fixture.ResetAsync();
        await fixture.AddUserAsync("dave", mfaEnabled: true, mfaSecret: Secret);

        var result = await HandleAsync("not-a-real-token", CurrentCode());

        result.IsSuccess.ShouldBeFalse();
        result.Error!.Code.ShouldBe("Mfa.ChallengeExpired");
    }

    [Fact]
    public async Task A_locked_user_cannot_complete_the_second_step()
    {
        // The lock may have been applied between the two steps.
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("erin", mfaEnabled: true, mfaSecret: Secret);
        var token = fixture.Challenges.Issue(user.Id);

        await fixture.ExecuteAsync($"UPDATE [Identity].[User] SET [IsLocked] = 1 WHERE [Id] = {user.Id};");

        (await HandleAsync(token, CurrentCode())).IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task A_user_who_is_not_enrolled_is_refused()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("frank");

        (await HandleAsync(fixture.Challenges.Issue(user.Id), "123456")).IsSuccess.ShouldBeFalse();
    }

    // ----------------------------------------------------------------- edge

    [Fact]
    public async Task A_recovery_code_works_exactly_once()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("grace", mfaEnabled: true, mfaSecret: Secret);
        await fixture.AddRecoveryCodeAsync(user.Id, "ONE-SHOT");

        (await HandleAsync(fixture.Challenges.Issue(user.Id), "ONE-SHOT")).IsSuccess.ShouldBeTrue();

        var second = await HandleAsync(fixture.Challenges.Issue(user.Id), "ONE-SHOT");
        second.IsSuccess.ShouldBeFalse("a spent recovery code must never work again");
    }

    [Fact]
    public async Task Spending_a_recovery_code_marks_that_row_and_leaves_the_others()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("heidi", mfaEnabled: true, mfaSecret: Secret);
        await fixture.AddRecoveryCodeAsync(user.Id, "KEEP-ME");
        await fixture.AddRecoveryCodeAsync(user.Id, "SPEND-ME");

        await HandleAsync(fixture.Challenges.Issue(user.Id), "SPEND-ME");

        await using var context = fixture.NewContext();
        var codes = await context.UserRecoveryCodes.Where(c => c.UserId == user.Id).ToListAsync();

        codes.Count(c => c.UsedOnUtc is not null).ShouldBe(1);
        codes.Count(c => c.UsedOnUtc is null).ShouldBe(1);
    }

    [Fact]
    public async Task A_code_from_the_previous_time_step_is_still_accepted()
    {
        // One step of drift each way, deliberately: phones are not in sync
        // with the server to the second.
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("ivan", mfaEnabled: true, mfaSecret: Secret);

        var previousStep = TotpProbe.StepAt(fixture.Clock.UtcNow);
        var codeForPreviousStep = TotpProbe.CodeForStep(Secret, previousStep);

        fixture.Clock.Advance(TimeSpan.FromSeconds(StepSeconds));
        TotpProbe.StepAt(fixture.Clock.UtcNow).ShouldBe(previousStep + 1, "the clock must have moved one step");

        (await HandleAsync(fixture.Challenges.Issue(user.Id), codeForPreviousStep)).IsSuccess.ShouldBeTrue();
        fixture.Clock.UtcNow = Baseline;
    }

    [Fact]
    public async Task A_code_from_far_outside_the_window_is_refused()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("judy", mfaEnabled: true, mfaSecret: Secret);

        var stale = CurrentCode();
        fixture.Clock.Advance(TimeSpan.FromMinutes(10));

        var result = await HandleAsync(fixture.Challenges.Issue(user.Id), stale);
        fixture.Clock.UtcNow = Baseline;

        result.IsSuccess.ShouldBeFalse();
    }

    [Fact]
    public async Task Completing_the_second_step_clears_the_failure_count()
    {
        await fixture.ResetAsync();
        var user = await fixture.AddUserAsync("ken", mfaEnabled: true, mfaSecret: Secret);
        await fixture.ExecuteAsync($"UPDATE [Identity].[User] SET [FailedLoginAttempts] = 3 WHERE [Id] = {user.Id};");

        await HandleAsync(fixture.Challenges.Issue(user.Id), CurrentCode());

        (await fixture.ReloadAsync(user.Id)).FailedLoginAttempts.ShouldBe(0);
    }

    /// <summary>The code for the step the test clock is currently in.</summary>
    private string CurrentCode() =>
        TotpProbe.CodeForStep(Secret, TotpProbe.StepAt(fixture.Clock.UtcNow));

    private async Task<SharedKernel.Results.Result<VerifyMfaCodeResponse>> HandleAsync(string token, string code)
    {
        await using var context = fixture.NewContext();
        var handler = new VerifyMfaCodeHandler(
            context, fixture.Challenges, fixture.Totp, fixture.Secrets,
            IdentityFixture.NewEffectiveAccess(context), fixture.AccessTokens,
            fixture.Hasher, fixture.Clock);
        return await handler.HandleAsync(
            new VerifyMfaCodeCommand(token, code), TestContext.Current.CancellationToken);
    }
}
