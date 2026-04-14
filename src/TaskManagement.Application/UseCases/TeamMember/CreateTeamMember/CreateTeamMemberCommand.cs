using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Application.UseCases.TeamMember.CreateTeamMember;

public sealed record CreateTeamMemberCommand(string Name, string Email) : IRequest<ApplicationResult<TeamMemberDto>>;
