using CommentsService.Application.Abstractions.Persistence;

namespace CommentsService.Infrastructure.Persistence;

internal sealed class EfUnitOfWork(CommentsDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        DatabaseOperation.ExecuteAsync(() => db.SaveChangesAsync(cancellationToken));
}
