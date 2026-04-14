using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Application.UseCases.TeamMember.GetTeamMemberById;

public sealed record GetTeamMemberByIdQuery(Guid Id) : IRequest<ApplicationResult<TeamMemberDto>>;
