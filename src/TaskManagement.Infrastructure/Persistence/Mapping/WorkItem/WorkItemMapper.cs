using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.WorkItem.Models;
using DomainWorkItem = TaskManagement.Domain.Entities.WorkItem;
using DomainPriority = TaskManagement.Domain.Enums.WorkItemPriority;
using DomainStatus = TaskManagement.Domain.Enums.WorkItemStatus;

namespace TaskManagement.Infrastructure.Persistence.Mapping.WorkItem;

internal static class WorkItemMapper
{
    public static WorkItemDto MapToWorkItemDto(DomainWorkItem entity) =>
        new(
            entity.Id,
            entity.Title,
            entity.Description,
            MapToApplicationWorkItemStatus(entity.Status),
            (WorkItemPriority)(int)entity.Priority,
            entity.AssigneeId,
            entity.AssignerId,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.CreatedById,
            entity.UpdatedById,
            entity.DeletedAt,
            entity.DeletedById);

    public static void CopyWorkItemDtoOntoEntity(DomainWorkItem entity, WorkItemDto dto)
    {
        entity.Id = dto.Id;
        entity.Title = dto.Title;
        entity.Description = dto.Description;
        entity.Status = MapToDomainWorkItemStatus(dto.Status);
        entity.Priority = (DomainPriority)(int)dto.Priority;
        entity.AssigneeId = dto.AssigneeId;
        entity.AssignerId = dto.AssignerId;
        entity.CreatedAt = dto.CreatedAt;
        entity.UpdatedAt = dto.UpdatedAt;
        entity.CreatedById = dto.CreatedById;
        entity.UpdatedById = dto.UpdatedById;
        entity.DeletedAt = dto.DeletedAt;
        entity.DeletedById = dto.DeletedById;
    }

    public static DomainWorkItem CreateWorkItemEntityFromDto(WorkItemDto dto)
    {
        var entity = new DomainWorkItem();
        CopyWorkItemDtoOntoEntity(entity, dto);
        return entity;
    }

    private static WorkItemStatus? MapToApplicationWorkItemStatus(DomainStatus? status) =>
        status is { } s ? (WorkItemStatus)(int)s : null;

    private static DomainStatus? MapToDomainWorkItemStatus(WorkItemStatus? status) =>
        status is { } s ? (DomainStatus)(int)s : null;
}
