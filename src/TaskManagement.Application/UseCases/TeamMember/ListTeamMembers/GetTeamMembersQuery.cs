using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Pagination;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Application.UseCases.TeamMember.ListTeamMembers;

public sealed record GetTeamMembersQuery(string? Q, int? Page, int? PageSize) : IRequest<ApplicationResult<PagedResult<TeamMemberDto>>>;
