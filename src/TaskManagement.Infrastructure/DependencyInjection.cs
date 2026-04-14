using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TaskManagement.Application.Common.Models.Interface.TeamMember;
using TaskManagement.Application.Common.Models.Interface.WorkItem;
using TaskManagement.Infrastructure.Persistence;
using TaskManagement.Infrastructure.Persistence.Repositories.TeamMember;
using TaskManagement.Infrastructure.Persistence.Repositories.WorkItem;

namespace TaskManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseInMemoryDatabase("TaskManagement")
                .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });
        services.AddScoped<IWorkItemRepository, WorkItemRepository>();
        services.AddScoped<ITeamMemberRepository, TeamMemberRepository>();
        return services;
    }

    public static void EnsureTaskManagementDatabaseCreated(this IServiceProvider provider)
    {
        try
        {
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }
        catch (Exception ex)
        {
            var logger = provider.GetService<ILoggerFactory>()?.CreateLogger("TaskManagement.Infrastructure.Startup");
            logger?.LogCritical(ex, "Failed to create or verify the Task Management database.");
            throw;
        }
    }
}
