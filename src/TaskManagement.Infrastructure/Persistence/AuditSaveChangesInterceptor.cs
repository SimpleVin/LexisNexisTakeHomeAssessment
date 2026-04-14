using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Common.Models.Interface.Identity;
using TaskManagement.Domain.Entities;
using TaskManagement.Domain.Enums;

namespace TaskManagement.Infrastructure.Persistence;

public sealed class AuditSaveChangesInterceptor(
    ILogger<AuditSaveChangesInterceptor> logger,
    ICurrentIdentity currentIdentity) : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
    };

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        try
        {
            AppendAuditRowsFromChangeTracker(eventData.Context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to append audit rows before SavingChanges.");
            throw;
        }

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        try
        {
            AppendAuditRowsFromChangeTracker(eventData.Context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to append audit rows before SavingChangesAsync.");
            throw;
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendAuditRowsFromChangeTracker(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var occurredAt = DateTimeOffset.UtcNow;
        var actorId = currentIdentity.TeamMemberId;

        foreach (var entry in context.ChangeTracker.Entries().ToList())
        {
            if (entry.Entity is AuditLogEntry)
            {
                continue;
            }

            switch (entry.Entity)
            {
                case TeamMember m:
                    AddAuditRowForTrackedEntity(context, entry, AuditEntityType.TeamMember, m.Id, occurredAt, actorId, SerializeTeamMemberForAudit(m));
                    break;
                case WorkItem w:
                    AddAuditRowForTrackedEntity(context, entry, AuditEntityType.WorkItem, w.Id, occurredAt, actorId, SerializeWorkItemForAudit(w));
                    break;
            }
        }
    }

    private static void AddAuditRowForTrackedEntity(
        DbContext context,
        EntityEntry entry,
        AuditEntityType entityType,
        Guid entityId,
        DateTimeOffset occurredAt,
        Guid? actorId,
        string payloadJson)
    {
        AuditAction action;
        switch (entry.State)
        {
            case EntityState.Added:
                action = AuditAction.Created;
                break;
            case EntityState.Modified:
                action = IsSoftDeleteStateTransition(entry) ? AuditAction.Deleted : AuditAction.Updated;
                break;
            case EntityState.Deleted:
                action = AuditAction.Deleted;
                payloadJson = "{}";
                break;
            default:
                return;
        }

        context.Set<AuditLogEntry>().Add(new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OccurredAt = occurredAt,
            ActorId = actorId,
            PayloadJson = payloadJson,
        });
    }

    private static bool IsSoftDeleteStateTransition(EntityEntry entry)
    {
        PropertyEntry? deletedProp = entry.Entity switch
        {
            TeamMember => entry.Property(nameof(TeamMember.DeletedAt)),
            WorkItem => entry.Property(nameof(WorkItem.DeletedAt)),
            _ => null,
        };

        if (deletedProp is null)
        {
            return false;
        }

        var original = (DateTimeOffset?)deletedProp.OriginalValue;
        var current = (DateTimeOffset?)deletedProp.CurrentValue;
        return original is null && current is not null;
    }

    private string SerializeTeamMemberForAudit(TeamMember m)
    {
        try
        {
            return JsonSerializer.Serialize(
                new
                {
                    m.Id,
                    m.Name,
                    m.Email,
                    m.CreatedAt,
                    m.UpdatedAt,
                    m.CreatedById,
                    m.UpdatedById,
                    m.DeletedAt,
                    m.DeletedById,
                },
                JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit JSON serialization failed for TeamMember {MemberId}. Using empty payload.", m.Id);
            return "{}";
        }
    }

    private string SerializeWorkItemForAudit(WorkItem w)
    {
        try
        {
            return JsonSerializer.Serialize(
                new
                {
                    w.Id,
                    w.Title,
                    w.Description,
                    Status = w.Status?.ToString(),
                    Priority = w.Priority.ToString(),
                    w.AssigneeId,
                    w.AssignerId,
                    w.CreatedAt,
                    w.UpdatedAt,
                    w.CreatedById,
                    w.UpdatedById,
                    w.DeletedAt,
                    w.DeletedById,
                },
                JsonOptions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit JSON serialization failed for WorkItem {WorkItemId}. Using empty payload.", w.Id);
            return "{}";
        }
    }
}
