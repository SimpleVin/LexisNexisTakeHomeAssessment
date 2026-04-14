using MediatR;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.Identity;
using TaskManagement.Application.Common.Models.Interface.TeamMember;

namespace TaskManagement.Application.UseCases.TeamMember.DeleteTeamMember;

public sealed class DeleteTeamMemberCommandHandler(
    ITeamMemberRepository repository,
    ICurrentIdentity currentIdentity,
    ILogger<DeleteTeamMemberCommandHandler> logger)
    : IRequestHandler<DeleteTeamMemberCommand, ApplicationUnitResult>
{
    public async Task<ApplicationUnitResult> Handle(DeleteTeamMemberCommand request, CancellationToken cancellationToken)
    {
        if (currentIdentity.TeamMemberId is null)
        {
            return ApplicationUnitResult.Fail(
                ApplicationErrorCodes.Forbidden,
                "Not authorized.");
        }

        var outcome = await repository.DeleteTeamMemberById(request.Id, currentIdentity.TeamMemberId, cancellationToken);
        if (outcome == SoftDeleteOutcome.NotFound)
        {
            logger.LogWarning(
                "Delete team member failed: not found or already removed. TeamMemberId={TeamMemberId}",
                request.Id);
            return ApplicationUnitResult.Fail(
                ApplicationErrorCodes.NotFound,
                "Not found.");
        }

        logger.LogInformation("Team member soft-deleted. TeamMemberId={TeamMemberId}", request.Id);
        return ApplicationUnitResult.Ok("Deleted.");
    }
}
