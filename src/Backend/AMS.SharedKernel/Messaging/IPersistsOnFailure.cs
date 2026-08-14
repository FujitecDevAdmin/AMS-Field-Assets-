namespace AMS.SharedKernel.Messaging;

/// <summary>
/// A command whose writes must survive its own failure.
/// </summary>
/// <remarks>
/// <para>
/// Commands are atomic: the dispatcher rolls one back when its handler returns
/// a failure, so a half-applied command cannot survive. This marker is the
/// deliberate exception, and it exists because of exactly one requirement.
/// </para>
/// <para>
/// <c>SignIn</c> increments the failed-attempt counter and then refuses the
/// sign-in. If that counter rolled back with the refusal, every wrong password
/// would cost an attacker nothing and account lockout would never trigger —
/// the handler's own remarks say a rollback there "would hand an attacker
/// unlimited guesses".
/// </para>
/// <para>
/// Reach for this only when a write is a RECORD OF THE ATTEMPT rather than
/// part of the work: lockout counters, audit of a refused action. If the
/// writes are part of the work, the answer is to return the failure before
/// making them.
/// </para>
/// </remarks>
public interface IPersistsOnFailure;
