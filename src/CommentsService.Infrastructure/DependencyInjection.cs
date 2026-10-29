using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Infrastructure.Persistence.Repositories;
using CommentsService.Infrastructure.Persistence.Queries;
using CommentsService.Infrastructure.Health;
using CommentsService.Infrastructure.Abstractions;
using CommentsService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CommentsService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<CommentsDbContext>(options =>
            options.UseNpgsql(DatabaseOptions.ConnectionString(),
                pg => pg.MigrationsHistoryTable("__EFMigrationsHistory", "comments")));
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ICommentQueries, CommentQueries>();
        services.AddScoped<IDatabaseMigrator, EfDatabaseMigrator>();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["database"]);
        var configured = new[] { "PGHOST", "PGDATABASE", "PGUSER", "PGPASSWORD" }
            .All(name => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)));
        if (configured) services.AddScoped<IDatabaseStatusReader, EfDatabaseStatusReader>();
        else services.AddScoped<IDatabaseStatusReader, UnconfiguredDatabaseStatusReader>();
        return services;
    }
}
