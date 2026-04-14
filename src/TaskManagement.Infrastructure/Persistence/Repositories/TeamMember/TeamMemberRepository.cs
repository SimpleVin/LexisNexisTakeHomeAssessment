using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Common.Contracts.DTO.TeamMember;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.Common.Models.Interface.TeamMember;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Mapping.TeamMember;

namespace TaskManagement.Infrastructure.Persistence.Repositories.TeamMember;

public sealed class TeamMemberRepository(AppDbContext db, ILogger<TeamMemberRepository> logger) : ITeamMemberRepository
{
    public async Task<TeamMemberDto?> GetTeamMemberById(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.TeamMembers.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : TeamMemberMapper.MapToTeamMemberDto(entity);
    }

    public async Task<(IReadOnlyList<TeamMemberDto> Items, int TotalCount)> SearchTeamMembersPaged(string? nameSearch, int skip, int take, CancellationToken cancellationToken = default)
    {
        var query = db.TeamMembers.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(nameSearch))
        {
            var term = nameSearch.Trim().ToLowerInvariant();
            query = query.Where(x => x.Name.ToLower().Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var list = await query
            .OrderBy(x => x.Name)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
        return (list.ConvertAll(TeamMemberMapper.MapToTeamMemberDto), totalCount);
    }

    public async Task<TeamMemberDto> AddTeamMember(TeamMemberDto member, CancellationToken cancellationToken = default)
    {
        var entity = TeamMemberMapper.CreateTeamMemberEntityFromDto(member);
        db.TeamMembers.Add(entity);
        await CommitAsync(nameof(AddTeamMember), cancellationToken);
        return TeamMemberMapper.MapToTeamMemberDto(entity);
    }

    public async Task<TeamMemberDto?> UpdateTeamMember(TeamMemberDto member, CancellationToken cancellationToken = default)
    {
        var entity = await db.TeamMembers.FirstOrDefaultAsync(x => x.Id == member.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        TeamMemberMapper.CopyTeamMemberDtoOntoEntity(entity, member);
        await CommitAsync(nameof(UpdateTeamMember), cancellationToken);
        return TeamMemberMapper.MapToTeamMemberDto(entity);
    }

    public async Task<SoftDeleteOutcome> DeleteTeamMemberById(Guid id, Guid? deletedById, CancellationToken cancellationToken = default)
    {
        var entity = await db.TeamMembers.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return SoftDeleteOutcome.NotFound;
        }

        var now = DateTimeOffset.UtcNow;
        entity.DeletedAt = now;
        entity.DeletedById = deletedById;
        entity.UpdatedAt = now;

        var affectedWorkItems = await db.WorkItems
            .Where(w => w.AssigneeId == id || w.AssignerId == id)
            .ToListAsync(cancellationToken);

        foreach (var w in affectedWorkItems)
        {
            if (w.AssigneeId == id)
            {
                w.AssigneeId = null;
            }

            if (w.AssignerId == id)
            {
                w.AssignerId = null;
            }
            w.UpdatedAt = now;
            w.UpdatedById = deletedById;
        }

        await CommitAsync(nameof(DeleteTeamMemberById), cancellationToken);

        return SoftDeleteOutcome.Deleted;
    }

    public Task<bool> ExistsTeamMemberById(Guid id, CancellationToken cancellationToken = default) =>
        db.TeamMembers.AsNoTracking().AnyAsync(x => x.Id == id, cancellationToken);

    private async Task CommitAsync(string operation, CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "EF Core failed to save changes during {Operation}.", operation);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while saving changes during {Operation}.", operation);
            throw;
        }
    }
}
