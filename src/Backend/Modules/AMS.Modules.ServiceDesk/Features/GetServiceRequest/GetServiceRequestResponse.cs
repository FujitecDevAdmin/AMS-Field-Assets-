namespace AMS.Modules.ServiceDesk.Features.GetServiceRequest;

/// <summary>
/// Everything the detail screen draws.
/// </summary>
/// <param name="Id">The ticket.</param>
/// <param name="RequestNumber">What the requester quotes.</param>
/// <param name="RequestKind">SupportTicket, AssetIssue or NewService.</param>
/// <param name="Subject">The one-line summary.</param>
/// <param name="Description">What the requester wrote.</param>
/// <param name="Priority">Low, Medium, High or Critical.</param>
/// <param name="RequestStatusId">Where it is.</param>
/// <param name="StatusName">Resolved once here so the screen need not hold the status list.</param>
/// <param name="IsClosedState">Whether the ticket is finished. What every open-queue filter tests.</param>
/// <param name="RequestCategoryId">Classification, if any.</param>
/// <param name="CategoryName">Resolved for display.</param>
/// <param name="RequestSubCategoryId">Finer classification, if any.</param>
/// <param name="SubCategoryName">Resolved for display.</param>
/// <param name="AssetId">Assets.Asset, id only (rule 2).</param>
/// <param name="ManualAssetText">What the requester typed when the asset is not on record.</param>
/// <param name="RequestedByEmployeeId">Who asked.</param>
/// <param name="OnBehalfOfEmployeeId">Who it is for, when somebody raised it for them.</param>
/// <param name="LocationId">The site.</param>
/// <param name="AssignedToUserId">The technician, if one holds it.</param>
/// <param name="AssignedTeamId">The team, if it sits with a team rather than a person.</param>
/// <param name="AssignedTeamName">Resolved for display.</param>
/// <param name="AssignedOnUtc">When it was last handed to somebody.</param>
/// <param name="ResolvedOnUtc">When a fix was recorded.</param>
/// <param name="ClosedOnUtc">When it was closed.</param>
/// <param name="Resolution">What was done.</param>
/// <param name="ResponseDueOnUtc">When somebody must have replied by.</param>
/// <param name="ResolutionDueOnUtc">When it must be fixed by.</param>
/// <param name="FirstResponseOnUtc">When somebody first did, stamped once and never again.</param>
/// <param name="IsSlaPaused">Whether the clock is frozen by the current status.</param>
/// <param name="IsSlaOverdue">Persisted, not derived, so the queue can sort on it.</param>
/// <param name="ResolutionConsumedMinutes">Operational minutes spent, not wall clock.</param>
/// <param name="CreatedOnUtc">When it was raised.</param>
/// <param name="NewService">The joiner/kit detail, on a NewService request only.</param>
/// <param name="History">Conversations and History as one chronological list.</param>
/// <param name="Attachments">Files on the ticket, including those that arrived by e-mail.</param>
public sealed record GetServiceRequestResponse(
    int Id,
    string RequestNumber,
    string RequestKind,
    string Subject,
    string? Description,
    string Priority,
    int RequestStatusId,
    string StatusName,
    bool IsClosedState,
    int? RequestCategoryId,
    string? CategoryName,
    int? RequestSubCategoryId,
    string? SubCategoryName,
    int? AssetId,
    string? ManualAssetText,
    int RequestedByEmployeeId,
    int? OnBehalfOfEmployeeId,
    int? LocationId,
    int? AssignedToUserId,
    int? AssignedTeamId,
    string? AssignedTeamName,
    DateTime? AssignedOnUtc,
    DateTime? ResolvedOnUtc,
    DateTime? ClosedOnUtc,
    string? Resolution,
    DateTime? ResponseDueOnUtc,
    DateTime? ResolutionDueOnUtc,
    DateTime? FirstResponseOnUtc,
    bool IsSlaPaused,
    bool IsSlaOverdue,
    int ResolutionConsumedMinutes,
    DateTime CreatedOnUtc,
    GetServiceRequestResponse.NewServiceDetail? NewService,
    IReadOnlyList<GetServiceRequestResponse.HistoryEntry> History,
    IReadOnlyList<GetServiceRequestResponse.Attachment> Attachments)
{
    /// <summary>The New Service questions and the kit asked for.</summary>
    /// <param name="NeedsEmail">Deprecated compatibility field; no longer persisted.</param>
    /// <param name="NeedsErp">Deprecated compatibility field; no longer persisted.</param>
    /// <param name="NeedsDms">Deprecated compatibility field; no longer persisted.</param>
    /// <param name="NeedsVpn">Deprecated compatibility field; no longer persisted.</param>
    /// <param name="RequestCategoryId">The Service category selected.</param>
    /// <param name="RequestSubCategoryId">The selected child sub-category.</param>
    /// <param name="RequiredByDate">The joining date, usually.</param>
    /// <param name="Notes">Anything the four flags do not cover.</param>
    /// <param name="Items">The kit.</param>
    public sealed record NewServiceDetail(
        bool NeedsEmail,
        bool NeedsErp,
        bool NeedsDms,
        bool NeedsVpn,
        int RequestCategoryId,
        int RequestSubCategoryId,
        DateOnly? RequiredByDate,
        string? Notes,
        IReadOnlyList<NewServiceItem> Items);

    /// <summary>One line of kit.</summary>
    /// <param name="AssetTypeId">Assets.AssetType, id only (rule 2).</param>
    /// <param name="Quantity">How many.</param>
    /// <param name="Specification">Anything the standard type does not say.</param>
    public sealed record NewServiceItem(int AssetTypeId, int Quantity, string? Specification);

    /// <summary>One line of the conversation.</summary>
    /// <param name="Id">The entry.</param>
    /// <param name="EntryKind">Transition, Note, Email, Automation, Sla or Escalation.</param>
    /// <param name="EntryText">The one-line summary the timeline shows.</param>
    /// <param name="Body">The long text, when there is one: a note or an e-mail body.</param>
    /// <param name="IsInternal">Hidden from the requester. Never hidden from audit.</param>
    /// <param name="FromStatusId">Where it was, on a transition.</param>
    /// <param name="ToStatusId">Where it went.</param>
    /// <param name="RequestEmailId">The message this entry is about, if it is about one.</param>
    /// <param name="OccurredOnUtc">When.</param>
    /// <param name="PerformedBy">Who, or 'SLA Automation' when nobody.</param>
    public sealed record HistoryEntry(
        long Id,
        string EntryKind,
        string EntryText,
        string? Body,
        bool IsInternal,
        int? FromStatusId,
        int? ToStatusId,
        int? RequestEmailId,
        DateTime OccurredOnUtc,
        string PerformedBy);

    /// <summary>One file.</summary>
    /// <param name="Id">The attachment row.</param>
    /// <param name="AttachmentType">Requester, Resolution or Email.</param>
    /// <param name="FileName">What to show.</param>
    /// <param name="ContentType">What it is.</param>
    /// <param name="SizeBytes">How big.</param>
    /// <param name="RequestEmailId">The message it arrived with, if it did.</param>
    /// <param name="UploadedOnUtc">When.</param>
    public sealed record Attachment(
        int Id,
        string AttachmentType,
        string? FileName,
        string? ContentType,
        long? SizeBytes,
        int? RequestEmailId,
        DateTime UploadedOnUtc);
}
