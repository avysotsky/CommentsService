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
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, DatabaseSettings? database = null)
    {
        services.AddScoped<CommentsService.Application.Events.ICommentEvents, Events.CommentEvents>();
        services.AddSingleton<CommentsService.Application.Events.ICommentCreatedHandler, Events.CommentCreatedLog>();
        services.AddDbContext<CommentsDbContext>(options =>
            options.UseNpgsql(DatabaseOptions.ConnectionString(database),
                pg => pg.MigrationsHistoryTable("__EFMigrationsHistory", "comments")));
        services.AddSingleton<CommentsService.Application.TextFiles.ITextFileProcessor, CommentsService.Infrastructure.TextFiles.Utf8TextFileProcessor>();
        services.AddScoped<CommentsService.Application.TextFiles.ITextFileQueries, CommentsService.Infrastructure.TextFiles.TextFileQueries>();
        services.AddSingleton<CommentsService.Application.Images.IImageProcessor, CommentsService.Infrastructure.Images.SkiaImageProcessor>();
        services.AddScoped<CommentsService.Application.Images.IImageQueries, CommentsService.Infrastructure.Images.ImageQueries>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<CommentsService.Infrastructure.Captcha.ICaptchaImageRenderer, CommentsService.Infrastructure.Captcha.PngCaptchaRenderer>();
        services.AddSingleton<CommentsService.Infrastructure.Captcha.MemoryCaptchaService>();
        services.AddSingleton<CommentsService.Application.Captcha.ICaptchaIssuer>(sp => sp.GetRequiredService<CommentsService.Infrastructure.Captcha.MemoryCaptchaService>());
        services.AddSingleton<CommentsService.Application.Captcha.ICaptchaVerifier>(sp => sp.GetRequiredService<CommentsService.Infrastructure.Captcha.MemoryCaptchaService>());
        services.AddScoped<ICommentRepository, CommentRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();
        services.AddScoped<ICommentQueries, CommentQueries>();
        services.AddScoped<IDatabaseMigrator, EfDatabaseMigrator>();
        services.AddHealthChecks().AddCheck<DatabaseHealthCheck>("database", tags: ["database"]);
        var configured = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("PGPASSWORD")) &&
            new[] { Environment.GetEnvironmentVariable("PGHOST") ?? database?.Host,
                Environment.GetEnvironmentVariable("PGDATABASE") ?? database?.Database,
                Environment.GetEnvironmentVariable("PGUSER") ?? database?.Username }.All(value => !string.IsNullOrWhiteSpace(value));
        if (configured) services.AddScoped<IDatabaseStatusReader, EfDatabaseStatusReader>();
        else services.AddScoped<IDatabaseStatusReader, UnconfiguredDatabaseStatusReader>();
        return services;
    }
}
