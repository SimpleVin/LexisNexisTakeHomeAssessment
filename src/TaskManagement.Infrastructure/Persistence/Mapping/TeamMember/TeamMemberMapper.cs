using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using DomainTeamMember = TaskManagement.Domain.Entities.TeamMember;

namespace TaskManagement.Infrastructure.Persistence.Mapping.TeamMember;

internal static class TeamMemberMapper
{
    public static TeamMemberDto MapToTeamMemberDto(DomainTeamMember entity) =>
        new(
            entity.Id,
            entity.Name,
            entity.Email,
            entity.CreatedAt,
            entity.UpdatedAt,
            entity.CreatedById,
            entity.UpdatedById,
            entity.DeletedAt,
            entity.DeletedById);

    public static void CopyTeamMemberDtoOntoEntity(DomainTeamMember entity, TeamMemberDto dto)
    {
        entity.Id = dto.Id;
        entity.Name = dto.Name;
        entity.Email = dto.Email;
        entity.CreatedAt = dto.CreatedAt;
        entity.UpdatedAt = dto.UpdatedAt;
        entity.CreatedById = dto.CreatedById;
        entity.UpdatedById = dto.UpdatedById;
        entity.DeletedAt = dto.DeletedAt;
        entity.DeletedById = dto.DeletedById;
    }

    public static DomainTeamMember CreateTeamMemberEntityFromDto(TeamMemberDto dto)
    {
        var entity = new DomainTeamMember();
        CopyTeamMemberDtoOntoEntity(entity, dto);
        return entity;
    }
}
