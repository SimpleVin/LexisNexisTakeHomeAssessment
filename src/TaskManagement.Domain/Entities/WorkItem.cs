using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class WorkItem
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public WorkItemStatus? Status { get; set; }
    public WorkItemPriority Priority { get; set; }
    public Guid? AssigneeId { get; set; }
    public Guid? AssignerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }

    public TeamMember? Assignee { get; set; }
    public TeamMember? Assigner { get; set; }
}
