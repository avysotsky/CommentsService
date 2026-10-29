namespace CommentsService.Application.Abstractions.Persistence;

// Persists the changes staged by repositories in the same application scope.
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
