using TaskManagement.Application.WorkItem.Models;

namespace TaskManagement.Application.Common.Contracts.DTO.WorkItem;

public sealed record WorkItemDto(
    Guid Id,
    string Title,
    string? Description,
    WorkItemStatus? Status,
    WorkItemPriority Priority,
    Guid? AssigneeId,
    Guid? AssignerId,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? CreatedById,
    Guid? UpdatedById,
    DateTimeOffset? DeletedAt,
    Guid? DeletedById);
