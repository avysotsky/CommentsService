namespace CommentsService.Infrastructure.Abstractions;

public interface IDatabaseMigrator
{
    Task MigrateAsync(CancellationToken cancellationToken = default);
}
