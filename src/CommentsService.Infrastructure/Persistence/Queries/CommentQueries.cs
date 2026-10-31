using CommentsService.Application.Abstractions.Persistence;
using CommentsService.Application.Comments;
using CommentsService.Domain.Entities;
using CommentsService.Application.Comments.GetRootPage;
using System.Data;
using Microsoft.EntityFrameworkCore;

namespace CommentsService.Infrastructure.Persistence.Queries;

internal sealed class CommentQueries(CommentsDbContext db) : ICommentQueries
{
    private static IQueryable<CommentDto> Project(IQueryable<Comment> source) => source.Select(comment =>
        new CommentDto(comment.Id, comment.Author.UserName, comment.Author.Email, comment.Author.HomePage,
            comment.Text, comment.CreatedAt, comment.ParentId, comment.RootId,
            comment.Image == null ? null : new ImageInfo(comment.Image.Id, comment.Image.FileName, comment.Image.Width, comment.Image.Height),
            comment.TextFile == null ? null : new TextFileInfo(comment.TextFile.Id, comment.TextFile.FileName, comment.TextFile.SizeBytes)));

    public async Task<IReadOnlyList<CommentDto>> GetRepliesAsync(Guid parentId, int limit,
        CommentsService.Application.Comments.GetReplies.ReplyCursor? after, CancellationToken cancellationToken = default)
    {
        var source = db.Comments.AsNoTracking().Where(comment => comment.ParentId == parentId);
        if (after is not null) source = source.Where(comment => comment.CreatedAt < after.CreatedAt ||
            (comment.CreatedAt == after.CreatedAt && comment.Id.CompareTo(after.Id) < 0));
        return await DatabaseOperation.ExecuteAsync(() => Project(source.OrderByDescending(comment => comment.CreatedAt)
            .ThenByDescending(comment => comment.Id).Take(limit)).ToListAsync(cancellationToken));
    }

    public Task<RootCommentsPage> GetRootPageAsync(RootPageQuery query, CancellationToken cancellationToken = default) =>
        DatabaseOperation.ExecuteAsync(async () =>
        {
            // Count and items share a snapshot; reuse the caller's transaction in rollback-based tests.
            await using var transaction = db.Database.CurrentTransaction is null
                ? await db.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead, cancellationToken) : null;
            var roots = db.Comments.AsNoTracking().Where(comment => comment.ParentId == null);
            var count = await roots.LongCountAsync(cancellationToken);
            var descending = query.Direction == SortDirection.Desc;
            IOrderedQueryable<Comment> ordered = query.Sort switch
            {
                CommentSort.UserName => descending ? roots.OrderByDescending(c => c.Author.UserName) : roots.OrderBy(c => c.Author.UserName),
                CommentSort.Email => descending ? roots.OrderByDescending(c => c.Author.Email) : roots.OrderBy(c => c.Author.Email),
                _ => descending ? roots.OrderByDescending(c => c.CreatedAt) : roots.OrderBy(c => c.CreatedAt)
            };
            ordered = descending ? ordered.ThenByDescending(c => c.Id) : ordered.ThenBy(c => c.Id);
            var items = await Project(ordered.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize)).ToListAsync(cancellationToken);
            if (transaction is not null) await transaction.CommitAsync(cancellationToken);
            return new RootCommentsPage(items, query.Page, query.PageSize, count,
                (count + query.PageSize - 1) / query.PageSize);
        });

    public Task<CommentDto?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        DatabaseOperation.ExecuteAsync(() => Project(db.Comments.AsNoTracking().Where(comment => comment.Id == id))
            .SingleOrDefaultAsync(cancellationToken));

    public async Task<IReadOnlyList<CommentDto>> GetLatestAsync(int limit, CancellationToken cancellationToken = default) =>
        await DatabaseOperation.ExecuteAsync(() => Project(db.Comments.AsNoTracking().Where(comment => comment.ParentId == null)
            .OrderByDescending(comment => comment.CreatedAt).ThenByDescending(comment => comment.Id).Take(limit))
            .ToListAsync(cancellationToken));
}
