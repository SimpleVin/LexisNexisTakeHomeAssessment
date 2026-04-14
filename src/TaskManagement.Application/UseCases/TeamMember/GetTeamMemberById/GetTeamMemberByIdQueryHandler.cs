using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.TeamMember;

namespace TaskManagement.Application.UseCases.TeamMember.GetTeamMemberById;

public sealed class GetTeamMemberByIdQueryHandler(ITeamMemberRepository repository)
    : IRequestHandler<GetTeamMemberByIdQuery, ApplicationResult<TeamMemberDto>>
{
    public async Task<ApplicationResult<TeamMemberDto>> Handle(GetTeamMemberByIdQuery request, CancellationToken cancellationToken)
    {
        var member = await repository.GetTeamMemberById(request.Id, cancellationToken);
        return member is null
            ? ApplicationResult<TeamMemberDto>.Fail(
                ApplicationErrorCodes.NotFound,
                "Not found.")
            : ApplicationResult<TeamMemberDto>.Ok(member);
    }
}
