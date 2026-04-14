using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.Identity;
using TaskManagement.Application.Common.Models.Interface.TeamMember;

namespace TaskManagement.Application.UseCases.TeamMember.UpdateTeamMember;

public sealed class UpdateTeamMemberCommandHandler(ITeamMemberRepository repository, ICurrentIdentity currentIdentity)
    : IRequestHandler<UpdateTeamMemberCommand, ApplicationResult<TeamMemberDto>>
{
    public async Task<ApplicationResult<TeamMemberDto>> Handle(UpdateTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentIdentity.TeamMemberId;
        if (actorId is null)
        {
            return ApplicationResult<TeamMemberDto>.Fail(
                ApplicationErrorCodes.Forbidden,
                "Not authorized.");
        }

        var existing = await repository.GetTeamMemberById(request.Id, cancellationToken);
        if (existing is null)
        {
            return ApplicationResult<TeamMemberDto>.Fail(
                ApplicationErrorCodes.NotFound,
                "Not found.");
        }

        var now = DateTimeOffset.UtcNow;
        var updated = existing with
        {
            Name = request.Name,
            Email = request.Email,
            UpdatedAt = now,
            UpdatedById = actorId.Value,
        };
        var saved = await repository.UpdateTeamMember(updated, cancellationToken);
        if (saved is null)
        {
            return ApplicationResult<TeamMemberDto>.Fail(
                ApplicationErrorCodes.NotFound,
                "Not found.");
        }

        return ApplicationResult<TeamMemberDto>.Ok(saved);
    }
}
