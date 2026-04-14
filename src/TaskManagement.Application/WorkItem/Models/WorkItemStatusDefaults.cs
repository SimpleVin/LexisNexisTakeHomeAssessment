namespace TaskManagement.Application.WorkItem.Models;

/// <summary>When status is omitted on create, use <see cref="WorkItemStatus.New"/>.</summary>
public static class WorkItemStatusDefaults
{
    public static WorkItemStatus ForCreate => WorkItemStatus.New;
}
