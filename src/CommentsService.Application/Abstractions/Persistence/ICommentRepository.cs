using CommentsService.Domain.Entities;

namespace CommentsService.Application.Abstractions.Persistence;

// Persistence port for application scenarios. Staging a comment does not commit it.
public interface ICommentRepository
{
    void Add(Comment comment);
    Task<Comment?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
