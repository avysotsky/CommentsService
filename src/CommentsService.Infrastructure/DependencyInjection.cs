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
        services.AddScoped<IDatabaseMigrator, EfDatabaseMigrator>();
        var configured = new[] { "PGHOST", "PGDATABASE", "PGUSER", "PGPASSWORD" }
            .All(name => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(name)));
        if (configured) services.AddScoped<IDatabaseStatusReader, EfDatabaseStatusReader>();
        else services.AddScoped<IDatabaseStatusReader, UnconfiguredDatabaseStatusReader>();
        return services;
    }
}
