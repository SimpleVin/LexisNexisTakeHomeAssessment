using MediatR;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Application.UseCases.WorkItem.DeleteWorkItem;

public sealed record DeleteWorkItemCommand(Guid Id) : IRequest<ApplicationUnitResult>;
