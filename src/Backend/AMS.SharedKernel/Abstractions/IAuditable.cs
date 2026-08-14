namespace AMS.SharedKernel.Abstractions;

/// <summary>
/// The four audit columns the design script puts on every table a person
/// edits. Implemented so the stamping interceptor can fill them in one place
/// instead of every handler remembering to (docs/03 §2).
/// </summary>
public interface IAuditable
{
    DateTime CreatedOnUtc { get; set; }

    string? CreatedBy { get; set; }

    DateTime? ModifiedOnUtc { get; set; }

    string? ModifiedBy { get; set; }
}

/// <summary>
/// The two-column variant on link tables, where the only event that ever
/// happens to a row is that somebody granted it.
/// </summary>
public interface IGrantable
{
    DateTime GrantedOnUtc { get; set; }

    string? GrantedBy { get; set; }
}
