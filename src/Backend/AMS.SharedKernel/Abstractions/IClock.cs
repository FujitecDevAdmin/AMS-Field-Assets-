namespace AMS.SharedKernel.Abstractions;

/// <summary>
/// The only source of "now". docs/02BACKENDCODINGSTANDARDS.md §4 forbids
/// <c>DateTime.UtcNow</c> in handlers and domain code: a rule that depends on
/// the wall clock cannot be tested, and an SLA system that cannot be tested
/// is one nobody can argue with when it is wrong.
/// </summary>
public interface IClock
{
    DateTime UtcNow { get; }
}
