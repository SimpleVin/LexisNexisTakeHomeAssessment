using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Results;

namespace TaskManagement.Application.Common.Models.Interface.TeamMember;

public interface ITeamMemberRepository
{
    Task<TeamMemberDto?> GetTeamMemberById(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<TeamMemberDto> Items, int TotalCount)> SearchTeamMembersPaged(
        string? nameSearch,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<TeamMemberDto> AddTeamMember(TeamMemberDto member, CancellationToken cancellationToken = default);
    Task<TeamMemberDto?> UpdateTeamMember(TeamMemberDto member, CancellationToken cancellationToken = default);
    Task<SoftDeleteOutcome> DeleteTeamMemberById(Guid id, Guid? deletedById, CancellationToken cancellationToken = default);
    Task<bool> ExistsTeamMemberById(Guid id, CancellationToken cancellationToken = default);
}
