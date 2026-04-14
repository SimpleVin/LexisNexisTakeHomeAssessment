using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.WorkItem.Models;

namespace TaskManagement.Application.UseCases.WorkItem.UpdateWorkItem;

public sealed record UpdateWorkItemCommand(
    Guid Id,
    string Title,
    string? Description,
    WorkItemStatus? Status,
    WorkItemPriority Priority,
    Guid? AssigneeId) : IRequest<ApplicationResult<WorkItemDto>>;
