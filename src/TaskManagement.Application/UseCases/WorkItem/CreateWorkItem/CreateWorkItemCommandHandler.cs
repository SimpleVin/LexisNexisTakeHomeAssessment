using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.Identity;
using TaskManagement.Application.Common.Models.Interface.TeamMember;
using TaskManagement.Application.WorkItem.Models;
using TaskManagement.Application.Common.Models.Interface.WorkItem;

namespace TaskManagement.Application.UseCases.WorkItem.CreateWorkItem;

public sealed class CreateWorkItemCommandHandler(
    IWorkItemRepository repository,
    ITeamMemberRepository teamMembers,
    ICurrentIdentity currentIdentity)
    : IRequestHandler<CreateWorkItemCommand, ApplicationResult<WorkItemDto>>
{
    public async Task<ApplicationResult<WorkItemDto>> Handle(CreateWorkItemCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentIdentity.TeamMemberId;
        if (actorId is null)
        {
            return ApplicationResult<WorkItemDto>.Fail(
                ApplicationErrorCodes.Forbidden,
                "Not authorized.");
        }

        if (request.AssigneeId is { } assigneeId && !await teamMembers.ExistsTeamMemberById(assigneeId, cancellationToken))
        {
            return ApplicationResult<WorkItemDto>.Fail(
                ApplicationErrorCodes.Validation,
                "Invalid assignee.");
        }

        var assignerId = actorId.Value;

        var now = DateTimeOffset.UtcNow;
        var dto = new WorkItemDto(
            Guid.NewGuid(),
            request.Title,
            request.Description,
            request.Status ?? WorkItemStatusDefaults.ForCreate,
            request.Priority ?? WorkItemPriorityDefaults.ForCreate,
            request.AssigneeId,
            assignerId,
            now,
            now,
            actorId.Value,
            null,
            null,
            null);

        var created = await repository.AddWorkItem(dto, cancellationToken);
        return ApplicationResult<WorkItemDto>.Ok(created);
    }
}
