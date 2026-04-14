namespace TaskManagement.Application.Common.Contracts.DTO.TeamMember;

public sealed record TeamMemberDto(
    Guid Id,
    string Name,
    string Email,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    Guid? CreatedById,
    Guid? UpdatedById,
    DateTimeOffset? DeletedAt,
    Guid? DeletedById);
