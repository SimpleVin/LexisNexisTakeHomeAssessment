using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.WorkItem.Models;
using TaskManagement.Application.Common.Models.Interface.WorkItem;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Mapping.WorkItem;

namespace TaskManagement.Infrastructure.Persistence.Repositories.WorkItem;

public sealed class WorkItemRepository(AppDbContext db, ILogger<WorkItemRepository> logger) : IWorkItemRepository
{
    public async Task<WorkItemDto?> GetWorkItemById(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.WorkItems.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        return entity is null ? null : WorkItemMapper.MapToWorkItemDto(entity);
    }

    public async Task<(IReadOnlyList<WorkItemDto> Items, int TotalCount)> SearchWorkItemsPaged(
        WorkItemListCriteria criteria,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = db.WorkItems.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.TitleSearch))
        {
            var term = criteria.TitleSearch.Trim().ToLowerInvariant();
            query = query.Where(x => x.Title.ToLower().Contains(term));
        }

        if (criteria.Status is { } status)
        {
            var domainStatus = (TaskManagement.Domain.Enums.WorkItemStatus)(int)status;
            query = query.Where(x => x.Status == domainStatus);
        }

        if (criteria.Priority is { } priority)
        {
            var domainPriority = (TaskManagement.Domain.Enums.WorkItemPriority)(int)priority;
            query = query.Where(x => x.Priority == domainPriority);
        }

        if (criteria.AssigneeId is { } assigneeId)
        {
            query = query.Where(x => x.AssigneeId == assigneeId);
        }

        if (criteria.CreatedFrom is { } from)
        {
            query = query.Where(x => x.CreatedAt >= from);
        }

        if (criteria.CreatedTo is { } to)
        {
            query = query.Where(x => x.CreatedAt <= to);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var list = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return (list.ConvertAll(WorkItemMapper.MapToWorkItemDto), totalCount);
    }

    public async Task<WorkItemDto> AddWorkItem(WorkItemDto workItem, CancellationToken cancellationToken = default)
    {
        var entity = WorkItemMapper.CreateWorkItemEntityFromDto(workItem);
        db.WorkItems.Add(entity);
        await CommitAsync(nameof(AddWorkItem), cancellationToken);
        return WorkItemMapper.MapToWorkItemDto(entity);
    }

    public async Task<WorkItemDto?> UpdateWorkItem(WorkItemDto workItem, CancellationToken cancellationToken = default)
    {
        var entity = await db.WorkItems.FirstOrDefaultAsync(x => x.Id == workItem.Id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        WorkItemMapper.CopyWorkItemDtoOntoEntity(entity, workItem);
        await CommitAsync(nameof(UpdateWorkItem), cancellationToken);
        return WorkItemMapper.MapToWorkItemDto(entity);
    }

    public async Task<SoftDeleteOutcome> DeleteWorkItemById(Guid id, Guid? deletedById, CancellationToken cancellationToken = default)
    {
        var entity = await db.WorkItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return SoftDeleteOutcome.NotFound;
        }

        var now = DateTimeOffset.UtcNow;
        entity.DeletedAt = now;
        entity.DeletedById = deletedById;
        entity.UpdatedAt = now;
        entity.UpdatedById = deletedById;
        await CommitAsync(nameof(DeleteWorkItemById), cancellationToken);
        return SoftDeleteOutcome.Deleted;
    }

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
