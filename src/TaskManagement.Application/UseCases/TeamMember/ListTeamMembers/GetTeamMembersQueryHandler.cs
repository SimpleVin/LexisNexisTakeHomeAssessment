using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Pagination;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.TeamMember;

namespace TaskManagement.Application.UseCases.TeamMember.ListTeamMembers;

public sealed class GetTeamMembersQueryHandler(ITeamMemberRepository repository)
    : IRequestHandler<GetTeamMembersQuery, ApplicationResult<PagedResult<TeamMemberDto>>>
{
    public async Task<ApplicationResult<PagedResult<TeamMemberDto>>> Handle(GetTeamMembersQuery request, CancellationToken cancellationToken)
    {
        var (page, pageSize) = Pagination.Normalize(request.Page, request.PageSize);
        var skip = (page - 1) * pageSize;
        var (items, totalCount) = await repository.SearchTeamMembersPaged(request.Q, skip, pageSize, cancellationToken);
        var pageResult = new PagedResult<TeamMemberDto>(items, page, pageSize, totalCount);
        return ApplicationResult<PagedResult<TeamMemberDto>>.Ok(pageResult);
    }
}
