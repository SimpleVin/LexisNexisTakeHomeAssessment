using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence;

internal static class SeedData
{
    internal static readonly Guid Member1Id = Guid.Parse("11111111-1111-1111-1111-111111111101");
    internal static readonly Guid Member2Id = Guid.Parse("11111111-1111-1111-1111-111111111102");
    internal static readonly Guid Member3Id = Guid.Parse("11111111-1111-1111-1111-111111111103");
    internal static readonly Guid TokenUserMemberId = Guid.Parse("11111111-1111-1111-1111-111111111104");
    internal static readonly Guid TokenAdminMemberId = Guid.Parse("11111111-1111-1111-1111-111111111105");

    internal static readonly Guid WorkItem1Id = Guid.Parse("22222222-2222-2222-2222-222222222201");
    internal static readonly Guid WorkItem2Id = Guid.Parse("22222222-2222-2222-2222-222222222202");
    internal static readonly Guid WorkItem3Id = Guid.Parse("22222222-2222-2222-2222-222222222203");

    public static void Apply(ModelBuilder modelBuilder)
    {
        var t0 = new DateTimeOffset(2026, 4, 1, 12, 0, 0, TimeSpan.Zero);
        var t1 = new DateTimeOffset(2026, 4, 2, 9, 30, 0, TimeSpan.Zero);
        var t2 = new DateTimeOffset(2026, 4, 3, 15, 0, 0, TimeSpan.Zero);
        var t3 = new DateTimeOffset(2026, 4, 4, 10, 0, 0, TimeSpan.Zero);
        var t4 = new DateTimeOffset(2026, 4, 4, 10, 1, 0, TimeSpan.Zero);

        modelBuilder.Entity<TeamMember>().HasData(
            new TeamMember
            {
                Id = Member1Id,
                Name = "Jono Mulaudzi",
                Email = "jono@example.com",
                CreatedAt = t0,
                UpdatedAt = t0,
            },
            new TeamMember
            {
                Id = Member2Id,
                Name = "Jack Lee",
                Email = "jack@example.com",
                CreatedAt = t1,
                UpdatedAt = t1,
            },
            new TeamMember
            {
                Id = Member3Id,
                Name = "Sam Baloyi",
                Email = "sam@example.com",
                CreatedAt = t2,
                UpdatedAt = t2,
            },
            new TeamMember
            {
                Id = TokenUserMemberId,
                Name = "Vin User",
                Email = "token-user@example.com",
                CreatedAt = t3,
                UpdatedAt = t3,
            },
            new TeamMember
            {
                Id = TokenAdminMemberId,
                Name = "Vin Admin",
                Email = "token-admin@example.com",
                CreatedAt = t4,
                UpdatedAt = t4,
            });

        modelBuilder.Entity<WorkItem>().HasData(
            new WorkItem
            {
                Id = WorkItem1Id,
                Title = "Draft API specification",
                Description = "Outline endpoints and payloads.",
                Status = WorkItemStatus.InProgress,
                Priority = WorkItemPriority.High,
                AssigneeId = Member1Id,
                AssignerId = Member2Id,
                CreatedAt = t0,
                UpdatedAt = t0,
            },
            new WorkItem
            {
                Id = WorkItem2Id,
                Title = "Seed database",
                Description = null,
                Status = WorkItemStatus.Todo,
                Priority = WorkItemPriority.Medium,
                AssigneeId = Member2Id,
                AssignerId = Member1Id,
                CreatedAt = t1,
                UpdatedAt = t1,
            },
            new WorkItem
            {
                Id = WorkItem3Id,
                Title = "Write README",
                Description = "Run instructions for reviewers.",
                Status = null,
                Priority = WorkItemPriority.Low,
                AssigneeId = null,
                AssignerId = Member2Id,
                CreatedAt = t2,
                UpdatedAt = t2,
            });
    }
}
