namespace TaskManagement.Application.WorkItem.Models;

public sealed record WorkItemListCriteria(
    string? TitleSearch,
    WorkItemStatus? Status,
    WorkItemPriority? Priority,
    Guid? AssigneeId,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo);
