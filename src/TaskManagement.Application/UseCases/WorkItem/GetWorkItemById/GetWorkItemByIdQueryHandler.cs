using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.WorkItem;

namespace TaskManagement.Application.UseCases.WorkItem.GetWorkItemById;

public sealed class GetWorkItemByIdQueryHandler(IWorkItemRepository repository)
    : IRequestHandler<GetWorkItemByIdQuery, ApplicationResult<WorkItemDto>>
{
    public async Task<ApplicationResult<WorkItemDto>> Handle(GetWorkItemByIdQuery request, CancellationToken cancellationToken)
    {
        var item = await repository.GetWorkItemById(request.Id, cancellationToken);
        return item is null
            ? ApplicationResult<WorkItemDto>.Fail(
                ApplicationErrorCodes.NotFound,
                "Not found.")
            : ApplicationResult<WorkItemDto>.Ok(item);
    }
}
