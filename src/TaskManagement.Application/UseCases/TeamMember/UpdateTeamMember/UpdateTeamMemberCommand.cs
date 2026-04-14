using MediatR;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Application.UseCases.TeamMember.UpdateTeamMember;

public sealed record UpdateTeamMemberCommand(Guid Id, string Name, string Email) : IRequest<ApplicationResult<TeamMemberDto>>;
