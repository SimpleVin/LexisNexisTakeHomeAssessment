using TaskManagement.Domain.Enums;

namespace TaskManagement.Domain.Entities;

public class AuditLogEntry
{
    public Guid Id { get; set; }
    public AuditEntityType EntityType { get; set; }
    public Guid EntityId { get; set; }
    public AuditAction Action { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public Guid? ActorId { get; set; }
    public string? PayloadJson { get; set; }
}
