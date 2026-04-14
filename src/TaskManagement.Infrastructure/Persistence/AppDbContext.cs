using Microsoft.EntityFrameworkCore;
using TaskManagement.Domain.Entities;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<AuditLogEntry> AuditLogEntries => Set<AuditLogEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TeamMember>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Name).HasMaxLength(200).IsRequired();
            b.Property(x => x.Email).HasMaxLength(320).IsRequired();
            b.HasQueryFilter(x => x.DeletedAt == null);
        });

        modelBuilder.Entity<WorkItem>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.Title).HasMaxLength(200).IsRequired();
            b.Property(x => x.Description).HasMaxLength(2000);
            b.HasOne(x => x.Assignee)
                .WithMany()
                .HasForeignKey(x => x.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasOne(x => x.Assigner)
                .WithMany()
                .HasForeignKey(x => x.AssignerId)
                .OnDelete(DeleteBehavior.SetNull);
            b.HasQueryFilter(x => x.DeletedAt == null);
        });

        modelBuilder.Entity<AuditLogEntry>(b =>
        {
            b.HasKey(x => x.Id);
            b.Property(x => x.PayloadJson).HasMaxLength(8000);
        });

        SeedData.Apply(modelBuilder);
    }
}
