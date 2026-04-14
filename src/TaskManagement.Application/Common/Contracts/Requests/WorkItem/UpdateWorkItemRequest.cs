using TaskManagement.Application.WorkItem.Models;

namespace TaskManagement.Application.Common.Contracts.Requests.WorkItem;

public sealed record UpdateWorkItemRequest(
    string Title,
    string? Description,
    WorkItemStatus? Status,
    WorkItemPriority Priority,
    Guid? AssigneeId);
