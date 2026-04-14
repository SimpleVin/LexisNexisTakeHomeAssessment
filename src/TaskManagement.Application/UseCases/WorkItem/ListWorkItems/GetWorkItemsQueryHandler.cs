using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Pagination;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.WorkItem.Models;
using TaskManagement.Application.Common.Models.Interface.WorkItem;

namespace TaskManagement.Application.UseCases.WorkItem.ListWorkItems;

public sealed class GetWorkItemsQueryHandler(IWorkItemRepository repository)
    : IRequestHandler<GetWorkItemsQuery, ApplicationResult<PagedResult<WorkItemDto>>>
{
    public async Task<ApplicationResult<PagedResult<WorkItemDto>>> Handle(GetWorkItemsQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var skip = (page - 1) * pageSize;
        var criteria = new WorkItemListCriteria(
            request.Q,
            request.Status,
            request.Priority,
            request.AssigneeId,
            request.CreatedFrom,
            request.CreatedTo);
        var (items, totalCount) = await repository.SearchWorkItemsPaged(criteria, skip, pageSize, cancellationToken);
        var pageResult = new PagedResult<WorkItemDto>(items, page, pageSize, totalCount);
        return ApplicationResult<PagedResult<WorkItemDto>>.Ok(pageResult);
    }
}
