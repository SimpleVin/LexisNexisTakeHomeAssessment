using MediatR;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.Identity;
using TaskManagement.Application.Common.Models.Interface.WorkItem;

namespace TaskManagement.Application.UseCases.WorkItem.DeleteWorkItem;

public sealed class DeleteWorkItemCommandHandler(
    IWorkItemRepository repository,
    ICurrentIdentity currentIdentity,
    ILogger<DeleteWorkItemCommandHandler> logger)
    : IRequestHandler<DeleteWorkItemCommand, ApplicationUnitResult>
{
    public async Task<ApplicationUnitResult> Handle(DeleteWorkItemCommand request, CancellationToken cancellationToken)
    {
        if (currentIdentity.TeamMemberId is null)
        {
            return ApplicationUnitResult.Fail(
                ApplicationErrorCodes.Forbidden,
                "Not authorized.");
        }

        var outcome = await repository.DeleteWorkItemById(request.Id, currentIdentity.TeamMemberId, cancellationToken);
        if (outcome == SoftDeleteOutcome.NotFound)
        {
            logger.LogWarning(
                "Delete work item failed: not found or already removed. WorkItemId={WorkItemId}",
                request.Id);
            return ApplicationUnitResult.Fail(
                ApplicationErrorCodes.NotFound,
                "Not found.");
        }

        logger.LogInformation("Work item soft-deleted. WorkItemId={WorkItemId}", request.Id);
        return ApplicationUnitResult.Ok("Deleted.");
    }
}
