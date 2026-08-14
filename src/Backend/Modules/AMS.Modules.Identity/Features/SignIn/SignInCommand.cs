using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Identity.Features.SignIn;

/// <summary>
/// Authenticate a username and password. Catalogue: Sign in, Forced password change, Account lockout.
/// </summary>
/// <remarks>
/// <see cref="IPersistsOnFailure"/> because the handler increments the
/// failed-attempt counter and THEN refuses. Rolling that back with the refusal
/// would make every wrong password free and account lockout unreachable.
/// </remarks>
public sealed record SignInCommand(
    string Username,
    string Password) : ICommand<SignInResponse>, IPersistsOnFailure;
