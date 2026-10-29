using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Comments;
using CommentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommentsService.Infrastructure.Persistence.Queries;

internal sealed class CommentQueries(CommentsDbContext db) : ICommentQueries
{
    private static IQueryable<CommentDto> Project(IQueryable<Comment> source) => source.Select(comment =>
        new CommentDto(comment.Id, comment.Author.UserName, comment.Author.Email, comment.Author.HomePage,
            comment.Text, comment.CreatedAt));

    public Task<CommentDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DatabaseOperation.ExecuteAsync(() => Project(db.Comments.AsNoTracking().Where(comment => comment.Id == id))
            .SingleOrDefaultAsync(cancellationToken));

    public async Task<IReadOnlyList<CommentDto>> GetLatestAsync(int limit, CancellationToken cancellationToken = default) =>
        await DatabaseOperation.ExecuteAsync(() => Project(db.Comments.AsNoTracking()
            .OrderByDescending(comment => comment.CreatedAt).ThenByDescending(comment => comment.Id).Take(limit))
            .ToListAsync(cancellationToken));
}
