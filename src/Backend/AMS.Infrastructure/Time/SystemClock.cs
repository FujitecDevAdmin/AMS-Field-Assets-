using AMS.SharedKernel.Abstractions;

namespace AMS.Infrastructure.Time;

/// <summary>
/// The wall clock, in UTC. The only implementation of <see cref="IClock"/> that
/// ships.
/// </summary>
/// <remarks>
/// <para>
/// Handlers and domain code are forbidden from calling
/// <see cref="DateTime.UtcNow"/> directly (docs/02 §4), which leaves exactly one
/// place in the application allowed to — here.
/// </para>
/// <para>
/// UTC, never local. Every instant in this design is <c>datetime2</c> named
/// <c>*OnUtc</c>, the SLA service converts once at the edge using the branch's
/// <c>TimeZoneId</c>, and a clock that returned local time would put that
/// conversion somewhere nobody could find it.
/// </para>
/// </remarks>
public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
