using AMS.SharedKernel.Messaging;

namespace AMS.Modules.Assets.Features.UpdateCustomField;

/// <summary>
/// Edit a custom field definition or retire it.
/// </summary>
public sealed record UpdateCustomFieldCommand(
    int Id,
    string DisplayLabel,
    bool IsRequired,
    decimal? MinValue,
    decimal? MaxValue,
    string? ValidationRegex,
    string? DefaultValue,
    int DisplayOrder,
    bool IsActive) : ICommand<UpdateCustomFieldResponse>;
