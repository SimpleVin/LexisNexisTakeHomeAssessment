using TaskManagement.Application.Common.Contracts.DTO.WorkItem;
using TaskManagement.Application.Common.Contracts.Results;
using TaskManagement.Application.WorkItem.Models;

namespace TaskManagement.Application.Common.Models.Interface.WorkItem;

public interface IWorkItemRepository
{
    Task<WorkItemDto?> GetWorkItemById(Guid id, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WorkItemDto> Items, int TotalCount)> SearchWorkItemsPaged(
        WorkItemListCriteria criteria,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
    Task<WorkItemDto> AddWorkItem(WorkItemDto workItem, CancellationToken cancellationToken = default);
    Task<WorkItemDto?> UpdateWorkItem(WorkItemDto workItem, CancellationToken cancellationToken = default);
    Task<SoftDeleteOutcome> DeleteWorkItemById(Guid id, Guid? deletedById, CancellationToken cancellationToken = default);
}
