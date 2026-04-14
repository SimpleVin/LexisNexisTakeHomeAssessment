using MediatR;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Application.UseCases.TeamMember.DeleteTeamMember;

public sealed record DeleteTeamMemberCommand(Guid Id) : IRequest<ApplicationUnitResult>;
