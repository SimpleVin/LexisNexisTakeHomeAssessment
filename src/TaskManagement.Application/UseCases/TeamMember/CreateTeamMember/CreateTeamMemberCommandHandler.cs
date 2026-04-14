using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.Identity;
using TaskManagement.Application.Common.Models.Interface.TeamMember;

namespace TaskManagement.Application.UseCases.TeamMember.CreateTeamMember;

public sealed class CreateTeamMemberCommandHandler(ITeamMemberRepository repository, ICurrentIdentity currentIdentity)
    : IRequestHandler<CreateTeamMemberCommand, ApplicationResult<TeamMemberDto>>
{
    public async Task<ApplicationResult<TeamMemberDto>> Handle(CreateTeamMemberCommand request, CancellationToken cancellationToken)
    {
        var actorId = currentIdentity.TeamMemberId;
        if (actorId is null)
        {
            return ApplicationResult<TeamMemberDto>.Fail(
                ApplicationErrorCodes.Forbidden,
                "Not authorized.");
        }

        var now = DateTimeOffset.UtcNow;
        var dto = new TeamMemberDto(
            Guid.NewGuid(),
            request.Name,
            request.Email,
            now,
            now,
            actorId.Value,
            null,
            null,
            null);
        var created = await repository.AddTeamMember(dto, cancellationToken);
        return ApplicationResult<TeamMemberDto>.Ok(created);
    }
}
