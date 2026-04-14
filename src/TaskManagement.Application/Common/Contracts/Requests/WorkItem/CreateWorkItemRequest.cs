using TaskManagement.Application.WorkItem.Models;

namespace TaskManagement.Application.Common.Contracts.Requests.WorkItem;

public sealed record CreateWorkItemRequest(
    string Title,
    string? Description,
    WorkItemStatus? Status,
    WorkItemPriority? Priority,
    Guid? AssigneeId);
