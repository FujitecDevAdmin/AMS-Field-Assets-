using Microsoft.Extensions.Logging;

namespace AMS.Modules.Notifications.Sending;

/// <summary>Every line the dispatcher writes.</summary>
/// <remarks>
/// <para>
/// Source-generated rather than called through <c>ILogger.LogWarning</c>
/// directly. CA1873 objects to the latter, and it is right to: a log call
/// evaluates and boxes its arguments whether or not the level is enabled, and
/// this code runs every fifteen seconds for the life of the process.
/// </para>
/// <para>
/// The first logging in this codebase, so it sets the pattern: the messages
/// live in one place per component, with their levels and ids, and a reader can
/// see everything a thing can say without reading what it does.
/// </para>
/// </remarks>
internal static partial class DispatcherLog
{
    [LoggerMessage(
        EventId = 1000,
        Level = LogLevel.Information,
        Message = "E-mail dispatcher started; polling every {PollSeconds}s.")]
    public static partial void Started(ILogger logger, int pollSeconds);

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "E-mail dispatcher stopped.")]
    public static partial void Stopped(ILogger logger);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Error,
        Message = "The e-mail dispatcher pass failed. It will try again.")]
    public static partial void PassFailed(ILogger logger, Exception exception);

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Warning,
        Message = "No active e-mail profile is configured; nothing was sent.")]
    public static partial void NoProfile(ILogger logger);

    [LoggerMessage(
        EventId = 1004,
        Level = LogLevel.Error,
        Message = "The stored SMTP password for profile {ProfileName} cannot be read. "
            + "Re-enter it on the e-mail settings screen.")]
    public static partial void PasswordUnreadable(
        ILogger logger,
        Exception exception,
        string profileName);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Warning,
        Message = "Attempt {Attempt} for message {MessageId} failed; it stays queued.")]
    public static partial void AttemptFailed(
        ILogger logger,
        Exception exception,
        int attempt,
        long messageId);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Error,
        Message = "Giving up on message {MessageId} to {Recipient} after {Attempts} attempts.")]
    public static partial void GaveUp(
        ILogger logger,
        Exception exception,
        long messageId,
        string recipient,
        int attempts);
}
