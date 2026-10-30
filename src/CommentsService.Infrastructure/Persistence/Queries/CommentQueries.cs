using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Comments;
using CommentsService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CommentsService.Infrastructure.Persistence.Queries;

internal sealed class CommentQueries(CommentsDbContext db) : ICommentQueries
{
    private static IQueryable<CommentDto> Project(IQueryable<Comment> source) => source.Select(comment =>
        new CommentDto(comment.Id, comment.Author.UserName, comment.Author.Email, comment.Author.HomePage,
            comment.Text, comment.CreatedAt, comment.ParentId, comment.RootId));

    public async Task<IReadOnlyList<CommentDto>> GetRepliesAsync(Guid parentId, int limit,
        CommentsService.Application.Comments.GetReplies.ReplyCursor? after, CancellationToken cancellationToken = default)
    {
        var source = db.Comments.AsNoTracking().Where(comment => comment.ParentId == parentId);
        if (after is not null) source = source.Where(comment => comment.CreatedAt < after.CreatedAt ||
            (comment.CreatedAt == after.CreatedAt && comment.Id.CompareTo(after.Id) < 0));
        return await DatabaseOperation.ExecuteAsync(() => Project(source.OrderByDescending(comment => comment.CreatedAt)
            .ThenByDescending(comment => comment.Id).Take(limit)).ToListAsync(cancellationToken));
    }

    public Task<CommentDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DatabaseOperation.ExecuteAsync(() => Project(db.Comments.AsNoTracking().Where(comment => comment.Id == id))
            .SingleOrDefaultAsync(cancellationToken));

    public async Task<IReadOnlyList<CommentDto>> GetLatestAsync(int limit, CancellationToken cancellationToken = default) =>
        await DatabaseOperation.ExecuteAsync(() => Project(db.Comments.AsNoTracking().Where(comment => comment.ParentId == null)
            .OrderByDescending(comment => comment.CreatedAt).ThenByDescending(comment => comment.Id).Take(limit))
            .ToListAsync(cancellationToken));
}
