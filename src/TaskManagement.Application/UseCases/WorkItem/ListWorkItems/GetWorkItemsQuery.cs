using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Pagination;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.WorkItem.Models;

namespace TaskManagement.Application.UseCases.WorkItem.ListWorkItems;

public sealed record GetWorkItemsQuery(
    string? Q,
    WorkItemStatus? Status,
    WorkItemPriority? Priority,
    Guid? AssigneeId,
    DateTimeOffset? CreatedFrom,
    DateTimeOffset? CreatedTo,
    int? Page,
    int? PageSize) : IRequest<ApplicationResult<PagedResult<WorkItemDto>>>;
