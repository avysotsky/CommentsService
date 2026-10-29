using CommentsService.Infrastructure.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CommentsService.Infrastructure.Persistence;

internal sealed class EfDatabaseMigrator(CommentsDbContext db) : IDatabaseMigrator
{
    public Task MigrateAsync(CancellationToken cancellationToken = default) =>
        db.Database.MigrateAsync(cancellationToken);
}
