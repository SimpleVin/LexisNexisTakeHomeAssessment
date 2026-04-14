using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.Identity;
using TaskManagement.Application.Common.Models.Interface.TeamMember;
using TaskManagement.Application.WorkItem.Models;
using TaskManagement.Application.Common.Models.Interface.WorkItem;

namespace TaskManagement.Application.UseCases.WorkItem.UpdateWorkItem;

public sealed class UpdateWorkItemCommandHandler(
    IWorkItemRepository repository,
    ITeamMemberRepository teamMembers,
    ICurrentIdentity currentIdentity)
    : IRequestHandler<UpdateWorkItemCommand, ApplicationResult<WorkItemDto>>
{
    public async Task<ApplicationResult<WorkItemDto>> Handle(UpdateWorkItemCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentIdentity.TeamMemberId;
        if (actorId is null)
        {
            return ApplicationResult<WorkItemDto>.Fail(
                ApplicationErrorCodes.Forbidden,
                "Not authorized.");
        }

        var existing = await repository.GetWorkItemById(request.Id, cancellationToken);
        if (existing is null)
        {
            return ApplicationResult<WorkItemDto>.Fail(
                ApplicationErrorCodes.NotFound,
                "Not found.");
        }

        if (request.AssigneeId is { } assigneeId && !await teamMembers.ExistsTeamMemberById(assigneeId, cancellationToken))
        {
            return ApplicationResult<WorkItemDto>.Fail(
                ApplicationErrorCodes.Validation,
                "Invalid assignee.");
        }

        var now = DateTimeOffset.UtcNow;
        var updated = existing with
        {
            Title = request.Title,
            Description = request.Description,
            Status = request.Status,
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            AssignerId = existing.AssignerId,
            UpdatedAt = now,
            UpdatedById = actorId.Value,
        };

        var saved = await repository.UpdateWorkItem(updated, cancellationToken);
        if (saved is null)
        {
            return ApplicationResult<WorkItemDto>.Fail(
                ApplicationErrorCodes.NotFound,
                "Not found.");
        }

        return ApplicationResult<WorkItemDto>.Ok(saved);
    }
}
