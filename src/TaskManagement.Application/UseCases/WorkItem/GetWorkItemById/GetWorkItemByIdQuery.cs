using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Application.UseCases.WorkItem.GetWorkItemById;

public sealed record GetWorkItemByIdQuery(Guid Id) : IRequest<ApplicationResult<WorkItemDto>>;
