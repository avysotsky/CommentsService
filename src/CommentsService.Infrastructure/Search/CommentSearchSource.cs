using CommentsService.Application.Search;
using CommentsService.Application.Comments;
using CommentsService.Infrastructure.Persistence;
using CommentsService.Infrastructure.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
namespace CommentsService.Infrastructure.Search;
internal sealed class CommentSearchSource(CommentsDbContext db) : ICommentSearchSource
{
    public async Task<IReadOnlyList<SearchDocument>> ScanAsync(Guid? after, int limit, CancellationToken token)
    {
        var query = db.Comments.AsNoTracking();
        if (after is { } id) query = query.Where(c => c.Id.CompareTo(id) > 0);
        return await DatabaseOperation.ExecuteAsync(() => query.OrderBy(c => c.Id).Take(limit)
            .Select(c => new SearchDocument(c.Id, c.Author.UserName, c.Text, c.CreatedAt)).ToListAsync(token));
    }
    public async Task<IReadOnlyList<SearchDocument>> GetAsync(IReadOnlyList<Guid> ids, CancellationToken token) =>
        await DatabaseOperation.ExecuteAsync(() => db.Comments.AsNoTracking().Where(c => ids.Contains(c.Id))
            .Select(c => new SearchDocument(c.Id, c.Author.UserName, c.Text, c.CreatedAt)).ToListAsync(token));
    public async Task<IReadOnlyList<CommentDto>> HydrateAsync(IReadOnlyList<Guid> ids, CancellationToken token) =>
        await DatabaseOperation.ExecuteAsync(() => CommentQueries.Project(db.Comments.AsNoTracking().Where(c => ids.Contains(c.Id))).ToListAsync(token));
}
