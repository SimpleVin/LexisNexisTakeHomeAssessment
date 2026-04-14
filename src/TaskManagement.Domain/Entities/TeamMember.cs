namespace TaskManagement.Domain.Entities;

public class TeamMember
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? CreatedById { get; set; }
    public Guid? UpdatedById { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedById { get; set; }
}
