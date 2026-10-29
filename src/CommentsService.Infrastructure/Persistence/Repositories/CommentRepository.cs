using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommentsService.Infrastructure.Persistence.Repositories;

internal sealed class CommentRepository(CommentsDbContext db) : ICommentRepository
{
    public void Add(Comment comment)
    {
        ArgumentNullException.ThrowIfNull(comment);
        db.Comments.Add(comment);
    }

    public Task<Comment?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DatabaseOperation.ExecuteAsync(() => db.Comments.Include(comment => comment.Author)
            .SingleOrDefaultAsync(comment => comment.Id == id, cancellationToken));
}
